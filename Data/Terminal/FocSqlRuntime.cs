using DTM.Config;

namespace DTM.Data.Terminal;

/// <summary>
/// Statische Brücke für die FOC-SQL-Modul-Konfiguration, analog zu
/// <see cref="SshRuntimeConfig"/>. Wird beim App-Start gesetzt.
/// </summary>
public static class FocSqlRuntime
{
    private static FocSqlConfig _config = new();

    public static FocSqlConfig Current
    {
        get => _config;
        set => _config = value ?? new FocSqlConfig();
    }

    /// <summary>
    /// Baut das PowerShell-Snippet, das das FOC-SQL-Modul bereitstellt.
    /// Spiegelt die Profil-Logik: das Modul liegt auf Samba und wird in den
    /// User-PSModulePath kopiert, falls dort noch nicht vorhanden. Anschließend
    /// importiert. Bereits vorhandene/importierte Module werden nicht erneut
    /// geladen (Prüfung via Get-Module).
    ///
    /// Wenn <see cref="FocSqlConfig.ModulePath"/> gesetzt ist, wird stattdessen
    /// direkt dieser Pfad importiert (Override für Tests/Spezialfälle).
    /// Sonst kommt die Samba-Quelle aus <see cref="FocSqlConfig.SambaSource"/>.
    /// </summary>
    public static string BuildImportSnippet()
    {
        string explicitPath = _config.ModulePath?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(explicitPath))
        {
            string escaped = explicitPath.Replace("'", "''");
            return
                "if (-not (Get-Module FOC-SQL)) { " +
                $"Import-Module '{escaped}' -Force -DisableNameChecking }}";
        }

        string sambaGlob = (_config.SambaSource?.Trim() ?? DefaultSambaSource).Replace("'", "''");

        // Nachbau der Profil-Logik:
        //  - Ziel = erstes Element des PSModulePath (User-Modulpfad)
        //  - wenn FOC-SQL dort schon als Verzeichnis existiert ODER bereits
        //    importiert ist → nichts tun
        //  - sonst von Samba kopieren (ohne *_ToExport.ps1) und importieren
        return
            "$__moduleName = 'FOC-SQL'; " +
            "$__dest = ($env:PSModulePath -split ';')[0]; " +
            "$__already = (Get-Module $__moduleName) -or (Test-Path (Join-Path $__dest $__moduleName)); " +
            "if (-not $__already) { " +
            "  try { " +
            "    if (-not (Test-Path $__dest)) { New-Item -ItemType Directory -Path $__dest -Force | Out-Null } " +
            $"    Copy-Item -Path '{sambaGlob}' -Destination $__dest -Recurse -Force -Exclude *_ToExport.ps1 -ErrorAction Stop; " +
            "  } catch { Write-Error ('FOC-SQL Modul konnte nicht von Samba kopiert werden: ' + $_.Exception.Message) } " +
            "} " +
            "if (-not (Get-Module $__moduleName)) { " +
            "  Import-Module $__moduleName -Force -DisableNameChecking -ErrorAction SilentlyContinue " +
            "} " +
            "Remove-Variable __moduleName, __dest, __already -ErrorAction SilentlyContinue";
    }

    /// <summary>Default-Samba-Glob, falls in der Config nichts hinterlegt ist.</summary>
    private const string DefaultSambaSource =
        @"\\samba01\542$\5422_IT-Basis-Infrastruktur\MS-SQL\Powershell\Module\FOC*";

    /// <summary>
    /// Baut einen Aufruf einer FOC-SQL-Funktion mit -Database und optional
    /// -Time/-Date. Zeitparameter werden nur angehängt, wenn gesetzt.
    /// </summary>
    /// <param name="functionName">z.B. "Backup-Database".</param>
    /// <param name="database">Datenbankname.</param>
    /// <param name="when">
    /// Geplanter Zeitpunkt oder null = sofort. Wird in -Time 'HH:mm' -Date
    /// 'dd.MM.yyyy' übersetzt.
    /// </param>
    public static string BuildCall(string functionName, string database, DateTime? when)
    {
        string dbEsc = database.Replace("'", "''");
        var sb = new System.Text.StringBuilder();
        sb.Append(functionName);
        sb.Append(" -Database '").Append(dbEsc).Append('\'');

        if (when is { } w)
        {
            sb.Append(" -Time '").Append(w.ToString("HH:mm")).Append('\'');
            sb.Append(" -Date '").Append(w.ToString("dd.MM.yyyy")).Append('\'');
        }
        // when == null → keine Zeitparameter → Modul führt sofort aus.

        return sb.ToString();
    }
}
