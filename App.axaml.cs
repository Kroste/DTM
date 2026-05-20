using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DTM.Config;
using DTM.ViewModels;
using DTM.Views;

namespace DTM;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            (Dictionary<DB_SERVER.ServerTyp, DB_SERVER>? servers, IDTM_DATA? dtmData) = BuildDataLayer();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(dtmData, servers)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static (Dictionary<DB_SERVER.ServerTyp, DB_SERVER> servers, IDTM_DATA data) BuildDataLayer()
    {
        DTM.Data.Terminal.FocSqlRuntime.Current = AppSettingsStore.LoadFocSql();

        Dictionary<DB_SERVER.ServerTyp, DB_SERVER> servers = new();
        foreach (ConnectionEntry entry in ConnectionStore.Load())
        {
            if (System.Enum.TryParse<DB_SERVER.ServerTyp>(entry.Key, ignoreCase: true, out var typ))
                servers[typ] = new DB_SERVER(entry.ToCredential());
        }

        return (servers, new DTM_DATA(servers, new ODBC_Factory()));
    }
}
