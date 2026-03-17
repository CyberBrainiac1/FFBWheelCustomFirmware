using System.Text.Json;
using FFBWheelConfig.Models;

namespace FFBWheelConfig.Services;

public sealed class ProfileStore
{
    private readonly string _filePath;

    public ProfileStore()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FFBWheelConfig");
        Directory.CreateDirectory(root);
        _filePath = Path.Combine(root, "profiles.json");
    }

    public IReadOnlyList<WheelProfile> LoadAll()
    {
        if (!File.Exists(_filePath))
            return Array.Empty<WheelProfile>();

        try
        {
            using FileStream stream = File.OpenRead(_filePath);
            List<WheelProfile>? profiles = JsonSerializer.Deserialize<List<WheelProfile>>(stream);
            return (profiles ?? new List<WheelProfile>())
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<WheelProfile>();
        }
    }

    public WheelProfile? Load(string name)
    {
        return LoadAll().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public void Save(string name, WheelSettings settings)
    {
        string profileName = NormalizeName(name);
        List<WheelProfile> profiles = LoadAll().ToList();
        WheelProfile? existing = profiles.FirstOrDefault(p => string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            profiles.Add(new WheelProfile
            {
                Name = profileName,
                Settings = CloneSettings(settings)
            });
        }
        else
        {
            existing.Settings = CloneSettings(settings);
        }

        WriteAll(profiles);
    }

    public bool Delete(string name)
    {
        string profileName = NormalizeName(name);
        List<WheelProfile> profiles = LoadAll().ToList();
        int removed = profiles.RemoveAll(p => string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
            return false;

        WriteAll(profiles);
        return true;
    }

    private void WriteAll(List<WheelProfile> profiles)
    {
        var ordered = profiles
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        using FileStream stream = File.Create(_filePath);
        JsonSerializer.Serialize(stream, ordered, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string NormalizeName(string? name)
    {
        string trimmed = (name ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "Default" : trimmed;
    }

    private static WheelSettings CloneSettings(WheelSettings settings)
    {
        return new WheelSettings
        {
            OverallForce = settings.OverallForce,
            MinimumForce = settings.MinimumForce,
            Damping = settings.Damping,
            Friction = settings.Friction,
            Spring = settings.Spring,
            SteeringRange = settings.SteeringRange,
            EncoderCenter = settings.EncoderCenter,
            EncoderCpr = settings.EncoderCpr,
            InvertEncoder = settings.InvertEncoder,
            InvertMotor = settings.InvertMotor,
            HBridgeMode = settings.HBridgeMode,
            FirmwareVersion = settings.FirmwareVersion,
            ProductName = settings.ProductName,
            ProfileName = settings.ProfileName,
            UsbMode = settings.UsbMode
        };
    }
}
