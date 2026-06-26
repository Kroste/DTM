using DTM.Config;
using DTM.Data.Terminal;
using DTM.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DTM.Composition;

/// <summary>
/// Composition-Root fuer DTM. Buendelt die Service- und ViewModel-
/// Registrierungen an einer Stelle und wird in <see cref="App.Initialize"/>
/// einmal aufgerufen.
///
/// Bewusste Abweichung von der Roadmap-Formulierung („+ Hosting"):
/// Microsoft.Extensions.Hosting wuerde IHost/IConfiguration/ILogger
/// mitbringen. Davon nutzt DTM nichts (NLog konfiguriert sich selbst,
/// die JSON-Stores haben ihr eigenes Schema, ein BackgroundService-
/// Lifecycle kollidiert mit Avalonias eigenem Lifecycle). Daher nur
/// das schlanke <c>Microsoft.Extensions.DependencyInjection</c>-Paket.
/// Falls spaeter Config/Logging via DI kommen, kann <c>HostBuilder</c>
/// jederzeit nachgezogen werden.
/// </summary>
internal static class ServiceRegistrations
{
    public static IServiceCollection AddDtmServices(this IServiceCollection services)
    {
        // --- Daten-/Infrastruktur-Schicht (Singletons) ---
        services.AddSingleton<ODBC_Factory>();

        services.AddSingleton<Dictionary<DB_SERVER.ServerTyp, DB_SERVER>>(_ =>
        {
            Dictionary<DB_SERVER.ServerTyp, DB_SERVER> dict = new();
            foreach (ConnectionEntry entry in ConnectionStore.Load())
            {
                if (Enum.TryParse<DB_SERVER.ServerTyp>(entry.Key, ignoreCase: true, out var typ))
                    dict[typ] = new DB_SERVER(entry.ToCredential());
            }
            return dict;
        });

        services.AddSingleton<IDTM_DATA>(sp =>
            new DTM_DATA(
                sp.GetRequiredService<Dictionary<DB_SERVER.ServerTyp, DB_SERVER>>(),
                sp.GetRequiredService<ODBC_Factory>()));

        // Strukturierte FOC-SQL-Aufrufe ueber eigenen PS-Runspace (komplementaer
        // zum TerminalBus, der Text in den pwsh-Tab schreibt).
        services.AddSingleton<OracleRestoreService>();
        services.AddSingleton<BackupBrowserService>();

        // --- ViewModels (Transient — neue Instanz pro Aufloesung) ---
        // MainWindowViewModel braucht den IServiceProvider, um untergeordnete
        // VMs (ConnectionManager, Sessions, TimePicker) zur Laufzeit aufzu-
        // loesen. Daher explizite Factory statt Default-Activator.
        services.AddTransient<MainWindowViewModel>(sp => new MainWindowViewModel(
            sp.GetRequiredService<IDTM_DATA>(),
            sp.GetRequiredService<Dictionary<DB_SERVER.ServerTyp, DB_SERVER>>(),
            sp));

        services.AddTransient<ConnectionManagerViewModel>();
        services.AddTransient<SessionsViewModel>();
        services.AddTransient<TimePickerViewModel>();
        services.AddTransient<OracleRestoreSelectViewModel>();
        services.AddTransient<BackupBrowserViewModel>();

        return services;
    }
}
