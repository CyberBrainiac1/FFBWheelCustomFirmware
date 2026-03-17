using System.Text.Json;

namespace FFBWheelTester.Services;

/// <summary>Persists user preferences to %APPDATA%\FFBWheelTester\settings.json.</summary>
public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FFBWheelTester",
        "settings.json");

    public string  LastPort         { get; set; } = string.Empty;
    public int     TestStrength     { get; set; } = 20;   // safe default on first launch
    public bool    StopOnKeyRelease { get; set; } = true;

    // ── Persistence ──────────────────────────────────────────────────────────

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* first run / corrupt – use defaults */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* non-fatal */ }
    }
}
