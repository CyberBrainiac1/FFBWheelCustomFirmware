namespace FFBWheelConfig.Models;

public class WheelLiveState
{
    public double LiveAngle { get; set; } = 0.0;
    public int RawCounts { get; set; } = 0;
    public bool IsConnected { get; set; } = false;
    public string DeviceName { get; set; } = string.Empty;
}
