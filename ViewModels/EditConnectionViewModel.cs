using CommunityToolkit.Mvvm.ComponentModel;
using DTM.Config;

namespace DTM.ViewModels;

public sealed partial class EditConnectionViewModel : ViewModelBase
{
    [ObservableProperty] private string _key = string.Empty;
    [ObservableProperty] private string _server = string.Empty;
    [ObservableProperty] private string _user = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _database = "Master";
    [ObservableProperty] private string _connectionString = string.Empty;

    public EditConnectionViewModel() { }

    public EditConnectionViewModel(ConnectionEntry entry)
    {
        _key = entry.Key;
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
            Key = Key,
            Server = Server,
            User = User,
            Database = Database,
            ConnectionString = ConnectionString
        };
        e.PlainPassword = Password;
        return e;
    }
}
