namespace FFBWheelConfig.Models;

public sealed class WheelProfile
{
    public string Name { get; set; } = "Default";
    public WheelSettings Settings { get; set; } = new();
}
