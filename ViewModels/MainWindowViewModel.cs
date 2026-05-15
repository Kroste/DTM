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

    public MainWindowViewModel(IDTM_DATA data, IEnumerable<DB_SERVER.ServerTyp> serverTypes)
    {
        _data = data;
        foreach (var typ in serverTypes)
        {
            RootNodes.Add(new ServerNodeViewModel(typ, data));
        }
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
            var stats = await Task.Run(() => _data.get_Database_Stats(db.ServerTyp, db.Database));
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

        var when = await PickTimeAsync();
        if (when is null) return;

        StatusBar = $"Backup für {db.Database.Name} um {when:g} gestartet…";
        try
        {
            var ok = await Task.Run(() => _data.Backup_Database(db.ServerTyp, db.Database, when.Value));
            StatusBar = ok ? $"Backup für {db.Database.Name} ausgelöst." : $"Backup für {db.Database.Name} fehlgeschlagen.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Backup-Fehler für {db.Database.Name}.");
            StatusBar = $"Fehler: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Clone()
    {
        if (SelectedNode is not DatabaseNodeViewModel db) return;

        var when = await PickTimeAsync();
        if (when is null) return;

        StatusBar = $"Clone für {db.Database.Name} um {when:g} gestartet…";
        try
        {
            var ok = await Task.Run(() => _data.Clone_Database(db.ServerTyp, db.Database, when.Value));
            StatusBar = ok ? $"Clone für {db.Database.Name} ausgelöst." : $"Clone für {db.Database.Name} fehlgeschlagen.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Clone-Fehler für {db.Database.Name}.");
            StatusBar = $"Fehler: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Snapshot() => StatusBar = "Snapshot: noch nicht implementiert.";

    [RelayCommand]
    private void DbToSamba() => StatusBar = "DB → Samba: noch nicht implementiert.";

    [RelayCommand]
    private async Task ShowSessions()
    {
        var owner = GetMainWindow();
        if (owner is null) return;

        var vm = new SessionsViewModel();
        vm.SetSessions(_currentSessions);
        var dlg = new SessionsWindow { DataContext = vm };
        await dlg.ShowDialog(owner);
    }

    private async Task<DateTime?> PickTimeAsync()
    {
        var owner = GetMainWindow();
        if (owner is null) return null;

        var dlg = new TimePickerWindow { DataContext = new TimePickerViewModel() };
        return await dlg.ShowDialog<DateTime?>(owner);
    }

    private static Window? GetMainWindow()
        => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
