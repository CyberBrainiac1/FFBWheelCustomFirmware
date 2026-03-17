using System.IO.Ports;
using FFBWheelTester.Models;
using SharpDX.DirectInput;

namespace FFBWheelTester.Services;

/// <summary>
/// High-level wheel test controller.
/// Holds both the DirectInput FFB device (for force output) and an optional
/// serial connection (for flashing, settings queries, live angle readback).
/// </summary>
public sealed class WheelTesterService : IDisposable
{
    private readonly DeviceManager _deviceManager = new();
    private readonly DirectInput   _di            = new();
    private FfbWheelDevice?        _ffbDevice;
    private SerialPort?            _serial;
    private bool                   _disposed;

    // ── State ────────────────────────────────────────────────────────────────

    public bool FfbConnected  => _ffbDevice is { IsDisposed: false };
    public bool SerialOpen    => _serial?.IsOpen ?? false;
    public string? SerialPort => _serial?.PortName;

    public ForceDirection CurrentForce { get; private set; } = ForceDirection.Stopped;

    // ── Device discovery ─────────────────────────────────────────────────────

    public IReadOnlyList<DeviceInstance> ListFfbDevices() =>
        _deviceManager.ListFfbJoysticks();

    // ── FFB connection ───────────────────────────────────────────────────────

    public bool ConnectFfb(DeviceInstance device, IntPtr windowHandle)
    {
        DisconnectFfb();
        try
        {
            _ffbDevice = new FfbWheelDevice(_di, device, windowHandle);
            return true;
        }
        catch
        {
            _ffbDevice = null;
            return false;
        }
    }

    public void DisconnectFfb()
    {
        if (_ffbDevice is not null)
        {
            _ffbDevice.ForceStop();
            _ffbDevice.Dispose();
            _ffbDevice = null;
        }
        CurrentForce = ForceDirection.Stopped;
    }

    // ── Serial connection ─────────────────────────────────────────────────────

    public bool OpenSerial(string portName)
    {
        CloseSerial();
        try
        {
            _serial = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
            {
                ReadTimeout  = 2000,
                WriteTimeout = 2000,
                DtrEnable    = false,
                RtsEnable    = false,
            };
            _serial.Open();
            return true;
        }
        catch
        {
            _serial?.Dispose();
            _serial = null;
            return false;
        }
    }

    public void CloseSerial()
    {
        if (_serial is { IsOpen: true })
        {
            try { _serial.Close(); } catch { }
        }
        _serial?.Dispose();
        _serial = null;
    }

    public static string[] GetSerialPorts() =>
        System.IO.Ports.SerialPort.GetPortNames()
            .OrderBy(p => p)
            .ToArray();

    // ── Force commands (via DirectInput FFB) ─────────────────────────────────

    public void SendForce(ForceCommand cmd)
    {
        if (_ffbDevice is null) return;

        switch (cmd.Direction)
        {
            case ForceDirection.Left:    _ffbDevice.ForceLeft(cmd.Strength);   break;
            case ForceDirection.Right:   _ffbDevice.ForceRight(cmd.Strength);  break;
            case ForceDirection.Center:  _ffbDevice.ForceCenter(cmd.Strength); break;
            case ForceDirection.Stopped: _ffbDevice.ForceStop();               break;
            default:                     _ffbDevice.ForceStop();               break;
        }

        CurrentForce = cmd.Direction;
    }

    public void EmergencyStop()
    {
        CurrentForce = ForceDirection.Stopped;
        _ffbDevice?.ForceStop();
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        EmergencyStop();
        DisconnectFfb();
        CloseSerial();
        _deviceManager.Dispose();
        _di.Dispose();
    }
}
