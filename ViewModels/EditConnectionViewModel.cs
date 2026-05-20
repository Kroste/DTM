using CommunityToolkit.Mvvm.ComponentModel;
using DTM.Config;

namespace DTM.ViewModels;

public sealed partial class EditConnectionViewModel : ViewModelBase
{
    public static IReadOnlyList<DB_SERVER.ServerTyp> ServerTypes { get; } =
        Enum.GetValues<DB_SERVER.ServerTyp>().ToArray();

    [ObservableProperty] private DB_SERVER.ServerTyp _selectedServerType = DB_SERVER.ServerTyp.MSSQL;
    [ObservableProperty] private string _server = string.Empty;
    [ObservableProperty] private string _user = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _database = "Master";
    [ObservableProperty] private string _connectionString = string.Empty;

    public EditConnectionViewModel() { }

    public EditConnectionViewModel(ConnectionEntry entry)
    {
        _selectedServerType = Enum.TryParse<DB_SERVER.ServerTyp>(entry.Key, out var t)
            ? t : DB_SERVER.ServerTyp.MSSQL;
        _server = entry.Server;
        _user = entry.User;
        _password = entry.PlainPassword;
        _database = entry.Database;
        _connectionString = entry.ConnectionString;
    }

    public ConnectionEntry ToEntry()
    {
        ConnectionEntry e = new()
        {
            Key = SelectedServerType.ToString(),
            Server = Server,
            User = User,
            Database = Database,
            ConnectionString = ConnectionString
        };
        e.PlainPassword = Password;
        return e;
    }
}
