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
    public int EncoderCpr { get; set; } = 2400;
    public bool InvertEncoder { get; set; } = false;
    public bool InvertMotor { get; set; } = false;
    public string HBridgeMode { get; set; } = "2PWM + 1DIR";
    public string FirmwareVersion { get; set; } = string.Empty;
    public string ProductName { get; set; } = "EMC-compatible wheel";
    public string ProfileName { get; set; } = "EMC-style serial setup";
    public string UsbMode { get; set; } = "CDC config channel";
}
