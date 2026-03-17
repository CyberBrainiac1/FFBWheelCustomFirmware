using FFBWheelConfig.Models;

namespace FFBWheelConfig.Services;

/// <summary>
/// Coordinates the serial client, protocol parser, and live-state polling timer.
/// All events are raised on a thread-pool thread; callers must marshal to the UI thread.
/// </summary>
public sealed class WheelControllerService : IDisposable
{
    private readonly SerialWheelClient _client = new();
    private readonly WheelProtocolParser _parser = new();
    private System.Timers.Timer? _liveTimer;
    private bool _disposed;

    // ── Public state ────────────────────────────────────────────────────────

    public WheelSettings? CurrentSettings { get; private set; }
    public WheelLiveState LiveState { get; } = new();
    public bool IsConnected => _client.IsConnected;

    // ── Events ──────────────────────────────────────────────────────────────

    /// <summary>Raised when a full settings block is received from the wheel.</summary>
    public event Action<WheelSettings>? SettingsUpdated;

    /// <summary>Raised every time a new live angle value arrives.</summary>
    public event Action<double>? LiveAngleUpdated;

    /// <summary>Raised every time a new raw encoder count arrives.</summary>
    public event Action<int>? RawCountsUpdated;

    /// <summary>Raised when the serial connection is lost unexpectedly.</summary>
    public event Action? Disconnected;

    // ── Constructor ──────────────────────────────────────────────────────────

    public WheelControllerService()
    {
        _parser.SettingsParsed += OnSettingsParsed;
        _parser.LiveStateParsed += OnLiveStateParsed;
        _parser.LiveAngleReceived += OnLiveAngleReceived;
        _parser.RawCountsReceived += OnRawCountsReceived;
        _client.LineReceived += _parser.ProcessLine;
        _client.Disconnected += OnClientDisconnected;
    }

    // ── Connection ───────────────────────────────────────────────────────────

    public bool Connect(string portName)
    {
        if (!_client.Connect(portName)) return false;
        LiveState.IsConnected = true;
        LiveState.DeviceName = portName;
        _parser.Reset();
        // Leonardo reboots when DTR goes high on connect.
        // Wait for the bootloader to finish before sending commands.
        Thread.Sleep(2500);
        _client.SetReady();
        StartLiveTimer();
        return true;
    }

    public void Disconnect()
    {
        // Safety: stop any test force before disconnecting
        if (IsConnected)
            _client.SendCommand("TEST_FORCE STOP");
        StopLiveTimer();
        _client.Disconnect();
        LiveState.IsConnected = false;
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public bool ReadSettings() =>
        IsConnected && _client.SendCommand("GET_SETTINGS");

    public bool ApplySettings(WheelSettings s)
    {
        if (!IsConnected) return false;
        bool ok = true;
        ok &= _client.SendCommand($"SET FORCE {s.OverallForce}");
        ok &= _client.SendCommand($"SET MIN_FORCE {s.MinimumForce}");
        ok &= _client.SendCommand($"SET DAMPING {s.Damping}");
        ok &= _client.SendCommand($"SET FRICTION {s.Friction}");
        ok &= _client.SendCommand($"SET SPRING {s.Spring}");
        ok &= _client.SendCommand($"SET RANGE {s.SteeringRange}");
        ok &= _client.SendCommand($"SET INV_ENCODER {(s.InvertEncoder ? 1 : 0)}");
        ok &= _client.SendCommand($"SET INV_MOTOR {(s.InvertMotor ? 1 : 0)}");
        ok &= _client.SendCommand("APPLY");
        return ok;
    }

    public bool SaveSettings() =>
        IsConnected && _client.SendCommand("SAVE");

    public bool ResetDefaults()
    {
        if (!IsConnected) return false;
        if (!_client.SendCommand("LOAD_DEFAULTS")) return false;
        _client.SendCommand("GET_SETTINGS");
        return true;
    }

    public bool SetCenter()
    {
        if (!IsConnected) return false;
        if (!_client.SendCommand("SET_CENTER_NOW")) return false;
        _client.SendCommand("GET_SETTINGS");
        return true;
    }

    // ── Test-force commands ──────────────────────────────────────────────────

    /// <summary>Sends a constant left force at the given strength (0-100 %).</summary>
    public bool TestForceLeft(int strengthPercent) =>
        IsConnected && _client.SendCommand($"TEST_FORCE LEFT {Math.Clamp(strengthPercent, 0, 100)}");

    /// <summary>Sends a constant right force at the given strength (0-100 %).</summary>
    public bool TestForceRight(int strengthPercent) =>
        IsConnected && _client.SendCommand($"TEST_FORCE RIGHT {Math.Clamp(strengthPercent, 0, 100)}");

    /// <summary>Activates spring-to-center mode.</summary>
    public bool TestForceCenter() =>
        IsConnected && _client.SendCommand("TEST_FORCE CENTER");

    /// <summary>Stops all test forces immediately.</summary>
    public bool TestForceStop() =>
        IsConnected && _client.SendCommand("TEST_FORCE STOP");


    public bool RequestLiveState() =>
        IsConnected && _client.SendCommand("GET_LIVE_STATE");

    public static string[] GetAvailablePorts() => SerialWheelClient.GetAvailablePorts();

    // ── Live-polling timer ───────────────────────────────────────────────────

    private void StartLiveTimer()
    {
        _liveTimer = new System.Timers.Timer(50) { AutoReset = true };
        _liveTimer.Elapsed += (_, _) => RequestLiveState();
        _liveTimer.Start();
    }

    private void StopLiveTimer()
    {
        _liveTimer?.Stop();
        _liveTimer?.Dispose();
        _liveTimer = null;
    }

    // ── Parser callbacks ─────────────────────────────────────────────────────

    private void OnSettingsParsed(WheelSettings settings)
    {
        CurrentSettings = settings;
        SettingsUpdated?.Invoke(settings);
    }

    private void OnLiveStateParsed(WheelLiveState state)
    {
        LiveState.LiveAngle = state.LiveAngle;
        LiveState.RawCounts = state.RawCounts;
        LiveAngleUpdated?.Invoke(state.LiveAngle);
        RawCountsUpdated?.Invoke(state.RawCounts);
    }

    private void OnLiveAngleReceived(double angle)
    {
        LiveState.LiveAngle = angle;
        LiveAngleUpdated?.Invoke(angle);
    }

    private void OnRawCountsReceived(int counts)
    {
        LiveState.RawCounts = counts;
        RawCountsUpdated?.Invoke(counts);
    }

    private void OnClientDisconnected()
    {
        LiveState.IsConnected = false;
        StopLiveTimer();
        Disconnected?.Invoke();
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopLiveTimer();
        _client.Dispose();
    }
}
