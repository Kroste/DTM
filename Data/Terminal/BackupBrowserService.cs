using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using NLog;

namespace DTM.Data.Terminal;

/// <summary>
/// Holt die Backup-Datei-Liste einer MSSQL-DB ueber einen eigenen
/// In-Process-PowerShell-Runspace (Get-DbBackups). Komplementaer zum
/// <see cref="TerminalBus"/>: der TerminalBus streamt Text in den pwsh-Tab,
/// fuer einen strukturierten DataGrid-Dialog brauchen wir aber das
/// PSCustomObject mit Name/Datum/Groesse.
///
/// Gleiches Pattern wie <see cref="OracleRestoreService"/>; Modul-Import
/// teilt sich beim ersten Aufruf die Samba-Copy-Zeit, Folgeaufrufe sind
/// schnell.
/// </summary>
public sealed class BackupBrowserService
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Importiert das FOC-SQL-Modul und ruft <c>Get-DbBackups -Database &lt;db&gt;</c>.
    /// Wirft <see cref="InvalidOperationException"/> bei Modul-Import- oder
    /// Cmdlet-Fehlern.
    /// </summary>
    public Task<IReadOnlyList<MssqlBackup>> FetchAsync(string database, CancellationToken ct = default)
    {
        return Task.Run<IReadOnlyList<MssqlBackup>>(() =>
        {
            ct.ThrowIfCancellationRequested();

            using PowerShell ps = PowerShell.Create();

            ps.AddScript(FocSqlRuntime.BuildImportSnippet()).Invoke();
            PowerShellDiagnostics.ThrowIfErrors(ps, "FOC-SQL Modul-Import");

            ct.ThrowIfCancellationRequested();
            ps.Commands.Clear();

            ps.AddCommand("Get-DbBackups")
              .AddParameter("Database", database);

            Collection<PSObject> results = ps.Invoke();
            PowerShellDiagnostics.ThrowIfErrors(ps, $"Get-DbBackups -Database '{database}'");

            List<MssqlBackup> backups = new();
            foreach (PSObject? item in results)
            {
                if (item is null) continue;
                backups.Add(new MssqlBackup(
                    Prop(item, "Name"),
                    PropDate(item, "LastWriteTime"),
                    PropLong(item, "Size"),
                    Prop(item, "Path")));
            }

            _logger.Info("Get-DbBackups({0}): {1} Backup-Datei(en)", database, backups.Count);
            return backups;
        }, ct);
    }

    private static string Prop(PSObject pso, string name) =>
        pso.Properties[name]?.Value?.ToString() ?? string.Empty;

    private static DateTime PropDate(PSObject pso, string name)
    {
        object? v = pso.Properties[name]?.Value;
        if (v is DateTime dt) return dt;
        if (v is string s && DateTime.TryParse(s, out DateTime parsed)) return parsed;
        return DateTime.MinValue;
    }

    private static long PropLong(PSObject pso, string name)
    {
        object? v = pso.Properties[name]?.Value;
        if (v is long l) return l;
        if (v is int i) return i;
        if (v is string s && long.TryParse(s, out long parsed)) return parsed;
        return 0;
    }
}
