using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DTM.Config;

namespace DTM.ViewModels;

public sealed partial class ConnectionManagerViewModel : ViewModelBase
{
    public ObservableCollection<ConnectionEntry> Connections { get; } = [];

    [ObservableProperty] private ConnectionEntry? _selectedConnection;

    [ObservableProperty] private string _sambaSource = string.Empty;
    [ObservableProperty] private string _modulePath = string.Empty;

    public ConnectionManagerViewModel()
    {
        foreach (ConnectionEntry e in ConnectionStore.Load())
            Connections.Add(e);

        FocSqlConfig foc = AppSettingsStore.LoadFocSql();
        _sambaSource = foc.SambaSource;
        _modulePath = foc.ModulePath;
    }

    public void SaveFocSql()
    {
        AppSettingsStore.SaveFocSql(new FocSqlConfig
        {
            SambaSource = SambaSource,
            ModulePath = ModulePath
        });
    }

    public void AddEntry(ConnectionEntry entry)
    {
        Connections.Add(entry);
        SelectedConnection = entry;
        Save();
    }

    public void UpdateEntry(ConnectionEntry updated)
    {
        int idx = SelectedConnection is not null ? Connections.IndexOf(SelectedConnection) : -1;
        if (idx >= 0) Connections[idx] = updated;
        SelectedConnection = updated;
        Save();
    }

    public void DeleteSelected()
    {
        if (SelectedConnection is null) return;
        Connections.Remove(SelectedConnection);
        SelectedConnection = null;
        Save();
    }

    private void Save() => ConnectionStore.Save([.. Connections]);
}
