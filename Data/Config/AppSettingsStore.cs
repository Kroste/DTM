using System.Text.Json;
using SystemFile = System.IO.File;

namespace DTM.Config;

public static class AppSettingsStore
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DTM", "settings.json");

    public static FocSqlConfig LoadFocSql()
    {
        if (!SystemFile.Exists(_path)) return new FocSqlConfig();
        try
        {
            string json = SystemFile.ReadAllText(_path);
            return JsonSerializer.Deserialize<FocSqlConfig>(json) ?? new FocSqlConfig();
        }
        catch { return new FocSqlConfig(); }
    }

    public static void SaveFocSql(FocSqlConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        SystemFile.WriteAllText(_path, json);
    }
}
