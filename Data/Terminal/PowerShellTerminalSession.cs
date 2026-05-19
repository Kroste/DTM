using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;

namespace DTM.Data.Terminal;

/// <summary>
/// Hostet PowerShell 7 in-process über Microsoft.PowerShell.SDK.
/// Statt Process.Start + pwsh.exe läuft alles im selben Prozess; Variablen,
/// Module und PSSession-Handles bleiben zwischen Befehlen erhalten.
///
/// Hinweis zu remote: <c>Enter-PSSession</c> braucht eine Konsole und
/// funktioniert in-process nicht. Stattdessen wird im InitialScript eine
/// persistente PSSession in <c>$session</c> aufgebaut; jeder vom User
/// getippte Befehl wird automatisch via <c>Invoke-Command -Session $session</c>
/// remote ausgeführt (sofern <c>$session</c> existiert und offen ist).
/// </summary>
public sealed class PowerShellTerminalSession : ITerminalSession, ITerminalBusInjector
{
    /// <summary>Optionales Setup-Skript, das einmal beim Start läuft.</summary>
    public string? InitialScript { get; }

    /// <summary>
    /// Wenn true (Default), wird jeder User-Befehl automatisch in
    /// <c>Invoke-Command -Session $session -ScriptBlock {...}</c> verpackt,
    /// sofern <c>$session</c> existiert und Open ist. So fühlt sich der Tab
    /// wie ein klassisches <c>Enter-PSSession</c> an.
    /// </summary>
    public bool AutoRouteThroughSession { get; }

    private Runspace? _runspace;
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler<string>? Notice;
    public event EventHandler? SessionEnded;

    public bool IsRunning => _runspace?.RunspaceStateInfo.State == RunspaceState.Opened;

    public PowerShellTerminalSession(string? initialScript = null, bool autoRouteThroughSession = true)
    {
        InitialScript = initialScript;
        AutoRouteThroughSession = autoRouteThroughSession;
    }

    // Defense in depth: falls die Telemetry-Opt-Out-Env-Variable aus irgendeinem
    // Grund nicht greift, schlucken wir AppInsights-Envelopes hier raus, statt
    // sie ins UI durchzulassen.
    private static bool IsTelemetryNoise(string? line)
    {
        if (string.IsNullOrEmpty(line)) return false;
        ReadOnlySpan<char> s = line.AsSpan().TrimStart();
        return s.StartsWith("Application Insights Telemetry:", StringComparison.Ordinal)
            || (s.StartsWith("{\"name\":", StringComparison.Ordinal) && s.Contains("\"iKey\":", StringComparison.Ordinal));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _runspace = RunspaceFactory.CreateRunspace();
            _runspace.Open();
            Notice?.Invoke(this, "[PowerShell-Runspace geöffnet]");

            if (!string.IsNullOrWhiteSpace(InitialScript))
            {
                Notice?.Invoke(this, "[Initial-Setup wird ausgeführt …]");
                await ExecuteRawAsync(InitialScript!, cancellationToken).ConfigureAwait(false);
                Notice?.Invoke(this, "[Initial-Setup abgeschlossen]");
            }
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"[PowerShell-Start fehlgeschlagen: {ex.Message}]");
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command)) return;

        string script = AutoRouteThroughSession
            ? WrapForOptionalRemoting(command)
            : command;

        await ExecuteRawAsync(script, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Verpackt einen User-Befehl so, dass er — falls $session offen ist —
    /// per Invoke-Command auf der Remote-Session läuft, sonst lokal.
    /// Output wird durch <c>Out-String -Stream</c> geschickt, damit der
    /// Default-Formatter aktiv wird (Tabellen statt 'System.Diagnostics.Process'
    /// als ToString-Output). Im SDK-Hosting fehlt sonst der Out-Default-Pfad,
    /// den pwsh.exe automatisch anhängt.
    /// Wir kodieren den User-Befehl Base64, um sämtliche Quoting-Probleme
    /// zwischen C# und PowerShell zu vermeiden.
    /// </summary>
    private static string WrapForOptionalRemoting(string userCommand)
    {
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(userCommand));
        return $@"
$__cmd = [Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('{encoded}'))
$__sb  = [scriptblock]::Create($__cmd)
& {{
    if ($session -and $session.State -eq 'Opened') {{
        Invoke-Command -Session $session -ScriptBlock $__sb
    }} else {{
        & $__sb
    }}
}} | Out-String -Stream -Width 200
Remove-Variable __cmd, __sb -ErrorAction SilentlyContinue
";
    }

    private async Task ExecuteRawAsync(string script, CancellationToken cancellationToken)
    {
        if (_runspace is null || _runspace.RunspaceStateInfo.State != RunspaceState.Opened)
        {
            ErrorReceived?.Invoke(this, "[PowerShell-Runspace nicht offen]");
            return;
        }

        await _executionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var ps = PowerShell.Create();
            ps.Runspace = _runspace;
            ps.AddScript(script);

            var output = new PSDataCollection<PSObject>();
            output.DataAdded += (_, e) =>
            {
                var obj = output[e.Index];
                if (obj is null) return;
                string text = obj.ToString();
                if (IsTelemetryNoise(text)) return;
                OutputReceived?.Invoke(this, text + Environment.NewLine);
            };

            ps.Streams.Error.DataAdded += (_, e) =>
            {
                var err = ps.Streams.Error[e.Index];
                string text = err.ToString();
                if (IsTelemetryNoise(text)) return;
                ErrorReceived?.Invoke(this, text + Environment.NewLine);
            };
            ps.Streams.Warning.DataAdded += (_, e) =>
            {
                var w = ps.Streams.Warning[e.Index];
                if (IsTelemetryNoise(w.Message)) return;
                Notice?.Invoke(this, $"WARNING: {w.Message}");
            };
            ps.Streams.Information.DataAdded += (_, e) =>
            {
                var info = ps.Streams.Information[e.Index];
                string text = info.MessageData?.ToString() ?? string.Empty;
                if (IsTelemetryNoise(text)) return;

                // Write-Host landet in PowerShell 5+ als Information-Record mit Tag "PSHOST".
                // Aus User-Sicht ist das normaler Console-Output (im Initial-Script z.B.
                // unsere "Verbunden mit ..."-Meldung), nicht ein Meta-Notice.
                bool isWriteHost = info.Tags?.Contains("PSHOST") == true;
                if (isWriteHost)
                    OutputReceived?.Invoke(this, text + Environment.NewLine);
                else
                    Notice?.Invoke(this, text);
            };
            ps.Streams.Verbose.DataAdded += (_, e) =>
            {
                var v = ps.Streams.Verbose[e.Index];
                if (IsTelemetryNoise(v.Message)) return;
                Notice?.Invoke(this, $"VERBOSE: {v.Message}");
            };

            // Async-Ausführung, damit der UI-Thread frei bleibt.
            await Task.Factory.FromAsync(
                (cb, st) => ps.BeginInvoke<PSObject, PSObject>(input: null, output, settings: null, cb, st),
                ps.EndInvoke,
                state: null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"[PowerShell-Fehler: {ex.Message}]");
        }
        finally
        {
            _executionLock.Release();
        }
    }

    public Task StopAsync()
    {
        try
        {
            if (_runspace is not null)
            {
                try { _runspace.Close(); } catch { /* swallow */ }
                _runspace.Dispose();
            }
        }
        catch { /* swallow */ }
        finally
        {
            _runspace = null;
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { StopAsync().GetAwaiter().GetResult(); }
        catch { /* swallow */ }
        _executionLock.Dispose();
    }

    /// <summary>
    /// Erlaubt dem TerminalBus, Hintergrund-Job-Header in den Output-Stream
    /// einzuschleusen, ohne dass dafür ein PS-Befehl ausgeführt werden muss.
    /// </summary>
    public void InjectNotice(string text) => Notice?.Invoke(this, text);
}
