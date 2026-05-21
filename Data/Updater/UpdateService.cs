using System.Diagnostics;
using System.Reflection;
using NLog;
using SystemFile = System.IO.File;

namespace DTM.Updater;

public static class UpdateService
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public static async Task<Version?> CheckForUpdateAsync(string updateSource)
    {
        if (string.IsNullOrWhiteSpace(updateSource)) return null;
        string versionFile = Path.Combine(updateSource, "version.txt");
        _logger.Debug("Prüfe Update-Quelle: {0}", updateSource);
        if (!SystemFile.Exists(versionFile))
        {
            _logger.Debug("Keine version.txt gefunden unter {0}", versionFile);
            return null;
        }
        string text = await SystemFile.ReadAllTextAsync(versionFile);
        if (!Version.TryParse(text.Trim(), out var remote)) return null;
        var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        if (remote > current)
        {
            _logger.Info("Update verfügbar: {0} → {1}", current, remote);
            return remote;
        }
        _logger.Debug("Kein Update: aktuelle Version {0} ist aktuell.", current);
        return null;
    }

    public static void ApplyUpdate(string updateSource)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "DTM_Update");
        string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        _logger.Info("Update wird angewendet: Quelle={0}, Ziel={1}", updateSource, exeDir);
        try
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            CopyDirectory(updateSource, tempDir);

            int pid = Environment.ProcessId;
            string scriptPath = Path.Combine(Path.GetTempPath(), "dtm_update.ps1");

            SystemFile.WriteAllText(scriptPath,
                $"$pid={pid}\n" +
                $"$src='{tempDir.Replace("'", "''")}'\n" +
                $"$dst='{exeDir.Replace("'", "''")}'\n" +
                "while (Get-Process -Id $pid -ErrorAction SilentlyContinue) { Start-Sleep 1 }\n" +
                "Copy-Item \"$src\\*\" $dst -Recurse -Force\n" +
                "Start-Process \"$dst\\DTM.exe\"\n");

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = true
            });

            _logger.Info("Update-Skript gestartet, Anwendung wird beendet.");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Update fehlgeschlagen.");
            throw;
        }
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (string file in Directory.GetFiles(src))
            SystemFile.Copy(file, Path.Combine(dst, Path.GetFileName(file)), overwrite: true);
        foreach (string dir in Directory.GetDirectories(src))
            CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
    }
}
