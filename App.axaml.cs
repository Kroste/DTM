using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DTM.Config;
using DTM.ViewModels;
using DTM.Views;
using Microsoft.Extensions.Configuration;
using NLog;

namespace DTM;

public partial class App : Application
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

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
        AppSettings settings = LoadSettings();

        // SSH-Settings für SshKeyLocator hinterlegen (kein DI im Locator).
        DTM.Data.Terminal.SshRuntimeConfig.Current = settings.Ssh;
        // FOC-SQL-Modulpfad für den Backup/Clone/Snapshot-Pfad.
        DTM.Data.Terminal.FocSqlRuntime.Current = settings.FocSql;

        Dictionary<DB_SERVER.ServerTyp, DB_SERVER> servers = new Dictionary<DB_SERVER.ServerTyp, DB_SERVER>();

        foreach (var (key, cfg) in settings.Servers)
        {
            if (System.Enum.TryParse<DB_SERVER.ServerTyp>(key, ignoreCase: true, out var typ))
            {
                servers[typ] = new DB_SERVER(cfg.ToCredential());
            }
            else
            {
                _logger.Warn($"Unbekannter Servertyp in appsettings.json: '{key}' — wird ignoriert.");
            }
        }

        DTM_DATA data = new DTM_DATA(servers, new ODBC_Factory());
        return (servers, data);
    }

    private static AppSettings LoadSettings()
    {
        string baseDir = AppContext.BaseDirectory;
        string primary = Path.Combine(baseDir, "appsettings.json");
        string fallback = Path.Combine(baseDir, "appsettings.example.json");

        IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(baseDir);
        if (System.IO.File.Exists(primary))
        {
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        }
        else
        {
            _logger.Warn($"appsettings.json nicht gefunden — lade Beispieldaten aus appsettings.example.json. Pfad: {fallback}");
            builder.AddJsonFile("appsettings.example.json", optional: true, reloadOnChange: false);
        }

        IConfigurationRoot config = builder.Build();
        return config.Get<AppSettings>() ?? new AppSettings();
    }
}
