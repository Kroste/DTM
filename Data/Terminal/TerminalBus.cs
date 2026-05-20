namespace DTM.Data.Terminal;

/// <summary>
/// Singleton-Mediator zwischen den DTM-Aktionen (Backup/Clone/Snapshot etc.)
/// und der pwsh-Console im UI. Die ViewModel ruft <see cref="RunFocSqlAction"/>
/// bzw. <see cref="RunFocSqlSimple"/>; die aktuell registrierte
/// <see cref="ITerminalSession"/> (siehe <see cref="RegisterPowerShellSession"/>)
/// führt den FOC-SQL-Modulaufruf aus, sodass der Output live im pwsh-Tab erscheint.
/// Wenn kein pwsh-Tab läuft, wird der optionale onUnavailable-Fallback aufgerufen.
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
    /// Ruft eine FOC-SQL-Modulfunktion (Backup-Database, Set-Snapshot,
    /// Sync-Database-ToTest) im pwsh-Tab auf. Das Modul ist im Initial-Setup
    /// bereits importiert; wir senden nur noch den Funktionsaufruf. Das Modul
    /// macht sein eigenes Remoting zu den DB-Servern. Output erscheint live im Tab.
    /// </summary>
    /// <param name="functionName">z.B. "Backup-Database".</param>
    /// <param name="database">Datenbankname (MSSQL) bzw. FQDN (Oracle).</param>
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
        _ = sess.SendCommandAsync(call);
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

        _ = sess.SendCommandAsync(call);
    }

    /// <summary>
    /// Sendet ein beliebiges PowerShell-Skript an die laufende Session.
    /// Fire-and-forget — kein Fehler wenn kein Tab aktiv ist.
    /// </summary>
    public static void SendScript(string script)
    {
        if (string.IsNullOrWhiteSpace(script)) return;
        ITerminalSession? sess;
        lock (_lock) sess = _powerShellSession;
        if (sess is null || !sess.IsRunning) return;
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
