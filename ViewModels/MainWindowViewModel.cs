using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DTM.ViewModels.TreeNodes;
using DTM.Views;
using NLog;

namespace DTM.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly IDTM_DATA _data;
    private readonly string _mssqlServer;

    public ObservableCollection<NodeViewModelBase> RootNodes { get; } = new();

    [ObservableProperty] private NodeViewModelBase? _selectedNode;

    [ObservableProperty] private string _dbName = "Datenbank: —";
    [ObservableProperty] private string _dbHost = "Host: —";
    [ObservableProperty] private string _dbStatus = "Status: —";
    [ObservableProperty] private string _dbVersion = "Version: —";
    [ObservableProperty] private string _dbSize = "Größe: —";
    [ObservableProperty] private string _recoveryOrArchiveMode = "RecoveryModel: —";
    [ObservableProperty] private string _activeSessionsLabel = "Aktive Sessions: —";
    [ObservableProperty] private string _statusBar = "Bereit";
    [ObservableProperty] private string _backupButtonText = "Backup";

    private List<Session> _currentSessions = new();

    // Initial-Setup der pwsh-Session:
    //  1. ExecutionPolicy für DIESEN Prozess auf Bypass (nur Runspace-lokal,
    //     ändert nichts am System, braucht keine Admin-Rechte). Sonst scheitert
    //     der Modul-Import an "running scripts is disabled on this system".
    //  2. credential.xml muss existieren (das FOC-SQL-Modul UND die interaktive
    //     $session brauchen sie). Fehlt sie → klare Meldung, Abbruch.
    //  3. FOC-SQL-Modul importieren — darüber laufen Backup/Clone/Snapshot.
    //  4. Persistente $session für interaktive User-Befehle (Get-Service etc.).
    public string ShellInitialCommand =>
        "try { Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force -ErrorAction Stop } catch {}; " +
        "if (-not (Test-Path \"$env:USERPROFILE\\credential.xml\")) { " +
        "  Write-Error 'credential.xml im Benutzerprofil fehlt. " +
        "Bitte einmalig erstellen: Get-Credential | Export-Clixml \"$env:USERPROFILE\\credential.xml\"'; " +
        "  return " +
        "}; " +
        DTM.Data.Terminal.FocSqlRuntime.BuildImportSnippet() + "; " +
        "Write-Host 'FOC-SQL Modul geladen.'; " +
        "$c = Import-Clixml \"$env:USERPROFILE\\credential.xml\"; " +
        $"$session = New-PSSession -ComputerName {_mssqlServer} -Credential $c; " +
        $"Write-Host \"Verbunden mit {_mssqlServer} — interaktive Befehle laufen via `$session\"";

    public MainWindowViewModel(IDTM_DATA data, Dictionary<DB_SERVER.ServerTyp, DB_SERVER> servers)
    {
        _data = data;
        _mssqlServer = servers.TryGetValue(DB_SERVER.ServerTyp.MSSQL, out var sv)
            ? sv.serverCredential?.Server ?? "FOC-SQL01"
            : "FOC-SQL01";
        foreach (var typ in servers.Keys)
            RootNodes.Add(new ServerNodeViewModel(typ, data));
    }

    partial void OnSelectedNodeChanged(NodeViewModelBase? value)
    {
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

    private void ApplyStats(Database_Stats stats)
    {
        _currentSessions = stats.Sessions ?? new List<Session>();
        ActiveSessionsLabel = $"Aktive Sessions: {_currentSessions.Count}";

        if (stats is Database_Stats_MSSQL m)
        {
            BackupButtonText = "Backup";
            DbName = $"Datenbank: {m.Name ?? "—"}";
            DbHost = $"Host: {m.Server ?? "—"}";
            DbStatus = $"Status: {m.State ?? "—"}";
            DbVersion = $"Comp. Lvl.: {m.CompatibllityLevel}";
            DbSize = $"Größe: {m.DataSizeMB} MB";
            RecoveryOrArchiveMode = $"RecoveryModel: {m.RecorveryModel ?? "—"}";
        }
        else if (stats is Database_Stats_ORACLE o)
        {
            BackupButtonText = "Dump";
            DbName = $"Instance: {o.InstanceName ?? "—"}";
            DbHost = $"Host: {o.Server ?? "—"}";
            DbStatus = $"Status: {o.State ?? "—"}";
            DbVersion = $"Version: {o.OracleVersion ?? "—"}";
            DbSize = $"Größe: {o.DataSizeMB} MB";
            RecoveryOrArchiveMode = $"ArchiveLog: {o.ArchiveLogMode ?? "—"}";
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
            database: db.Database.Name,
            when: when,
            title: $"{label} {db.Database.Name}",
            onUnavailable: () =>
                Dispatcher.UIThread.Post(() =>
                    StatusBar = $"{label} nicht möglich: pwsh-Tab ist nicht bereit."));

        StatusBar = $"{label} für {db.Database.Name} ausgelöst.";
    }

    [RelayCommand]
    private void DbToSamba()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Copy-Database-ToSamba", db, "", "DB → Samba");
    }

    [RelayCommand]
    private void RestoreSnapshot()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Restore-Snapshot", db, "", "Restore Snapshot");
    }

    [RelayCommand]
    private void RemoveSnapshot()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Remove-Snapshot", db, "", "Remove Snapshot");
    }

    [RelayCommand]
    private void ArchiveLogOn()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Set-Archive-Log", db, "", "ArchiveLog An");
    }

    [RelayCommand]
    private void ArchiveLogOff()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;
        RunSimpleAction("Set-Archive-Log", db, "-Off", "ArchiveLog Aus");
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
            database: db.Database.Name,
            extraArgs: extraArgs,
            title: $"{label} {db.Database.Name}",
            onUnavailable: () =>
                Dispatcher.UIThread.Post(() =>
                    StatusBar = $"{label} nicht möglich: pwsh-Tab ist nicht bereit."));
        StatusBar = $"{label} für {db.Database.Name} gestartet — siehe Shell-Tab.";
    }

    [RelayCommand]
    private async Task ShowSessions()
    {
        Window? owner = GetMainWindow();
        if (owner is null) return;

        SessionsViewModel vm = new SessionsViewModel();
        vm.SetSessions(_currentSessions);
        SessionsWindow dlg = new SessionsWindow { DataContext = vm };
        await dlg.ShowDialog(owner);
    }

    private async Task<TimePickResult> PickTimeAsync()
    {
        Window? owner = GetMainWindow();
        if (owner is null) return TimePickResult.Cancel();

        TimePickerWindow dlg = new TimePickerWindow { DataContext = new TimePickerViewModel() };
        return await dlg.ShowDialog<TimePickResult>(owner);
    }

    private static Window? GetMainWindow()
        => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
