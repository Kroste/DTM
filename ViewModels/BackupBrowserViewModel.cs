using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DTM.Data.Terminal;
using NLog;

namespace DTM.ViewModels;

/// <summary>
/// ViewModel fuer den Backup-Browser-Dialog. Laedt asynchron alle
/// Backup-Dateien der ausgewaehlten MSSQL-DB via <see cref="BackupBrowserService"/>.
/// Oracle wird in v1 nicht unterstuetzt — der Dialog wird fuer Oracle gar nicht
/// erst geoeffnet (Filter in MainWindowViewModel).
/// </summary>
public sealed partial class BackupBrowserViewModel : ViewModelBase
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly BackupBrowserService _service;

    [ObservableProperty] private string _databaseName = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _hasBackups;
    [ObservableProperty] private MssqlBackup? _selectedBackup;

    public ObservableCollection<MssqlBackup> Backups { get; } = new();

    public BackupBrowserViewModel(BackupBrowserService service)
    {
        _service = service;
    }

    /// <summary>
    /// Server-Hostname fuer den Restore-Aufruf (Multi-Server-Support).
    /// </summary>
    public string? ServerHost { get; set; }

    public async Task LoadAsync(string database, string? server = null)
    {
        DatabaseName = database;
        ServerHost = server;
        IsLoading = true;
        ErrorMessage = null;
        Backups.Clear();
        HasBackups = false;
        SelectedBackup = null;

        try
        {
            IReadOnlyList<MssqlBackup> list = await _service.FetchAsync(database, server);
            foreach (MssqlBackup b in list) Backups.Add(b);
            HasBackups = Backups.Count > 0;
            SelectedBackup = Backups.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Get-DbBackups fuer '{0}' fehlgeschlagen.", database);
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Setzt den eigentlichen Invoke-DbRestore-Aufruf in den pwsh-Tab ab.
    /// Bestaetigung passiert im Code-Behind (ConfirmWindow); diese Methode
    /// triggert die Aktion ohne weitere Rueckfrage.
    /// </summary>
    public void PerformRestore(MssqlBackup backup)
    {
        if (backup is null || string.IsNullOrWhiteSpace(DatabaseName)) return;

        // Invoke-DbRestore -Database '<db>' -BackupFile '<file>' [-Server '<host>']
        string dbEsc = DatabaseName.Replace("'", "''");
        string fileEsc = backup.Name.Replace("'", "''");
        string script = $"Invoke-DbRestore -Database '{dbEsc}' -BackupFile '{fileEsc}'";
        if (!string.IsNullOrWhiteSpace(ServerHost))
        {
            string srvEsc = ServerHost.Replace("'", "''");
            script += $" -Server '{srvEsc}'";
        }
        TerminalBus.SendScript(script);
    }
}
