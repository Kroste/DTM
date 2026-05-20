using System.Text;
using Renci.SshNet;

namespace DTM.Data.Terminal;

/// <summary>
/// Interaktive SSH-Session über SSH.NET. Nutzt einen <see cref="ShellStream"/>
/// mit echtem PTY ("xterm-256color"), daher kein PTY-Allokationsproblem mehr
/// wie beim externen <c>ssh.exe</c> mit umgeleitetem stdin.
/// </summary>
public sealed class SshTerminalSession : ITerminalSession
{
    public string Host { get; }
    public int Port { get; }
    public string User { get; }

    private SshClient? _client;
    private ShellStream? _shell;
    private CancellationTokenSource? _readerCts;
    private Task? _readerTask;

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler<string>? Notice;
    public event EventHandler? SessionEnded;

    public bool IsRunning =>
        _client?.IsConnected == true && _shell is { CanRead: true };

    public SshTerminalSession(string host, string user, int port = 22)
    {
        Host = host;
        User = user;
        Port = port;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(User))
        {
            ErrorReceived?.Invoke(this, "[SSH: Host oder User leer]");
            SessionEnded?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        SshKeyLoadResult keyResult = SshKeyLocator.LoadKeys();

        // Übersprungene Keys als Notice melden (nicht als Error: User weiß dann
        // welche Datei warum nicht ging, kann das gezielt fixen).
        foreach (string skip in keyResult.SkippedReasons)
            Notice?.Invoke(this, "[" + skip + "]");

        if (!keyResult.HasUsable)
        {
            string msg = keyResult.HasAnyFiles
                ? $"[Keys gefunden ({string.Join(", ", keyResult.FoundFiles)}), aber keiner ist nutzbar. " +
                  "Verschlüsselte Keys benötigen eine Passphrase unter 'Ssh.KeyPassphrases' in appsettings.json " +
                  "(Schlüssel = Dateiname, z.B. \"id_rsa\": \"meinepassphrase\", oder \"*\" als Fallback).]"
                : "[Kein SSH-Key in ~/.ssh gefunden (id_ed25519, id_ecdsa, id_rsa). " +
                  "Lege einen Schlüssel an oder hinterlege einen passenden auf dem Zielserver.]";
            ErrorReceived?.Invoke(this, msg);
            SessionEnded?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        IPrivateKeySource[] keys = keyResult.UsableKeys;

        try
        {
            var info = new ConnectionInfo(
                Host, Port, User,
                new PrivateKeyAuthenticationMethod(User, keys));
            // Etwas Toleranz für DNS/Routing-Hänger.
            info.Timeout = TimeSpan.FromSeconds(15);

            _client = new SshClient(info);
            _client.ErrorOccurred += (_, e) =>
                ErrorReceived?.Invoke(this, $"[SSH-Fehler: {e.Exception.Message}]");

            Notice?.Invoke(this, $"[Verbinde {User}@{Host}:{Port} …]");
            _client.Connect();
            Notice?.Invoke(this, $"[Verbunden: {User}@{Host}:{Port}]");

            // 120x30 ist ein guter Default; bei Bedarf später dynamisch anpassen.
            _shell = _client.CreateShellStream(
                terminalName: "xterm-256color",
                columns: 120, rows: 30,
                width: 800, height: 600,
                bufferSize: 4096);

            _shell.Closed += (_, _) =>
            {
                Notice?.Invoke(this, "[SSH-Session beendet]");
                SessionEnded?.Invoke(this, EventArgs.Empty);
            };

            _readerCts = new CancellationTokenSource();
            _readerTask = Task.Run(() => ReadLoopAsync(_readerCts.Token));
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"[SSH-Verbindung fehlgeschlagen: {ex.Message}]");
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }
        return Task.CompletedTask;
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];
        var decoder = Encoding.UTF8.GetDecoder();
        try
        {
            while (!token.IsCancellationRequested && _shell is { CanRead: true })
            {
                int read = await _shell.ReadAsync(buffer.AsMemory(0, buffer.Length), token)
                    .ConfigureAwait(false);
                if (read <= 0) break;

                // Decoder handhabt halbierte Multi-Byte-Sequenzen über Chunk-Grenzen hinweg.
                char[] chars = new char[decoder.GetCharCount(buffer, 0, read)];
                int n = decoder.GetChars(buffer, 0, read, chars, 0);
                if (n > 0)
                    OutputReceived?.Invoke(this, new string(chars, 0, n));
            }
        }
        catch (OperationCanceledException) { /* Stop angefordert */ }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            ErrorReceived?.Invoke(this, $"[SSH-Read-Fehler: {ex.Message}]");
        }
        finally
        {
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task SendCommandAsync(string command, bool bypassSessionRouting = false, CancellationToken cancellationToken = default)
    {
        // bypassSessionRouting ist nur für PowerShell relevant; bei SSH ignoriert.
        if (_shell is null || !_shell.CanWrite)
        {
            ErrorReceived?.Invoke(this, "[Keine aktive SSH-Session]");
            return Task.CompletedTask;
        }
        try
        {
            // WriteLine hängt LF an; das Server-PTY echo't den Befehl auf der Leitung zurück,
            // daher KEIN lokales Echo nötig (würde sonst doppelt erscheinen).
            _shell.WriteLine(command);
            _shell.Flush();
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"[SSH-Write-Fehler: {ex.Message}]");
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        try
        {
            _readerCts?.Cancel();
            if (_readerTask is not null)
            {
                try { await _readerTask.ConfigureAwait(false); }
                catch { /* swallow */ }
            }
            _shell?.Dispose();
            if (_client?.IsConnected == true) _client.Disconnect();
            _client?.Dispose();
        }
        catch { /* swallow */ }
        finally
        {
            _shell = null;
            _client = null;
            _readerCts?.Dispose();
            _readerCts = null;
            _readerTask = null;
        }
    }

    public void Dispose()
    {
        try { StopAsync().GetAwaiter().GetResult(); }
        catch { /* swallow on dispose */ }
    }
}
