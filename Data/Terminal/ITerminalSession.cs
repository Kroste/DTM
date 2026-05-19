namespace DTM.Data.Terminal;

/// <summary>
/// Gemeinsame Abstraktion über interaktive Terminal-Sessions
/// (SSH via SSH.NET, PowerShell via Microsoft.PowerShell.SDK).
/// Ersetzt das frühere Process.Start-mit-Pipes-Modell, das von
/// SSH wegen fehlender PTY-Allokation abgewiesen wurde.
/// </summary>
public interface ITerminalSession : IDisposable
{
    /// <summary>Normaler Output (stdout-Äquivalent).</summary>
    event EventHandler<string>? OutputReceived;

    /// <summary>Fehler-Output (stderr-Äquivalent).</summary>
    event EventHandler<string>? ErrorReceived;

    /// <summary>Informelle Meldungen vom Wrapper (Verbindungs-Status etc.).</summary>
    event EventHandler<string>? Notice;

    /// <summary>Wird gefeuert, wenn die Session endet (regulär oder durch Fehler).</summary>
    event EventHandler? SessionEnded;

    bool IsRunning { get; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task SendCommandAsync(string command, CancellationToken cancellationToken = default);
    Task StopAsync();
}
