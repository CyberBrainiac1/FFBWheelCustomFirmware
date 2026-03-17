using SharpDX.DirectInput;

namespace FFBWheelTester.Services;

/// <summary>
/// Manages discovery and listing of HID force-feedback joystick devices
/// using DirectInput. Does NOT hold an open device handle — that is handled by
/// <see cref="FfbWheelDevice"/>.
/// </summary>
public sealed class DeviceManager : IDisposable
{
    private readonly DirectInput _di = new();

    /// <summary>
    /// Returns a snapshot of all attached force-feedback joystick devices.
    /// </summary>
    public IReadOnlyList<DeviceInstance> ListFfbJoysticks()
    {
        return _di
            .GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AttachedOnly)
            .Where(d => d.ForceFeedbackDriverGuid != Guid.Empty)
            .ToList();
    }

    public void Dispose() => _di.Dispose();
}
