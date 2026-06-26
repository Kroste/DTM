using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DTM.ViewModels.TreeNodes;
using DTM.Config;
using DTM.Views;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace DTM.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private IDTM_DATA _data;

    // Optional: wird von der App via DI gesetzt; in Tests bleibt es null und die
    // Dialog-Aufrufe fallen auf direktes "new" zurueck (Tests stossen die UI-
    // Befehle nicht an, daher reicht das).
    private readonly IServiceProvider? _services;

    private T ResolveOrNew<T>() where T : class, new() =>
        _services?.GetService<T>() ?? new T();

    public ObservableCollection<NodeViewModelBase> RootNodes { get; } = new();

    [ObservableProperty] private NodeViewModelBase? _selectedNode;

    [ObservableProperty] private string _dbName = "—";
    [ObservableProperty] private string _dbHost = "—";
    [ObservableProperty] private string _dbStatus = "—";
    [ObservableProperty] private string _dbVersion = "—";
    [ObservableProperty] private string _dbSize = "—";
    [ObservableProperty] private string _recoveryOrArchiveMode = "—";
    [ObservableProperty] private string _recoveryLabel = "Recovery";
    [ObservableProperty] private string _activeSessionsLabel = "Aktive Sessions: —";
    [ObservableProperty] private string _activeSessionsCount = "0";
    [ObservableProperty] private string _statusBar = "Bereit";
    [ObservableProperty] private string _backupButtonText = "Backup";

    [ObservableProperty] private bool _archiveLogOnEnabled;
    [ObservableProperty] private bool _archiveLogOffEnabled;

    // Get-ClusterHealthStatus ist MSSQL-only (Always-On/Failover-Cluster).
    // Bei Oracle-Selection blenden wir den Button ganz aus.
    [ObservableProperty] private bool _clusterHealthVisible;

    // Backup-Browser ist in v1 MSSQL-only — Oracle-Tab blendet die ganze
    // Gruppe aus, bis ein RMAN-Wrapper kommt.
    [ObservableProperty] private bool _backupBrowserVisible;

    // Wartungs-Gruppe (DBCC CHECKDB / Index-Rebuild / Shrink-Log) ist
    // T-SQL-spezifisch und nur fuer MSSQL sichtbar.
    [ObservableProperty] private bool _maintenanceVisible;

    // Recovery-Mode-Dropdown im Info-Card (MSSQL-only). Oracle zeigt
    // stattdessen den read-only ArchiveLogMode-TextBlock.
    [ObservableProperty] private bool _recoveryModeVisible;
    [ObservableProperty] private string _recoveryModeSelected = "FULL";

    public IReadOnlyList<string> RecoveryModeOptions { get; } =
        new[] { "FULL", "SIMPLE", "BULK_LOGGED" };

    // Schutz vor Rekursion: ApplyStats setzt RecoveryModeSelected aus dem
    // Server-Stand — der ComboBox-Selection-Changed-Handler darf das nicht
    // als User-Aktion missverstehen und Set-DbRecoveryMode aufrufen.
    private bool _settingRecoveryModeInternally;
    private string _lastSyncedRecoveryMode = string.Empty;

    private List<Session> _currentSessions = new();

    // Initial-Setup der pwsh-Session:
    //  1. ExecutionPolicy für DIESEN Prozess auf Bypass (nur Runspace-lokal,
    //     ändert nichts am System, braucht keine Admin-Rechte). Sonst scheitert
    //     der Modul-Import an "running scripts is disabled on this system".
    //  2. credential.xml muss existieren — das FOC-SQL-Modul nutzt sie für sein
    //     eigenes Remoting/Credential-Handling. Fehlt sie → klare Meldung.
    //  3. FOC-SQL-Modul frisch von Samba laden — darüber laufen alle Aktionen.
    //     Das Modul baut sein Remoting zu den Servern selbst auf; DTM hält
    //     KEINE eigene PSSession mehr.
    public string ShellInitialCommand =>
        "try { Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force -ErrorAction Stop } catch {}; " +
        "if (-not (Test-Path \"$env:USERPROFILE\\credential.xml\")) { " +
        "  Write-Error 'credential.xml im Benutzerprofil fehlt. " +
        "Bitte einmalig erstellen: Get-Credential | Export-Clixml \"$env:USERPROFILE\\credential.xml\"'; " +
        "  return " +
        "}; " +
        DTM.Data.Terminal.FocSqlRuntime.BuildImportSnippet() + "; " +
        "Write-Host 'FOC-SQL Modul geladen. Bereit.'";

    public MainWindowViewModel(
        IDTM_DATA data,
        Dictionary<DB_SERVER.ServerTyp, DB_SERVER> servers,
        IServiceProvider? services = null)
    {
        _data = data;
        _services = services;
        foreach (var typ in servers.Keys)
            RootNodes.Add(new ServerNodeViewModel(typ, data));
    }

    partial void OnSelectedNodeChanged(NodeViewModelBase? value)
    {
        ArchiveLogOnEnabled = false;
        ArchiveLogOffEnabled = false;
        ClusterHealthVisible = false;
        BackupBrowserVisible = false;
        MaintenanceVisible = false;
        RecoveryModeVisible = false;

        switch (value)
        {
            case ServerNodeViewModel server:
                _ = LoadServerAsync(server);
                break;
            case DatabaseNodeViewModel db:
                _ = LoadStatsAsync(db);
                break;
        }
    }

    private static async Task LoadServerAsync(ServerNodeViewModel server)
    {
        await server.EnsureChildrenLoadedAsync();
        server.IsExpanded = true;
    }

    private async Task LoadStatsAsync(DatabaseNodeViewModel db)
    {
        StatusBar = $"Lade Stats für {db.Database.Name}…";
        try
        {
            Database_Stats stats = await Task.Run(() => _data.get_Database_Stats(db.ServerTyp, db.Database));
            await Dispatcher.UIThread.InvokeAsync(() => ApplyStats(stats));
            StatusBar = "Bereit";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Stats für {db.Database.Name} fehlgeschlagen.");
            StatusBar = $"Fehler: {ex.Message}";
        }
    }

    internal void ApplyStats(Database_Stats stats)
    {
        _currentSessions = stats.Sessions ?? new List<Session>();
        ActiveSessionsCount = _currentSessions.Count.ToString();
        ActiveSessionsLabel = $"Aktive Sessions: {_currentSessions.Count}";

        if (stats is Database_Stats_MSSQL m)
        {
            bool recoveryOn = string.Equals(m.RecorveryModel, "FULL", StringComparison.OrdinalIgnoreCase);
            ArchiveLogOnEnabled  = !recoveryOn;
            ArchiveLogOffEnabled =  recoveryOn;
            ClusterHealthVisible = true;
            BackupBrowserVisible = true;
            MaintenanceVisible   = true;
            BackupButtonText = "Backup";
            DbName = m.Name ?? "—";
            DbHost = m.Server ?? "—";
            DbStatus = m.State ?? "—";
            DbVersion = m.CompatibllityLevel.ToString();
            DbSize = $"{m.DataSizeMB.ToString(System.Globalization.CultureInfo.InvariantCulture)} MB";
            RecoveryLabel = "Recovery";
            RecoveryOrArchiveMode = m.RecorveryModel ?? "—";

            // Dropdown auf aktuellen Server-Stand setzen, ohne den User-
            // Change-Pfad zu triggern (Suppression-Flag).
            SyncRecoveryModeFromStats(m.RecorveryModel);
        }
        else if (stats is Database_Stats_ORACLE o)
        {
            bool archiveOn = string.Equals(o.ArchiveLogMode, "ARCHIVELOG", StringComparison.OrdinalIgnoreCase);
            ArchiveLogOnEnabled  = !archiveOn;
            ArchiveLogOffEnabled =  archiveOn;
            BackupButtonText = "Dump";
            DbName = o.InstanceName ?? "—";
            DbHost = o.Server ?? "—";
            DbStatus = o.State ?? "—";
            DbVersion = o.OracleVersion ?? "—";
            DbSize = $"{o.DataSizeMB.ToString(System.Globalization.CultureInfo.InvariantCulture)} MB";
            RecoveryLabel = "ArchiveLog";
            RecoveryOrArchiveMode = o.ArchiveLogMode ?? "—";
        }
    }

    [RelayCommand]
    private async Task Backup()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        await RunDbActionAsync("Backup-Database", db, "Backup");
    }

    [RelayCommand]
    private async Task Clone()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        await RunDbActionAsync("Sync-Database-ToTest", db, "Clone");
    }

    [RelayCommand]
    private async Task Snapshot()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        await RunDbActionAsync("Set-Snapshot", db, "Snapshot");
    }

    /// <summary>
    /// Gemeinsamer Pfad für Backup/Clone/Snapshot: Zeit abfragen, dann die
    /// passende FOC-SQL-Modulfunktion über den TerminalBus im pwsh-Tab aufrufen.
    /// Das Modul übernimmt Remoting, Credential-Handling und Scheduling.
    /// </summary>
    private async Task RunDbActionAsync(string focFunction, DatabaseNodeViewModel db, string label)
    {
        TimePickResult pick = await PickTimeAsync();
        if (pick.Cancelled) return;

        DateTime? when = pick.When; // null = sofort
        string whenText = when is { } w ? $"um {w:g}" : "sofort";
        StatusBar = $"{label} für {db.Database.Name} {whenText} …";

        DTM.Data.Terminal.TerminalBus.RunFocSqlAction(
            functionName: focFunction,
            database: ModuleDatabaseId(db),
            when: when,
            title: $"{label} {db.Database.Name}",
            onUnavailable: () =>
                Dispatcher.UIThread.Post(() =>
                    StatusBar = $"{label} nicht möglich: pwsh-Tab ist nicht bereit."));

        StatusBar = $"{label} für {db.Database.Name} ausgelöst.";
    }

    /// <summary>
    /// Bezeichner, den die FOC-SQL-Modulfunktionen erwarten:
    /// MSSQL → DB-Name, Oracle → FQDN (das Modul baut daraus 'oracle@&lt;FQDN&gt;'
    /// als SSH-Ziel). Fällt bei fehlendem FQDN auf den Namen zurück.
    /// </summary>
    internal static string ModuleDatabaseId(DatabaseNodeViewModel db) =>
        db.ServerTyp == DB_SERVER.ServerTyp.MSSQL
            ? db.Database.Name
            : (string.IsNullOrWhiteSpace(db.Database.FQDN) ? db.Database.Name : db.Database.FQDN!);

    [RelayCommand]
    private void DbToSamba()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Copy-Database-ToSamba", db, "", "DB → Samba");
    }

    [RelayCommand]
    private async Task RestoreSnapshot()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;

        // Oracle: Vorab Restore-Vorschau-Dialog mit Restore-Points und
        // PDB-Liste + Multi-PDB-Warnung. MSSQL ueberspringt das.
        if (db.ServerTyp == DB_SERVER.ServerTyp.ORACLE)
        {
            Window? owner = GetMainWindow();
            if (owner is null || _services is null) return;

            OracleRestoreSelectViewModel vm =
                _services.GetRequiredService<OracleRestoreSelectViewModel>();
            OracleRestoreSelectWindow dlg = new() { DataContext = vm };

            // LoadAsync nicht awaiten — der Dialog geht sofort auf mit
            // Spinner, die Daten landen im UI sobald sie da sind.
            _ = vm.LoadAsync(ModuleDatabaseId(db));

            bool ok = await dlg.ShowDialog<bool>(owner);
            if (!ok) return;
        }

        RunSimpleAction("Restore-Snapshot", db, "", "Restore Snapshot");
    }

    [RelayCommand]
    private void RemoveSnapshot()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Remove-Snapshot", db, "", "Remove Snapshot");
    }

    // Set-Archive-Log dispatched im FOC-SQL-Modul nach DB-Typ:
    //   MSSQL  -> Database-Set-Recovery-Mode -Recovery FULL/SIMPLE
    //   Oracle -> /mnt/dbmgmt/scripts/archivelog-on.sh / -off.sh
    // Die "Log An/Aus"-Labels sind dadurch Oracle-zentriert; fuer MSSQL ist
    // es semantisch ein Recovery-Mode-Toggle. Akzeptierte Doppelnutzung
    // (siehe CLAUDE.md / Roadmap 1.1); fuer MSSQL bringt 3.4 einen dedizierten
    // Recovery-Mode-Dropdown als saubere Alternative.
    [RelayCommand]
    private void ArchiveLogOn()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Set-Archive-Log", db, "", "ArchiveLog An");
        ArchiveLogOnEnabled = false;
        ArchiveLogOffEnabled = false;
        _ = Task.Delay(TimeSpan.FromSeconds(8))
                .ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => LoadStatsAsync(db)));
    }

    [RelayCommand]
    private void ArchiveLogOff()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Set-Archive-Log", db, "-Off", "ArchiveLog Aus");
        ArchiveLogOnEnabled = false;
        ArchiveLogOffEnabled = false;
        _ = Task.Delay(TimeSpan.FromSeconds(8))
                .ContinueWith(_ => Dispatcher.UIThread.InvokeAsync(() => LoadStatsAsync(db)));
    }

    /// <summary>
    /// Aktionen ohne Zeitplanung (Restore/Remove/Archive/Samba). Teils
    /// interaktiv — der Output und etwaige Prompts erscheinen im pwsh-Tab,
    /// Antworten tippt der User in die Befehlszeile.
    /// </summary>
    private void RunSimpleAction(string focFunction, DatabaseNodeViewModel db, string extraArgs, string label)
    {
        StatusBar = $"{label} für {db.Database.Name} …";
        DTM.Data.Terminal.TerminalBus.RunFocSqlSimple(
            functionName: focFunction,
            database: ModuleDatabaseId(db),
            extraArgs: extraArgs,
            title: $"{label} {db.Database.Name}",
            onUnavailable: () =>
                Dispatcher.UIThread.Post(() =>
                    StatusBar = $"{label} nicht möglich: pwsh-Tab ist nicht bereit."));
        StatusBar = $"{label} für {db.Database.Name} gestartet — siehe Shell-Tab.";
    }

    [RelayCommand]
    private async Task ManageConnections()
    {
        Window? owner = GetMainWindow();
        if (owner is null) return;
        ConnectionManagerWindow dlg = new() { DataContext = ResolveOrNew<ConnectionManagerViewModel>() };
        await dlg.ShowDialog(owner);
        ReloadFromStores();
    }

    private void ReloadFromStores()
    {
        Dictionary<DB_SERVER.ServerTyp, DB_SERVER> newServers = new();
        foreach (DTM.Config.ConnectionEntry entry in DTM.Config.ConnectionStore.Load())
        {
            if (Enum.TryParse<DB_SERVER.ServerTyp>(entry.Key, ignoreCase: true, out var typ))
                newServers[typ] = new DB_SERVER(typ, entry.ToCredential());
        }

        _data = new DTM_DATA(newServers, new ODBC_Factory());

        SelectedNode = null;
        RootNodes.Clear();
        foreach (var typ in newServers.Keys)
            RootNodes.Add(new ServerNodeViewModel(typ, _data));

        DbName = "—"; DbHost = "—"; DbStatus = "—"; DbVersion = "—";
        DbSize = "—"; RecoveryOrArchiveMode = "—"; ActiveSessionsCount = "0";
        ArchiveLogOnEnabled = false;
        ArchiveLogOffEnabled = false;
        ClusterHealthVisible = false;
        BackupBrowserVisible = false;
        MaintenanceVisible = false;
        RecoveryModeVisible = false;
        StatusBar = "Verbindungen aktualisiert.";
        _logger.Debug("Verbindungen neu geladen: {0} Server.", newServers.Count);
    }

    // Get-ClusterHealthStatus -Server <host> — Always-On/Failover-Cluster-Status.
    // Read-only, MSSQL-only; Output erscheint im pwsh-Tab.
    [RelayCommand]
    private void CheckClusterHealth()
    {
        if (string.IsNullOrWhiteSpace(DbHost) || DbHost == "—") return;
        DTM.Data.Terminal.TerminalBus.RunFocSqlServerAction(
            "Get-ClusterHealthStatus", DbHost, "Cluster-Health");
    }

    // --- Recovery-Mode-Dropdown (Phase 3.4, MSSQL-only) ---

    private void SyncRecoveryModeFromStats(string? recoveryFromServer)
    {
        string normalized = recoveryFromServer?.ToUpperInvariant() ?? "FULL";
        if (!RecoveryModeOptions.Contains(normalized))
            normalized = "FULL";

        _settingRecoveryModeInternally = true;
        try
        {
            RecoveryModeSelected = normalized;
            _lastSyncedRecoveryMode = normalized;
            RecoveryModeVisible = true;
        }
        finally
        {
            _settingRecoveryModeInternally = false;
        }
    }

    partial void OnRecoveryModeSelectedChanged(string value)
    {
        if (_settingRecoveryModeInternally) return;
        if (string.Equals(value, _lastSyncedRecoveryMode, StringComparison.OrdinalIgnoreCase)) return;

        _ = OnRecoveryModeChangedByUserAsync(value);
    }

    private async Task OnRecoveryModeChangedByUserAsync(string newMode)
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        Window? owner = GetMainWindow();
        if (owner is null) return;

        string warning = string.Equals(newMode, "SIMPLE", StringComparison.OrdinalIgnoreCase)
            ? "\n\nAchtung: Wechsel zu SIMPLE bricht die Log-Chain — Point-in-Time-Restore ist "
              + "ab diesem Zeitpunkt erst nach dem naechsten Voll-Backup wieder moeglich."
            : string.Empty;

        ConfirmWindow dlg = new()
        {
            WindowTitle = "Recovery-Modus aendern?",
            Message = $"Der Recovery-Modus der Datenbank „{db.Database.Name}\" wird von "
                    + $"{_lastSyncedRecoveryMode} auf {newMode} gesetzt.{warning}\n\nFortfahren?",
            ConfirmText = newMode,
            CancelText = "Abbrechen",
        };

        bool ok = await dlg.ShowDialog<bool>(owner);
        if (!ok)
        {
            // User hat abgelehnt — Dropdown auf den zuletzt synchronisierten
            // Server-Stand zurueckdrehen, ohne erneut den Change-Pfad zu triggern.
            _settingRecoveryModeInternally = true;
            try { RecoveryModeSelected = _lastSyncedRecoveryMode; }
            finally { _settingRecoveryModeInternally = false; }
            return;
        }

        RunSimpleAction("Set-DbRecoveryMode", db, $"-Recovery {newMode}", $"Recovery -> {newMode}");
        // Optimistisches Update — der naechste DB-Select holt den echten Stand neu.
        _lastSyncedRecoveryMode = newMode;
    }

    // --- Wartung (Phase 3.2, MSSQL-only via Invoke-DbMaintenance) ---

    [RelayCommand]
    private void RunCheckDb()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Invoke-DbMaintenance", db, "-CheckDb", "DBCC CHECKDB");
    }

    [RelayCommand]
    private void RunIndexRebuild()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Invoke-DbMaintenance", db, "-IndexRebuild", "Index-Rebuild");
    }

    [RelayCommand]
    private async Task RunShrinkLog()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;

        Window? owner = GetMainWindow();
        if (owner is null) return;

        ConfirmWindow dlg = new()
        {
            WindowTitle = "Logdatei verkleinern?",
            Message = $"Die Log-Datei der Datenbank „{db.Database.Name}\" wird per DBCC SHRINKFILE verkleinert.\n\n"
                    + "Die Funktion schaltet intern auf Recovery-Modus SIMPLE und wieder zurueck — "
                    + "dadurch wird die Log-Chain unterbrochen. Point-in-Time-Restore ab diesem Zeitpunkt "
                    + "ist erst nach dem naechsten Voll-Backup wieder moeglich.\n\nWirklich fortfahren?",
            ConfirmText = "Shrinken",
            CancelText = "Abbrechen",
        };

        bool ok = await dlg.ShowDialog<bool>(owner);
        if (!ok) return;

        RunSimpleAction("Invoke-DbMaintenance", db, "-ShrinkLog", "Shrink-Log");
    }

    // Backup-Browser: Dialog mit allen .bak-Dateien der selektierten MSSQL-DB,
    // mit Restore-Knopf (WITH REPLACE). MSSQL-only in v1.
    [RelayCommand]
    private async Task OpenBackupBrowser()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        if (db.ServerTyp != DB_SERVER.ServerTyp.MSSQL) return;

        Window? owner = GetMainWindow();
        if (owner is null || _services is null) return;

        BackupBrowserViewModel vm =
            _services.GetRequiredService<BackupBrowserViewModel>();
        BackupBrowserWindow dlg = new() { DataContext = vm };

        // Spinner ist sofort sichtbar, Daten laden parallel.
        _ = vm.LoadAsync(ModuleDatabaseId(db));

        await dlg.ShowDialog(owner);
    }

    [RelayCommand]
    private async Task ShowSessions()
    {
        Window? owner = GetMainWindow();
        if (owner is null) return;

        SessionsViewModel vm = ResolveOrNew<SessionsViewModel>();
        vm.SetSessions(_currentSessions);
        if (SelectedNode is DatabaseNodeViewModel db)
            vm.Configure(ModuleDatabaseId(db), db.Database.Name);
        SessionsWindow dlg = new SessionsWindow { DataContext = vm };
        await dlg.ShowDialog(owner);
    }

    private async Task<TimePickResult> PickTimeAsync()
    {
        Window? owner = GetMainWindow();
        if (owner is null) return TimePickResult.Cancel();

        TimePickerWindow dlg = new TimePickerWindow { DataContext = ResolveOrNew<TimePickerViewModel>() };
        return await dlg.ShowDialog<TimePickResult>(owner);
    }

    public async Task CheckForUpdateAsync()
    {
        try
        {
            string src = DTM.Data.Terminal.FocSqlRuntime.Current.UpdateSource;
            var newVersion = await DTM.Updater.UpdateService.CheckForUpdateAsync(src);
            if (newVersion is not null)
                await ShowUpdateDialogAsync(newVersion, src);
        }
        catch (Exception ex) { _logger.Warn(ex, "Update-Prüfung fehlgeschlagen."); }
    }

    private async Task ShowUpdateDialogAsync(Version newVersion, string updateSource)
    {
        Window? owner = GetMainWindow();
        if (owner is null) return;

        var dlg = new UpdatePromptWindow(newVersion.ToString(),
                                         DTM.Updater.UpdateService.CurrentVersion().ToString(3));
        await dlg.ShowDialog(owner);

        switch (dlg.Result)
        {
            case UpdateDialogResult.ApplyNow:
                _logger.Info("Update wird jetzt angewendet: {0}", newVersion);
                var applyProgress = new Progress<(int Done, int Total, string File)>(p =>
                    StatusBar = $"Update: {p.Done}/{p.Total} — {p.File}");
                await DTM.Updater.UpdateService.ApplyUpdateAsync(updateSource, applyProgress);
                break;
            case UpdateDialogResult.Later:
                _logger.Info("Update auf {0} auf später verschoben (30 min).", newVersion);
                _ = Task.Delay(TimeSpan.FromMinutes(30))
                        .ContinueWith(_ =>
                            Dispatcher.UIThread.InvokeAsync(() =>
                                ShowUpdateDialogAsync(newVersion, updateSource)));
                break;
            case UpdateDialogResult.Skip:
                _logger.Info("Update auf {0} für diese Sitzung übersprungen.", newVersion);
                break;
        }
    }

    private static Window? GetMainWindow()
        => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
