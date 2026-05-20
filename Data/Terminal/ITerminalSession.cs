namespace DTM.Data.Terminal;

/// <summary>
/// Abstraktion über eine interaktive Terminal-Session (aktuell die in-process
/// PowerShell-Session via Microsoft.PowerShell.SDK). Ersetzt das frühere
/// Process.Start-mit-Pipes-Modell. Als Interface gehalten, damit das
/// ConsoleControl und der TerminalBus nicht direkt an die konkrete
/// Session-Klasse koppeln.
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

    /// <summary>
    /// Sendet einen Befehl an die Session.
    /// </summary>
    /// <param name="command">Der auszuführende Befehl/Skript.</param>
    /// <param name="bypassSessionRouting">
    /// Nur für PowerShell relevant: wenn true, wird der Befehl NICHT durch das
    /// automatische <c>Invoke-Command -Session $session</c>-Routing geleitet,
    /// sondern direkt im lokalen Runspace ausgeführt. Wichtig für Skripte, die
    /// ihr eigenes Remoting aufbauen (z.B. Backup), sonst entsteht ein
    /// PowerShell-Double-Hop und der Output geht verloren.
    /// </param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task SendCommandAsync(string command, bool bypassSessionRouting = false, CancellationToken cancellationToken = default);

    Task StopAsync();
}
