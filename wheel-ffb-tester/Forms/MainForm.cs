using FFBWheelTester.Models;
using FFBWheelTester.Services;
using SharpDX.DirectInput;

namespace FFBWheelTester.Forms;

/// <summary>
/// Main window of the FFB Wheel Force Test Utility.
/// 760 × 520 px fixed dark-theme window.
/// </summary>
public sealed class MainForm : Form
{
    // ── Timing constants ──────────────────────────────────────────────────────
    private const int PulseIntervalMs     = 300;
    private const int OscillateIntervalMs = 400;

    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color C_Root    = ColorTranslator.FromHtml("#1E2124");
    private static readonly Color C_Panel   = ColorTranslator.FromHtml("#2A2D31");
    private static readonly Color C_Border  = ColorTranslator.FromHtml("#3A3D42");
    private static readonly Color C_Ctrl    = ColorTranslator.FromHtml("#1A1C1F");
    private static readonly Color C_Btn     = ColorTranslator.FromHtml("#3A3F46");
    private static readonly Color C_BtnBdr  = ColorTranslator.FromHtml("#555B64");
    private static readonly Color C_PrimBtn = ColorTranslator.FromHtml("#2F5F87");
    private static readonly Color C_PrimBdr = ColorTranslator.FromHtml("#4C83B1");
    private static readonly Color C_Text    = ColorTranslator.FromHtml("#E6E6E6");
    private static readonly Color C_Dim     = ColorTranslator.FromHtml("#9098A1");
    private static readonly Color C_Warn    = ColorTranslator.FromHtml("#E8C84C");
    private static readonly Color C_Stop    = ColorTranslator.FromHtml("#C94040");
    private static readonly Color C_StopHov = ColorTranslator.FromHtml("#E05050");
    private static readonly Color C_Active  = ColorTranslator.FromHtml("#4CAF50");
    private static readonly Color C_Left    = ColorTranslator.FromHtml("#5B9BD5");
    private static readonly Color C_Right   = ColorTranslator.FromHtml("#D57B5B");

    // ── Services ──────────────────────────────────────────────────────────────
    private readonly WheelTesterService _wheel   = new();
    private readonly FirmwareFlasher    _flasher = new();
    private readonly AppSettings        _settings;

    // ── Keyboard state ────────────────────────────────────────────────────────
    private bool _keyLeft, _keyRight;

    // ── Pulse / oscillate timers ──────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _pulseTimer;
    private readonly System.Windows.Forms.Timer _oscTimer;
    private bool _oscPhase;

    // ── Top-bar controls ──────────────────────────────────────────────────────
    private ComboBox   _cboDevice  = null!;
    private Button     _btnRefresh = null!;
    private Button     _btnConnect = null!;
    private Button     _btnFlash   = null!;
    private Label      _lblStatus  = null!;

    // ── Centre-panel controls ─────────────────────────────────────────────────
    private Label      _lblForce   = null!;
    private TrackBar   _slider     = null!;
    private NumericUpDown _numStr  = null!;
    private Button     _btnLeft    = null!;
    private Button     _btnRight   = null!;
    private Button     _btnCenter  = null!;
    private Button     _btnStop    = null!;
    private Button     _btnPulseL  = null!;
    private Button     _btnPulseR  = null!;
    private Button     _btnOsc     = null!;
    private Button     _btnPreStop = null!;
    private CheckBox   _chkStopKey = null!;

    // ── Bottom controls ───────────────────────────────────────────────────────
    private Button     _btnEStop   = null!;
    private Label      _lblLog     = null!;

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainForm()
    {
        _settings = AppSettings.Load();

        _pulseTimer = new System.Windows.Forms.Timer { Interval = PulseIntervalMs };
        _oscTimer   = new System.Windows.Forms.Timer { Interval = OscillateIntervalMs };

        _pulseTimer.Tick += OnPulseTick;
        _oscTimer.Tick   += OnOscTick;

        InitializeComponent();
        RefreshDevices();

        _slider.Value = _settings.TestStrength;
        _numStr.Value = _settings.TestStrength;
        _chkStopKey.Checked = _settings.StopOnKeyRelease;

        _flasher.Progress += line => SafeInvoke(() => Log(line));
    }

    // ── Form init ─────────────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        Text            = "FFB Wheel Force Test Utility";
        Size            = new Size(760, 520);
        MinimumSize     = Size;
        MaximumSize     = Size;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = C_Root;
        ForeColor       = C_Text;
        KeyPreview      = true;

        BuildTopBar();
        BuildCentrePanel();
        BuildBottomBar();
    }

    // ── TOP BAR ───────────────────────────────────────────────────────────────

    private void BuildTopBar()
    {
        var pnl = MakePanel(8, 8, 742, 56);
        Controls.Add(pnl);

        int x = 8;

        _cboDevice = new ComboBox
        {
            Left = x, Top = 14, Width = 240, Height = 26,
            BackColor = C_Ctrl, ForeColor = C_Text,
            FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f),
        };
        pnl.Controls.Add(_cboDevice);
        x += _cboDevice.Width + 6;

        _btnRefresh = MakeBtn("⟳ Refresh", x, 13, 80, 28, C_Btn, C_BtnBdr);
        _btnRefresh.Click += (_, _) => RefreshDevices();
        pnl.Controls.Add(_btnRefresh);
        x += _btnRefresh.Width + 6;

        _btnConnect = MakeBtn("Connect", x, 13, 90, 28, C_PrimBtn, C_PrimBdr);
        _btnConnect.Click += OnConnectClick;
        pnl.Controls.Add(_btnConnect);
        x += _btnConnect.Width + 6;

        _btnFlash = MakeBtn("Flash EMC Hex", x, 13, 120, 28, C_Btn, C_BtnBdr);
        _btnFlash.Click += OnFlashClick;
        pnl.Controls.Add(_btnFlash);
        x += _btnFlash.Width + 10;

        _lblStatus = new Label
        {
            Left = x, Top = 17, Width = 740 - x, Height = 20,
            Text = "Not connected", ForeColor = C_Dim,
            Font = new Font("Segoe UI", 9f), AutoSize = false,
        };
        pnl.Controls.Add(_lblStatus);
    }

    // ── CENTRE PANEL ──────────────────────────────────────────────────────────

    private void BuildCentrePanel()
    {
        var pnl = MakePanel(8, 72, 742, 360);
        Controls.Add(pnl);

        // Safety warning
        var warn = new Label
        {
            Left = 0, Top = 8, Width = 742, Height = 20, TextAlign = ContentAlignment.MiddleCenter,
            Text = "⚠  Keep hands clear during testing  ⚠",
            ForeColor = C_Warn, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.Transparent,
        };
        pnl.Controls.Add(warn);

        // Force status label
        _lblForce = new Label
        {
            Left = 0, Top = 34, Width = 742, Height = 48, TextAlign = ContentAlignment.MiddleCenter,
            Text = "STATUS:  STOPPED",
            ForeColor = C_Dim, Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
            BackColor = Color.Transparent,
        };
        pnl.Controls.Add(_lblForce);

        // Strength row
        var lblStr = new Label
        {
            Left = 30, Top = 94, Width = 110, Height = 20,
            Text = "Force Strength:", ForeColor = C_Dim,
            Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.MiddleLeft,
        };
        pnl.Controls.Add(lblStr);

        _slider = new TrackBar
        {
            Left = 148, Top = 89, Width = 380, Height = 32,
            Minimum = 0, Maximum = 100, TickFrequency = 10, SmallChange = 5,
            BackColor = C_Panel,
        };
        _slider.ValueChanged += OnStrengthChanged;
        pnl.Controls.Add(_slider);

        _numStr = new NumericUpDown
        {
            Left = 536, Top = 92, Width = 60, Height = 26,
            Minimum = 0, Maximum = 100,
            BackColor = C_Ctrl, ForeColor = C_Text,
            Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle,
        };
        _numStr.ValueChanged += OnNumStrChanged;
        pnl.Controls.Add(_numStr);

        var lblPct = new Label
        {
            Left = 600, Top = 94, Width = 30, Height = 20,
            Text = "%", ForeColor = C_Dim, Font = new Font("Segoe UI", 9f),
        };
        pnl.Controls.Add(lblPct);

        // Manual control buttons
        int by = 138, bw = 156, bh = 38, gap = 10;
        int totalW = bw * 4 + gap * 3;
        int bx = (742 - totalW) / 2;

        _btnLeft   = MakeBtn("◀  Turn Left",   bx,             by, bw, bh, C_Btn, C_BtnBdr);
        _btnRight  = MakeBtn("Turn Right  ▶",  bx + bw + gap,  by, bw, bh, C_Btn, C_BtnBdr);
        _btnCenter = MakeBtn("⊙  Center",       bx + (bw+gap)*2, by, bw, bh, C_Btn, C_BtnBdr);
        _btnStop   = MakeBtn("■  Stop Force",   bx + (bw+gap)*3, by, bw, bh, C_Btn, C_BtnBdr);

        _btnLeft.MouseDown   += (_, _) => SendForce(ForceCommand.Left(Strength));
        _btnLeft.MouseUp     += (_, _) => { if (_chkStopKey.Checked) SendForce(ForceCommand.Stop()); };
        _btnRight.MouseDown  += (_, _) => SendForce(ForceCommand.Right(Strength));
        _btnRight.MouseUp    += (_, _) => { if (_chkStopKey.Checked) SendForce(ForceCommand.Stop()); };
        _btnCenter.Click     += (_, _) => SendForce(ForceCommand.Center());
        _btnStop.Click       += (_, _) => SendForce(ForceCommand.Stop());

        pnl.Controls.AddRange([_btnLeft, _btnRight, _btnCenter, _btnStop]);

        // Preset buttons
        int py = 196, pw = 140, ph = 32, pgap = 8;
        int totalPW = pw * 4 + pgap * 3;
        int px = (742 - totalPW) / 2;

        var lblPreset = new Label
        {
            Left = 0, Top = py - 18, Width = 742, Height = 18,
            Text = "Test Presets", ForeColor = C_Dim, Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleCenter,
        };
        pnl.Controls.Add(lblPreset);

        _btnPulseL  = MakeBtn("Pulse Left",  px,                    py, pw, ph, C_Btn, C_BtnBdr);
        _btnPulseR  = MakeBtn("Pulse Right", px + pw + pgap,        py, pw, ph, C_Btn, C_BtnBdr);
        _btnOsc     = MakeBtn("Oscillate",   px + (pw+pgap)*2,      py, pw, ph, C_Btn, C_BtnBdr);
        _btnPreStop = MakeBtn("Stop",        px + (pw+pgap)*3,      py, pw, ph, C_Btn, C_BtnBdr);

        _btnPulseL.Click  += OnPulseLeft;
        _btnPulseR.Click  += OnPulseRight;
        _btnOsc.Click     += OnOscillate;
        _btnPreStop.Click += (_, _) => StopPresets();

        pnl.Controls.AddRange([_btnPulseL, _btnPulseR, _btnOsc, _btnPreStop]);

        // Stop-on-key-release checkbox
        _chkStopKey = new CheckBox
        {
            Left = px, Top = 244, Width = 260, Height = 22,
            Text = "Stop force on key / button release",
            ForeColor = C_Dim, Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent, Checked = true,
        };
        pnl.Controls.Add(_chkStopKey);

        // Keyboard help
        var kbHelp = new Label
        {
            Left = 0, Top = 276, Width = 742, Height = 72,
            TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent,
            ForeColor = C_Dim, Font = new Font("Segoe UI", 8.5f),
            Text  = "Keyboard:  ← Left force   → Right force   Space = stop   C = center\r\n" +
                    "           ↑ +5% strength   ↓ −5% strength",
        };
        pnl.Controls.Add(kbHelp);
    }

    // ── BOTTOM BAR ────────────────────────────────────────────────────────────

    private void BuildBottomBar()
    {
        var pnl = MakePanel(8, 440, 742, 44);
        Controls.Add(pnl);

        _btnEStop = new Button
        {
            Left = 8, Top = 7, Width = 160, Height = 30,
            Text = "🛑  EMERGENCY STOP",
            BackColor = C_Stop, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        };
        _btnEStop.FlatAppearance.BorderColor = C_Stop;
        _btnEStop.FlatAppearance.MouseOverBackColor = C_StopHov;
        _btnEStop.Click += (_, _) => { StopPresets(); EmergencyStop(); };
        pnl.Controls.Add(_btnEStop);

        _lblLog = new Label
        {
            Left = 178, Top = 11, Width = 554, Height = 22,
            Text = "Ready.", ForeColor = C_Dim,
            Font = new Font("Segoe UI", 8.5f), AutoEllipsis = true,
        };
        pnl.Controls.Add(_lblLog);
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private Panel MakePanel(int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Left = x, Top = y, Width = w, Height = h,
            BackColor = C_Panel,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(4),
        };
        // draw border manually
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(C_Border);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return p;
    }

    private static Button MakeBtn(string text, int x, int y, int w, int h, Color back, Color border)
    {
        var b = new Button
        {
            Left = x, Top = y, Width = w, Height = h,
            Text = text, FlatStyle = FlatStyle.Flat,
            BackColor = back, ForeColor = ColorTranslator.FromHtml("#E6E6E6"),
            Font = new Font("Segoe UI", 9f),
        };
        b.FlatAppearance.BorderColor = border;
        b.FlatAppearance.MouseOverBackColor = back.Lerp(Color.White, 0.12f);
        return b;
    }

    // ── Device refresh / connect ──────────────────────────────────────────────

    private void RefreshDevices()
    {
        _cboDevice.Items.Clear();

        var devices = _wheel.ListFfbDevices();
        foreach (var d in devices)
            _cboDevice.Items.Add(d);

        if (_cboDevice.Items.Count > 0)
            _cboDevice.SelectedIndex = 0;

        _cboDevice.DisplayMember = "ProductName";
        Log($"Found {devices.Count} FFB device(s).");
    }

    private void OnConnectClick(object? sender, EventArgs e)
    {
        if (_wheel.FfbConnected)
        {
            StopPresets();
            _wheel.DisconnectFfb();
            _btnConnect.Text = "Connect";
            SetStatus("Disconnected.", C_Dim);
            UpdateForceLabel(ForceDirection.Stopped);
            return;
        }

        if (_cboDevice.SelectedItem is not DeviceInstance dev)
        {
            Log("No FFB device selected.");
            return;
        }

        bool ok = _wheel.ConnectFfb(dev, Handle);
        if (ok)
        {
            _btnConnect.Text = "Disconnect";
            SetStatus($"Connected: {dev.ProductName}", C_Active);
            Log($"Connected to {dev.ProductName}");
        }
        else
        {
            SetStatus("Connection failed.", C_Stop);
            Log("Failed to acquire DirectInput device.");
        }
    }

    // ── Flash ─────────────────────────────────────────────────────────────────

    private void OnFlashClick(object? sender, EventArgs e)
    {
        string? hex = _flasher.FindHex();
        if (hex is null)
        {
            Log("Hex not found. Expected at: versions/1.2.2/firmware/leonardo-wheel.ino.hex");
            return;
        }

        // Ask for COM port
        string? port = PromptForPort();
        if (port is null) return;

        _btnFlash.Enabled = false;
        Log($"Flashing: {hex}");
        Log($"Target port: {port}");

        _ = Task.Run(async () =>
        {
            bool ok = await _flasher.FlashAsync(hex, port);
            SafeInvoke(() =>
            {
                _btnFlash.Enabled = true;
                Log(ok ? "Flash succeeded." : "Flash failed — check log above.");
            });
        });
    }

    private string? PromptForPort()
    {
        using var dlg = new Form
        {
            Text = "Select COM Port", Size = new Size(300, 130),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = C_Root, ForeColor = C_Text,
        };
        var cb = new ComboBox
        {
            Left = 20, Top = 20, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = C_Ctrl, ForeColor = C_Text, FlatStyle = FlatStyle.Flat,
        };
        cb.Items.AddRange(WheelTesterService.GetSerialPorts());
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        dlg.Controls.Add(cb);

        var ok = new Button
        {
            Left = 80, Top = 58, Width = 140, Height = 28,
            Text = "Flash", BackColor = C_PrimBtn, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK,
        };
        ok.FlatAppearance.BorderColor = C_PrimBdr;
        dlg.Controls.Add(ok);
        dlg.AcceptButton = ok;

        return dlg.ShowDialog(this) == DialogResult.OK && cb.SelectedItem is string p ? p : null;
    }

    // ── Force helpers ─────────────────────────────────────────────────────────

    private int Strength => _slider.Value;

    private void SendForce(ForceCommand cmd)
    {
        if (!_wheel.FfbConnected)
        {
            Log("Not connected.");
            return;
        }
        _wheel.SendForce(cmd);
        UpdateForceLabel(cmd.Direction);
        Log($"Force: {cmd}");
    }

    private void EmergencyStop()
    {
        _wheel.EmergencyStop();
        UpdateForceLabel(ForceDirection.Stopped);
        Log("EMERGENCY STOP.");
    }

    private void UpdateForceLabel(ForceDirection dir)
    {
        (string text, Color color) = dir switch
        {
            ForceDirection.Left      => ("STATUS:  LEFT FORCE",   C_Left),
            ForceDirection.Right     => ("STATUS:  RIGHT FORCE",  C_Right),
            ForceDirection.Center    => ("STATUS:  CENTERING",    C_Active),
            ForceDirection.PulseLeft => ("STATUS:  PULSE LEFT",   C_Left),
            ForceDirection.PulseRight=> ("STATUS:  PULSE RIGHT",  C_Right),
            ForceDirection.Oscillate => ("STATUS:  OSCILLATING",  C_Warn),
            _                        => ("STATUS:  STOPPED",      C_Dim),
        };
        _lblForce.Text      = text;
        _lblForce.ForeColor = color;
    }

    // ── Preset timers ─────────────────────────────────────────────────────────

    private void OnPulseLeft(object? sender, EventArgs e)
    {
        StopPresets();
        _btnPulseL.BackColor = C_PrimBtn;
        _pulseTimer.Tag = "left";
        _pulseTimer.Start();
        UpdateForceLabel(ForceDirection.PulseLeft);
    }

    private void OnPulseRight(object? sender, EventArgs e)
    {
        StopPresets();
        _btnPulseR.BackColor = C_PrimBtn;
        _pulseTimer.Tag = "right";
        _pulseTimer.Start();
        UpdateForceLabel(ForceDirection.PulseRight);
    }

    private void OnOscillate(object? sender, EventArgs e)
    {
        StopPresets();
        _btnOsc.BackColor = C_PrimBtn;
        _oscPhase = false;
        _oscTimer.Start();
        UpdateForceLabel(ForceDirection.Oscillate);
    }

    private bool _pulseOn;

    private void OnPulseTick(object? sender, EventArgs e)
    {
        _pulseOn = !_pulseOn;
        if (_pulseOn)
        {
            var dir = _pulseTimer.Tag as string == "right"
                ? ForceCommand.Right(Strength)
                : ForceCommand.Left(Strength);
            _wheel.SendForce(dir);
        }
        else
        {
            _wheel.SendForce(ForceCommand.Stop());
        }
    }

    private void OnOscTick(object? sender, EventArgs e)
    {
        _oscPhase = !_oscPhase;
        _wheel.SendForce(_oscPhase ? ForceCommand.Left(Strength) : ForceCommand.Right(Strength));
    }

    private void StopPresets()
    {
        _pulseTimer.Stop();
        _oscTimer.Stop();
        _btnPulseL.BackColor = C_Btn;
        _btnPulseR.BackColor = C_Btn;
        _btnOsc.BackColor    = C_Btn;
        _wheel.SendForce(ForceCommand.Stop());
        UpdateForceLabel(ForceDirection.Stopped);
    }

    // ── Keyboard ──────────────────────────────────────────────────────────────

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP   = 0x101;

        if (msg.Msg == WM_KEYDOWN)
        {
            switch (keyData)
            {
                case Keys.Left  when !_keyLeft:
                    _keyLeft = true;
                    StopPresets();
                    SendForce(ForceCommand.Left(Strength));
                    return true;
                case Keys.Right when !_keyRight:
                    _keyRight = true;
                    StopPresets();
                    SendForce(ForceCommand.Right(Strength));
                    return true;
                case Keys.Space:
                    StopPresets();
                    SendForce(ForceCommand.Stop());
                    return true;
                case Keys.C:
                    StopPresets();
                    SendForce(ForceCommand.Center());
                    return true;
                case Keys.Up:
                    _slider.Value = Math.Min(100, _slider.Value + 5);
                    return true;
                case Keys.Down:
                    _slider.Value = Math.Max(0, _slider.Value - 5);
                    return true;
            }
        }
        else if (msg.Msg == WM_KEYUP)
        {
            if (keyData == Keys.Left)
            {
                _keyLeft = false;
                if (_chkStopKey.Checked && !_keyRight)
                    SendForce(ForceCommand.Stop());
                return true;
            }
            if (keyData == Keys.Right)
            {
                _keyRight = false;
                if (_chkStopKey.Checked && !_keyLeft)
                    SendForce(ForceCommand.Stop());
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ── Strength sync ─────────────────────────────────────────────────────────

    private bool _syncingStrength;

    private void OnStrengthChanged(object? sender, EventArgs e)
    {
        if (_syncingStrength) return;
        _syncingStrength = true;
        _numStr.Value = _slider.Value;
        _syncingStrength = false;
    }

    private void OnNumStrChanged(object? sender, EventArgs e)
    {
        if (_syncingStrength) return;
        _syncingStrength = true;
        _slider.Value = (int)_numStr.Value;
        _syncingStrength = false;
    }

    // ── Log / status ──────────────────────────────────────────────────────────

    private void Log(string msg)     => _lblLog.Text = msg;
    private void SetStatus(string msg, Color c) { _lblStatus.Text = msg; _lblStatus.ForeColor = c; }

    // ── Form close ────────────────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopPresets();
        _wheel.EmergencyStop();
        _settings.TestStrength     = _slider.Value;
        _settings.StopOnKeyRelease = _chkStopKey.Checked;
        _settings.Save();
        _wheel.Dispose();
        _flasher.Progress -= line => SafeInvoke(() => Log(line));
        base.OnFormClosing(e);
    }

    // ── Thread helper ─────────────────────────────────────────────────────────

    private void SafeInvoke(Action a)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(a);
        else a();
    }
}

// ── Extension ─────────────────────────────────────────────────────────────────

file static class ColorEx
{
    internal static Color Lerp(this Color from, Color to, float t) =>
        Color.FromArgb(
            (int)(from.A + (to.A - from.A) * t),
            (int)(from.R + (to.R - from.R) * t),
            (int)(from.G + (to.G - from.G) * t),
            (int)(from.B + (to.B - from.B) * t));
}
