using System.Text.Json;

namespace NWNBuilder;

/// <summary>Lokale indstillinger i %APPDATA%\NWNBuilder\settings.json (Linux: ~/.config/NWNBuilder).</summary>
public sealed class AppSettings
{
    public string ConnectionString { get; set; } = "";

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NWNBuilder", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new()
                : new();
        }
        catch
        {
            return new();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
