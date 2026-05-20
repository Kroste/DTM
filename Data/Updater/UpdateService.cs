using System.Diagnostics;
using System.Reflection;
using SystemFile = System.IO.File;

namespace DTM.Updater;

public static class UpdateService
{
    public static async Task<Version?> CheckForUpdateAsync(string updateSource)
    {
        if (string.IsNullOrWhiteSpace(updateSource)) return null;
        string versionFile = Path.Combine(updateSource, "version.txt");
        if (!SystemFile.Exists(versionFile)) return null;
        string text = await SystemFile.ReadAllTextAsync(versionFile);
        if (!Version.TryParse(text.Trim(), out var remote)) return null;
        var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        return remote > current ? remote : null;
    }

    public static void ApplyUpdate(string updateSource)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "DTM_Update");
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        CopyDirectory(updateSource, tempDir);

        string exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
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

        Environment.Exit(0);
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
