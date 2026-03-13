using FFBWheelConfig.Models;

namespace FFBWheelConfig.Services;

/// <summary>
/// Parses text-based serial protocol messages from the wheel firmware.
/// Handles both structured block replies (BEGIN_SETTINGS...END_SETTINGS,
/// BEGIN_LIVE...END_LIVE) and standalone live-state lines (LIVE_ANGLE=, RAW_COUNTS=).
/// </summary>
public class WheelProtocolParser
{
    private readonly List<string> _blockLines = new();
    private bool _inSettingsBlock;
    private bool _inLiveBlock;

    public event Action<WheelSettings>? SettingsParsed;
    public event Action<WheelLiveState>? LiveStateParsed;
    public event Action<double>? LiveAngleReceived;
    public event Action<int>? RawCountsReceived;

    public void ProcessLine(string line)
    {
        line = line.Trim();
        if (string.IsNullOrEmpty(line)) return;

        switch (line)
        {
            case "BEGIN_SETTINGS":
                _inSettingsBlock = true;
                _blockLines.Clear();
                return;

            case "END_SETTINGS" when _inSettingsBlock:
                _inSettingsBlock = false;
                var settings = ParseSettingsBlock(_blockLines);
                if (settings != null) SettingsParsed?.Invoke(settings);
                _blockLines.Clear();
                return;

            case "BEGIN_LIVE":
                _inLiveBlock = true;
                _blockLines.Clear();
                return;

            case "END_LIVE" when _inLiveBlock:
                _inLiveBlock = false;
                var liveState = ParseLiveBlock(_blockLines);
                if (liveState != null) LiveStateParsed?.Invoke(liveState);
                _blockLines.Clear();
                return;
        }

        if (_inSettingsBlock || _inLiveBlock)
        {
            _blockLines.Add(line);
            return;
        }

        // Standalone live-angle line: LIVE_ANGLE=42.5
        if (line.StartsWith("LIVE_ANGLE=", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(
                    line["LIVE_ANGLE=".Length..],
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double angle))
            {
                LiveAngleReceived?.Invoke(angle);
            }
            return;
        }

        // Standalone raw-counts line: RAW_COUNTS=12054
        if (line.StartsWith("RAW_COUNTS=", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(line["RAW_COUNTS=".Length..], out int counts))
            {
                RawCountsReceived?.Invoke(counts);
            }
            return;
        }
    }

    private static WheelSettings? ParseSettingsBlock(List<string> lines)
    {
        var dict = ParseKeyValuePairs(lines);
        var s = new WheelSettings();

        if (dict.TryGetValue("FORCE", out string? v) && int.TryParse(v, out int i))
            s.OverallForce = Clamp(i, 0, 100);
        if (dict.TryGetValue("MIN_FORCE", out v) && int.TryParse(v, out i))
            s.MinimumForce = Clamp(i, 0, 100);
        if (dict.TryGetValue("DAMPING", out v) && int.TryParse(v, out i))
            s.Damping = Clamp(i, 0, 100);
        if (dict.TryGetValue("FRICTION", out v) && int.TryParse(v, out i))
            s.Friction = Clamp(i, 0, 100);
        if (dict.TryGetValue("SPRING", out v) && int.TryParse(v, out i))
            s.Spring = Clamp(i, 0, 100);
        if (dict.TryGetValue("RANGE", out v) && int.TryParse(v, out i))
            s.SteeringRange = Clamp(i, 90, 1800);
        if (dict.TryGetValue("CENTER", out v) && int.TryParse(v, out i))
            s.EncoderCenter = i;
        if (dict.TryGetValue("INV_ENCODER", out v))
            s.InvertEncoder = v == "1";
        if (dict.TryGetValue("INV_MOTOR", out v))
            s.InvertMotor = v == "1";
        if (dict.TryGetValue("FW_VERSION", out v))
            s.FirmwareVersion = v;

        return s;
    }

    private static WheelLiveState ParseLiveBlock(List<string> lines)
    {
        var dict = ParseKeyValuePairs(lines);
        var state = new WheelLiveState();

        if (dict.TryGetValue("LIVE_ANGLE", out string? v) &&
            double.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double angle))
            state.LiveAngle = angle;

        if (dict.TryGetValue("RAW_COUNTS", out v) && int.TryParse(v, out int counts))
            state.RawCounts = counts;

        return state;
    }

    private static Dictionary<string, string> ParseKeyValuePairs(List<string> lines)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            int idx = line.IndexOf('=');
            if (idx > 0)
                dict[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }
        return dict;
    }

    private static int Clamp(int value, int min, int max) =>
        value < min ? min : value > max ? max : value;

    public void Reset()
    {
        _inSettingsBlock = false;
        _inLiveBlock = false;
        _blockLines.Clear();
    }
}
