namespace FFBWheelConfig.Models;

public class WheelSettings
{
    public int OverallForce { get; set; } = 60;
    public int MinimumForce { get; set; } = 5;
    public int Damping { get; set; } = 10;
    public int Friction { get; set; } = 4;
    public int Spring { get; set; } = 15;
    public int SteeringRange { get; set; } = 900;
    public int EncoderCenter { get; set; } = 0;
    public bool InvertEncoder { get; set; } = false;
    public bool InvertMotor { get; set; } = false;
    public string FirmwareVersion { get; set; } = string.Empty;
}
