using System.Text.Json;

namespace FFBWheelConfig.Services;

/// <summary>Persists user preferences between app sessions.</summary>
public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FFBWheelTester",
        "settings.json");

    public string LastPort { get; set; } = string.Empty;

    /// <summary>Force strength % used in test mode. Capped at 20 on first launch.</summary>
    public int TestForceStrength { get; set; } = 20;

    public bool StopOnKeyRelease { get; set; } = true;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) return s;
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
