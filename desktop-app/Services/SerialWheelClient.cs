using System.IO.Ports;
using System.Text;

namespace FFBWheelConfig.Services;

/// <summary>
/// Low-level serial port wrapper.  Fires LineReceived for each complete line
/// and Disconnected if the port disappears unexpectedly.
/// Uses a dedicated read thread instead of DataReceived event for reliability.
/// </summary>
public sealed class SerialWheelClient : IDisposable
{
    private SerialPort? _port;
    private Thread? _readThread;
    private readonly StringBuilder _buffer = new();
    private readonly object _writeLock = new();
    private volatile bool _running;
    private volatile bool _ready;
    private bool _disposed;

    public event Action<string>? LineReceived;
    public event Action? Disconnected;

    public bool IsConnected => _port?.IsOpen == true;

    public bool Connect(string portName, int baudRate = 115200)
    {
        try
        {
            Disconnect();
            _ready = false;
            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                DtrEnable = true,
                RtsEnable = true,
                Encoding = Encoding.ASCII,
                NewLine = "\n",
            };
            _port.Open();
            _running = true;
            _readThread = new Thread(ReadLoop)
            {
                IsBackground = true,
                Name = "SerialRead",
            };
            _readThread.Start();
            return true;
        }
        catch
        {
            _port?.Dispose();
            _port = null;
            return false;
        }
    }

    /// <summary>Call after the board has finished booting to start processing data.</summary>
    public void SetReady()
    {
        _buffer.Clear();
        _ready = true;
    }

    public void Disconnect()
    {
        _running = false;
        _ready = false;
        if (_port == null) return;
        try { if (_port.IsOpen) _port.Close(); } catch { }
        try { _port.Dispose(); } catch { }
        _port = null;
        _readThread = null;
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
                return false;
            }
        }
    }

    public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

    /// <summary>
    /// Triggers a Leonardo reset into bootloader mode by opening the
    /// port at 1200 baud and immediately closing it (1200-baud touch).
    /// </summary>
    public static void ResetToBootloader(string portName)
    {
        using var p = new SerialPort(portName, 1200)
        {
            DtrEnable = true,
            RtsEnable = true,
        };
        p.Open();
        Thread.Sleep(50);
        p.Close();
    }

    private void ReadLoop()
    {
        try
        {
            while (_running && _port is { IsOpen: true })
            {
                try
                {
                    int b = _port.ReadByte();
                    if (b < 0) continue;
                    char c = (char)b;

                    if (!_ready)
                        continue; // discard boot garbage

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
                catch (TimeoutException) { }
                catch (IOException) { break; }
                catch (InvalidOperationException) { break; }
            }
        }
        catch { }

        if (_running)
        {
            _running = false;
            Disconnected?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
    }
}
