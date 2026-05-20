using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SystemFile = System.IO.File;

namespace DTM.Config;

public static class ConnectionStore
{
    internal static string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DTM", "connections.json");

    public static List<ConnectionEntry> Load()
    {
        if (!SystemFile.Exists(_path)) return [];
        try
        {
            string json = SystemFile.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<ConnectionEntry>>(json) ?? [];
        }
        catch { return []; }
    }

    public static void Save(List<ConnectionEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        SystemFile.WriteAllText(_path, json);
    }

    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        byte[] data = Encoding.UTF8.GetBytes(plainText);
#pragma warning disable CA1416
        if (OperatingSystem.IsWindows())
            return Convert.ToBase64String(ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser));
#pragma warning restore CA1416
        // Linux: Base64-only (kein OS-Schutz, aber kein Klartext im JSON)
        return Convert.ToBase64String(data);
    }

    public static string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText)) return string.Empty;
        try
        {
            byte[] data = Convert.FromBase64String(protectedText);
#pragma warning disable CA1416
            if (OperatingSystem.IsWindows())
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser));
#pragma warning restore CA1416
            return Encoding.UTF8.GetString(data);
        }
        catch { return string.Empty; }
    }
}
