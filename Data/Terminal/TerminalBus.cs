using DTM.Data.Terminal;

namespace DTM.Data.Terminal;

/// <summary>
/// Singleton-Mediator zwischen Services (z.B. MSSQL-Backup) und der pwsh-Console
/// im UI. Ein Service ruft <see cref="RunScript"/> mit einem PS-Skript; der
/// aktuell registrierte <see cref="ITerminalSession"/> (siehe <see cref="RegisterPowerShellSession"/>)
/// führt es aus, sodass der Output live im pwsh-Tab erscheint.
/// Wenn kein pwsh-Tab läuft (Tab nie geöffnet, App noch nicht initialisiert),
/// wird das Skript verworfen und ein optionaler Fallback-Logger aufgerufen.
/// </summary>
public static class TerminalBus
{
    private static readonly object _lock = new();
    private static ITerminalSession? _powerShellSession;

    /// <summary>
    /// Wird vom <c>ConsoleControl</c> aufgerufen, sobald eine PowerShell-Session
    /// bereit ist. Mehrfach-Aufrufe sind okay: die letzte Registrierung gewinnt
    /// (etwa wenn das Control neu attached oder die Session reconnected).
    /// </summary>
    public static void RegisterPowerShellSession(ITerminalSession session)
    {
        lock (_lock) _powerShellSession = session;
    }

    /// <summary>Hebt die Registrierung auf (wenn das Control disposed wird).</summary>
    public static void UnregisterPowerShellSession(ITerminalSession session)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_powerShellSession, session))
                _powerShellSession = null;
        }
    }

    /// <summary>Ob aktuell eine pwsh-Session zum Routen verfügbar ist.</summary>
    public static bool HasPowerShellSession
    {
        get { lock (_lock) return _powerShellSession is { IsRunning: true }; }
    }

    /// <summary>
    /// Schickt ein PowerShell-Skript an die registrierte pwsh-Session.
    /// Bei Erfolg sieht der User Befehl + Output im pwsh-Tab.
    /// Falls noch keine Session existiert, wird <paramref name="onUnavailable"/>
    /// aufgerufen (etwa um auf den alten <c>ExecuteLocalPs</c>-Pfad zurückzufallen).
    /// </summary>
    /// <param name="title">Kurze Beschreibung, wird als Header in den Tab geschrieben (optional).</param>
    /// <param name="script">PowerShell-Skript.</param>
    /// <param name="onUnavailable">Fallback wenn kein pwsh-Tab aktiv ist.</param>
    public static void RunScript(string? title, string script, Action? onUnavailable = null)
    {
        ITerminalSession? sess;
        lock (_lock) sess = _powerShellSession;

        if (sess is null || !sess.IsRunning)
        {
            onUnavailable?.Invoke();
            return;
        }

        // Header sichtbar machen: über den Notice-Kanal, sonst würde der Header
        // selbst durch Out-String-Stream geleitet. Notice landet sofort.
        if (!string.IsNullOrWhiteSpace(title))
            (sess as ITerminalBusInjector)?.InjectNotice($"[Hintergrund-Job: {title}]");

        _ = sess.SendCommandAsync(script);
    }
}

/// <summary>
/// Optionale Schnittstelle für Sessions, die "synthetische Notices" anzeigen
/// können (z.B. Header für Hintergrund-Jobs). Wenn eine Session das nicht
/// implementiert, fehlt nur die Headerzeile - der Job läuft trotzdem.
/// </summary>
public interface ITerminalBusInjector
{
    void InjectNotice(string text);
}
