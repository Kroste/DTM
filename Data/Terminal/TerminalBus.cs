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

        // Header sichtbar machen über den Notice-Kanal. Output des Scripts
        // selbst läuft danach durch die normale Pipeline und erscheint
        // mit seinen Write-Host/Write-Output-Zeilen im Tab. Das Skript
        // selbst (oft 30+ Zeilen Code) NICHT als Echo zeigen — das wäre
        // unleserlicher Lärm; nur den Titel.
        if (sess is ITerminalBusInjector injector)
        {
            if (!string.IsNullOrWhiteSpace(title))
                injector.InjectNotice($"[Hintergrund-Job: {title}]");
            injector.InjectNotice($"[Script läuft, Output folgt …]");
        }

        // Backup/Clone-Scripts bauen IHR EIGENES Remoting auf (New-PSSession +
        // Invoke-Command). Daher bypassSessionRouting: true — sonst entsteht ein
        // Double-Hop (Script läuft in $session-Remote, will dort nochmal
        // New-PSSession aufbauen → Credentials fehlen → Output verschwindet).
        _ = sess.SendCommandAsync(script, bypassSessionRouting: true);
    }

    /// <summary>
    /// Ruft eine FOC-SQL-Modulfunktion (Backup-Database, Set-Snapshot,
    /// Sync-Database-ToTest) im pwsh-Tab auf. Das Modul ist im Initial-Setup
    /// bereits importiert; wir senden nur noch den Funktionsaufruf.
    /// Der Aufruf läuft mit bypassSessionRouting (das Modul macht sein eigenes
    /// Remoting). Output erscheint live im Tab.
    /// </summary>
    /// <param name="functionName">z.B. "Backup-Database".</param>
    /// <param name="database">Datenbankname.</param>
    /// <param name="when">Geplanter Zeitpunkt oder null = sofort.</param>
    /// <param name="title">Header-Zeile im Tab.</param>
    /// <param name="onUnavailable">Fallback wenn kein pwsh-Tab aktiv ist.</param>
    public static void RunFocSqlAction(
        string functionName, string database, DateTime? when,
        string title, Action? onUnavailable = null)
    {
        ITerminalSession? sess;
        lock (_lock) sess = _powerShellSession;

        if (sess is null || !sess.IsRunning)
        {
            onUnavailable?.Invoke();
            return;
        }

        if (sess is ITerminalBusInjector injector)
        {
            injector.InjectNotice($"[Hintergrund-Job: {title}]");
            injector.InjectNotice("[Script läuft, Output folgt …]");
        }

        string call = FocSqlRuntime.BuildCall(functionName, database, when);
        // bypassSessionRouting: das Modul baut sein eigenes Remoting auf.
        _ = sess.SendCommandAsync(call, bypassSessionRouting: true);
    }

    /// <summary>
    /// Ruft eine FOC-SQL-Funktion ohne Zeitparameter auf (Restore-Snapshot,
    /// Remove-Snapshot, Set-Archive-Log, Copy-Database-ToSamba). Diese sind
    /// teils interaktiv (Snapshot-Auswahl, ja/nein) — die Prompts erscheinen
    /// im Tab und werden über die Input-Box beantwortet (DtmPSHost).
    /// </summary>
    /// <param name="functionName">z.B. "Restore-Snapshot".</param>
    /// <param name="database">Datenbankname.</param>
    /// <param name="extraArgs">Zusätzliche Argumente, z.B. "-Off" für Archive-Log.</param>
    /// <param name="title">Header im Tab.</param>
    /// <param name="onUnavailable">Fallback wenn kein pwsh-Tab aktiv.</param>
    public static void RunFocSqlSimple(
        string functionName, string database, string extraArgs,
        string title, Action? onUnavailable = null)
    {
        ITerminalSession? sess;
        lock (_lock) sess = _powerShellSession;

        if (sess is null || !sess.IsRunning)
        {
            onUnavailable?.Invoke();
            return;
        }

        if (sess is ITerminalBusInjector injector)
        {
            injector.InjectNotice($"[Aktion: {title}]");
            injector.InjectNotice("[Falls Eingaben nötig sind, bitte in die Befehlszeile tippen]");
        }

        string dbEsc = database.Replace("'", "''");
        string call = $"{functionName} -Database '{dbEsc}'";
        if (!string.IsNullOrWhiteSpace(extraArgs))
            call += " " + extraArgs;

        _ = sess.SendCommandAsync(call, bypassSessionRouting: true);
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
