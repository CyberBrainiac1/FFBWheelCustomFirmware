using System.IO.Ports;
using System.Text;

namespace FFBWheelConfig.Services;

/// <summary>
/// Low-level serial port wrapper.  Fires LineReceived for each complete line
/// and Disconnected if the port disappears unexpectedly.
/// </summary>
public sealed class SerialWheelClient : IDisposable
{
    private SerialPort? _port;
    private readonly StringBuilder _buffer = new();
    private readonly object _writeLock = new();
    private bool _disposed;

    public event Action<string>? LineReceived;
    public event Action? Disconnected;

    public bool IsConnected => _port?.IsOpen == true;

    public bool Connect(string portName, int baudRate = 115200)
    {
        try
        {
            Disconnect();
            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                DtrEnable = true,
                RtsEnable = true,
                Encoding = Encoding.ASCII,
                NewLine = "\n",
            };
            _port.DataReceived += OnDataReceived;
            _port.ErrorReceived += OnErrorReceived;
            _port.Open();
            return true;
        }
        catch
        {
            _port?.Dispose();
            _port = null;
            return false;
        }
    }

    public void Disconnect()
    {
        if (_port == null) return;
        try
        {
            _port.DataReceived -= OnDataReceived;
            _port.ErrorReceived -= OnErrorReceived;
            if (_port.IsOpen) _port.Close();
        }
        catch { }
        finally
        {
            _port.Dispose();
            _port = null;
        }
    }

    /// <summary>Sends a command line to the wheel (appends newline).</summary>
    public bool SendCommand(string command)
    {
        if (!IsConnected) return false;
        lock (_writeLock)
        {
            try
            {
                _port!.WriteLine(command);
                return true;
            }
            catch
            {
                HandleUnexpectedDisconnect();
                return false;
            }
        }
    }

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_port == null || !_port.IsOpen) return;
        try
        {
            string data = _port.ReadExisting();
            foreach (char c in data)
            {
                if (c == '\n')
                {
                    string line = _buffer.ToString().TrimEnd('\r');
                    _buffer.Clear();
                    if (!string.IsNullOrWhiteSpace(line))
                        LineReceived?.Invoke(line);
                }
                else
                {
                    _buffer.Append(c);
                }
            }
        }
        catch
        {
            HandleUnexpectedDisconnect();
        }
    }

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e) =>
        HandleUnexpectedDisconnect();

    private void HandleUnexpectedDisconnect()
    {
        Disconnect();
        Disconnected?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
    }
}
