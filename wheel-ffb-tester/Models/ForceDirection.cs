namespace FFBWheelTester.Models;

/// <summary>Which direction (or mode) the wheel is currently being commanded.</summary>
public enum ForceDirection
{
    Stopped,
    Left,
    Right,
    Center,
    PulseLeft,
    PulseRight,
    Oscillate,
}
