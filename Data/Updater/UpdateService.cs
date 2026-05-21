using System.Diagnostics;
using System.Reflection;
using NLog;
using SystemFile = System.IO.File;

namespace DTM.Updater;

public static class UpdateService
{
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Liest die laufende Version aus AssemblyInformationalVersion (&lt;Version&gt; in der csproj),
    /// damit das Format mit der version.txt übereinstimmt (z. B. "1.0.4").
    /// AssemblyVersion hat ein anderes Schema (1.0.0.4) und darf hier NICHT verwendet werden.
    /// </summary>
    public static Version CurrentVersion()
    {
        var raw = Assembly.GetExecutingAssembly()
                          .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                          ?.InformationalVersion ?? "1.0.0";
        return Version.TryParse(raw.Split('+')[0], out var v) ? v : new Version(1, 0, 0);
    }

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
        var current = CurrentVersion();
        if (remote > current)
        {
            _logger.Info("Update verfügbar: {0} → {1}", current, remote);
            return remote;
        }
        _logger.Debug("Kein Update: aktuelle Version {0} ist aktuell.", current);
        return null;
    }

    public static async Task ApplyUpdateAsync(string updateSource,
        IProgress<(int Done, int Total, string File)>? progress = null)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "DTM_Update");
        string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        _logger.Info("Update wird angewendet: Quelle={0}, Ziel={1}", updateSource, exeDir);
        try
        {
            var files = await Task.Run(() => CollectChangedFiles(updateSource, exeDir));
            _logger.Info("Update: {0} geänderte / neue Dateien werden kopiert.", files.Count);

            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            await Task.Run(() =>
            {
                for (int i = 0; i < files.Count; i++)
                {
                    string rel = Path.GetRelativePath(updateSource, files[i]);
                    string dst = Path.Combine(tempDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                    SystemFile.Copy(files[i], dst, overwrite: true);
                    progress?.Report((i + 1, files.Count, rel));
                    _logger.Debug("Kopiert: {0}", rel);
                }
            });

            int pid = Environment.ProcessId;
            string scriptPath = Path.Combine(Path.GetTempPath(), "dtm_update.ps1");

            // $dtmPid statt $pid: $PID ist in PowerShell eine automatische Variable
            // (PID des laufenden powershell.exe). Überschreiben ist zwar möglich,
            // aber fehleranfällig — eigener Name vermeidet jede Kollision.
            SystemFile.WriteAllText(scriptPath,
                $"$dtmPid={pid}\n" +
                $"$src='{tempDir.Replace("'", "''")}'\n" +
                $"$dst='{exeDir.Replace("'", "''")}'\n" +
                "while (Get-Process -Id $dtmPid -ErrorAction SilentlyContinue) { Start-Sleep 1 }\n" +
                "Copy-Item \"$src\\*\" $dst -Recurse -Force\n" +
                "Start-Process \"$dst\\DTM.exe\"\n");

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                UseShellExecute = true
            });

            // Environment.Exit(0) würde die Finalizer des eingebetteten PS-SDK-Runspace
            // aufrufen und damit unbegrenzt blockieren.
            // Process.Kill() schickt direkt TerminateProcess/SIGKILL — kein Finalizer, kein Hang.
            _logger.Info("Update-Skript gestartet, Anwendung wird beendet.");
            LogManager.Flush();
            LogManager.Shutdown();
            Process.GetCurrentProcess().Kill();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Update fehlgeschlagen.");
            throw;
        }
    }

    private static List<string> CollectChangedFiles(string sourceDir, string currentExeDir)
    {
        var result = new List<string>();
        CollectChangedFilesRecursive(sourceDir, sourceDir, currentExeDir, result);
        return result;
    }

    private static void CollectChangedFilesRecursive(
        string rootSource, string currentSource, string currentExeDir, List<string> result)
    {
        foreach (string srcFile in Directory.GetFiles(currentSource))
        {
            string rel = Path.GetRelativePath(rootSource, srcFile);
            string exeFile = Path.Combine(currentExeDir, rel);

            if (!SystemFile.Exists(exeFile))
            {
                result.Add(srcFile);
                continue;
            }

            var srcInfo = new FileInfo(srcFile);
            var dstInfo = new FileInfo(exeFile);
            if (srcInfo.Length != dstInfo.Length ||
                srcInfo.LastWriteTimeUtc > dstInfo.LastWriteTimeUtc)
                result.Add(srcFile);
        }
        foreach (string dir in Directory.GetDirectories(currentSource))
            CollectChangedFilesRecursive(rootSource, dir, currentExeDir, result);
    }
}
