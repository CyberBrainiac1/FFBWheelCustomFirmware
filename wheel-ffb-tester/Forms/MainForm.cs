using FFBWheelTester.Models;
using FFBWheelTester.Services;
using SharpDX.DirectInput;

namespace FFBWheelTester.Forms;

/// <summary>
/// Main window of the EMC FFB Tester utility.
/// 780 × 560 px fixed dark-theme window.
/// </summary>
public sealed class MainForm : Form
{
    // ── Timing constants ──────────────────────────────────────────────────────
    private const int PulseIntervalMs     = 300;
    private const int OscillateIntervalMs = 400;

    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly Color C_Root    = ColorTranslator.FromHtml("#202225");
    private static readonly Color C_Panel   = ColorTranslator.FromHtml("#2A2D31");
    private static readonly Color C_Border  = ColorTranslator.FromHtml("#3A3D42");
    private static readonly Color C_Ctrl    = ColorTranslator.FromHtml("#1F2124");
    private static readonly Color C_Btn     = ColorTranslator.FromHtml("#3A3F46");
    private static readonly Color C_BtnBdr  = ColorTranslator.FromHtml("#555B64");
    private static readonly Color C_PrimBtn = ColorTranslator.FromHtml("#2F5F87");
    private static readonly Color C_PrimBdr = ColorTranslator.FromHtml("#4C83B1");
    private static readonly Color C_DangerBtn = ColorTranslator.FromHtml("#6E3A3A");
    private static readonly Color C_EmergBtn  = ColorTranslator.FromHtml("#8B2E2E");
    private static readonly Color C_EmergBdr  = ColorTranslator.FromHtml("#B44A4A");
    private static readonly Color C_Error     = ColorTranslator.FromHtml("#C94040");
    private static readonly Color C_Text    = ColorTranslator.FromHtml("#E6E6E6");
    private static readonly Color C_Dim     = ColorTranslator.FromHtml("#C8CDD4");
    private static readonly Color C_DimAlt  = ColorTranslator.FromHtml("#AAB1B9");
    private static readonly Color C_Warn    = ColorTranslator.FromHtml("#D8B86A");
    private static readonly Color C_Stop    = ColorTranslator.FromHtml("#8B2E2E");
    private static readonly Color C_StopHov = ColorTranslator.FromHtml("#B44A4A");
    private static readonly Color C_Active  = ColorTranslator.FromHtml("#4CAF50");
    private static readonly Color C_ForceTxt = ColorTranslator.FromHtml("#EAF6FF");
    private static readonly Color C_Left    = ColorTranslator.FromHtml("#5B9BD5");
    private static readonly Color C_Right   = ColorTranslator.FromHtml("#D57B5B");

    // ── Services ──────────────────────────────────────────────────────────────
    private readonly WheelTesterService _wheel;
    private readonly AppSettings        _settings;

    // ── Keyboard state ────────────────────────────────────────────────────────
    private bool _keyLeft, _keyRight;

    // ── Pulse / oscillate timers ──────────────────────────────────────────────
    private readonly System.Windows.Forms.Timer _pulseTimer;
    private readonly System.Windows.Forms.Timer _oscTimer;
    private bool _oscPhase;
    private bool _pulseOn;

    // ── Top-bar controls ──────────────────────────────────────────────────────
    private ComboBox   _cboDevice     = null!;
    private Button     _btnRefresh    = null!;
    private Button     _btnConnect    = null!;
    private Button     _btnDisconnect = null!;
    private Label      _lblSelected   = null!;
    private Label      _lblConnStatus = null!;

    // ── Main force panel controls ─────────────────────────────────────────────
    private Label         _lblForce   = null!;
    private TrackBar      _slider     = null!;
    private NumericUpDown _numStr     = null!;
    private Button        _btnLeft    = null!;
    private Button        _btnRight   = null!;
    private Button        _btnCenter  = null!;
    private Button        _btnStop    = null!;
    private CheckBox      _chkStopKey = null!;

    // ── Preset panel controls ─────────────────────────────────────────────────
    private Button _btnConstL  = null!;
    private Button _btnConstR  = null!;
    private Button _btnCenterSpr = null!;
    private Button _btnPulseL  = null!;
    private Button _btnPulseR  = null!;
    private Button _btnOsc     = null!;
    private Button _btnPreStop = null!;

    // ── Bottom controls ───────────────────────────────────────────────────────
    private Button _btnEStop   = null!;
    private Label  _lblStatus  = null!;

    // ── Constructor ───────────────────────────────────────────────────────────

    public MainForm()
    {
        _wheel    = new WheelTesterService();
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
    }

    // ── Form init ─────────────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        Text            = "EMC FFB Tester";
        Size            = new Size(780, 560);
        MinimumSize     = Size;
        MaximumSize     = Size;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = C_Root;
        ForeColor       = C_Text;
        Font            = new Font("Segoe UI", 9f);
        KeyPreview      = true;
        StartPosition   = FormStartPosition.CenterScreen;

        BuildTopBar();
        BuildForcePanel();
        BuildPresetPanel();
        BuildBottomBar();
    }

    // ── SECTION 1: TOP DEVICE BAR ────────────────────────────────────────────
    // X:16, Y:12, W:748, H:70

    private void BuildTopBar()
    {
        var pnl = MakePanel(16, 12, 748, 70);
        Controls.Add(pnl);

        // Device label  X:12, Y:16, W:74, H:20
        var lblDevice = MakeLabel("Device", 12, 16, 74, 20);
        pnl.Controls.Add(lblDevice);

        // Device dropdown  X:86, Y:12, W:170, H:28
        _cboDevice = new ComboBox
        {
            Left = 86, Top = 12, Width = 170, Height = 28,
            BackColor = C_Ctrl, ForeColor = C_Text,
            FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f),
        };
        _cboDevice.SelectedIndexChanged += OnDeviceSelectionChanged;
        pnl.Controls.Add(_cboDevice);

        // Refresh  X:268, Y:12, W:82, H:28
        _btnRefresh = MakeBtn("Refresh", 268, 12, 82, 28, C_Btn, C_BtnBdr);
        _btnRefresh.Click += (_, _) => RefreshDevices();
        pnl.Controls.Add(_btnRefresh);

        // Connect  X:360, Y:12, W:78, H:28
        _btnConnect = MakeBtn("Connect", 360, 12, 78, 28, C_PrimBtn, C_PrimBdr);
        _btnConnect.Click += OnConnectClick;
        pnl.Controls.Add(_btnConnect);

        // Disconnect  X:448, Y:12, W:88, H:28
        _btnDisconnect = MakeBtn("Disconnect", 448, 12, 88, 28, C_Btn, C_BtnBdr);
        _btnDisconnect.Enabled = false;
        _btnDisconnect.Click += OnDisconnectClick;
        pnl.Controls.Add(_btnDisconnect);

        // Selected device label  X:12, Y:46, W:420, H:16
        _lblSelected = MakeLabel("Selected: —", 12, 46, 420, 16, C_DimAlt);
        pnl.Controls.Add(_lblSelected);

        // Connection status label  X:548, Y:46, W:180, H:16
        _lblConnStatus = MakeLabel("Status: Not Connected", 548, 46, 180, 16, C_DimAlt);
        pnl.Controls.Add(_lblConnStatus);
    }

    // ── SECTION 2: MAIN FORCE PANEL ──────────────────────────────────────────
    // X:16, Y:92, W:748, H:190

    private void BuildForcePanel()
    {
        var pnl = MakePanel(16, 92, 748, 190);
        Controls.Add(pnl);

        // Title  X:12, Y:8, W:180, H:18
        var title = MakeLabel("Manual Force Control", 12, 8, 180, 18);
        title.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        pnl.Controls.Add(title);

        // Large force status label  X:20, Y:36, W:320, H:72
        _lblForce = new Label
        {
            Left = 20, Top = 36, Width = 320, Height = 72,
            Text = "STATUS: STOPPED",
            ForeColor = C_ForceTxt,
            Font = new Font("Segoe UI Semibold", 28f, FontStyle.Bold),
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        pnl.Controls.Add(_lblForce);

        // Force strength label  X:390, Y:36, W:120, H:20
        var lblStr = MakeLabel("Force Strength", 390, 36, 120, 20, C_Dim);
        pnl.Controls.Add(lblStr);

        // Force strength slider  X:390, Y:64, W:220, H:28
        _slider = new TrackBar
        {
            Left = 390, Top = 64, Width = 220, Height = 28,
            Minimum = 0, Maximum = 100, TickFrequency = 10, SmallChange = 5,
            BackColor = C_Panel,
        };
        _slider.ValueChanged += OnStrengthChanged;
        pnl.Controls.Add(_slider);

        // Force strength numeric box  X:624, Y:62, W:56, H:28
        _numStr = new NumericUpDown
        {
            Left = 624, Top = 62, Width = 56, Height = 28,
            Minimum = 0, Maximum = 100,
            BackColor = C_Ctrl, ForeColor = C_Text,
            Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle,
        };
        _numStr.ValueChanged += OnNumStrChanged;
        pnl.Controls.Add(_numStr);

        // Percent suffix label  X:686, Y:66, W:24, H:20
        var lblPct = MakeLabel("%", 686, 66, 24, 20, C_Dim);
        pnl.Controls.Add(lblPct);

        // Manual buttons row
        // Turn Left  X:24, Y:120, W:130, H:38
        _btnLeft = MakeBtn("Turn Left", 24, 120, 130, 38, C_Btn, C_BtnBdr);
        _btnLeft.MouseDown += (_, _) => SendForce(ForceCommand.Left(Strength));
        _btnLeft.MouseUp   += (_, _) => { if (_chkStopKey.Checked) SendForce(ForceCommand.Stop()); };
        pnl.Controls.Add(_btnLeft);

        // Turn Right  X:164, Y:120, W:130, H:38
        _btnRight = MakeBtn("Turn Right", 164, 120, 130, 38, C_Btn, C_BtnBdr);
        _btnRight.MouseDown += (_, _) => SendForce(ForceCommand.Right(Strength));
        _btnRight.MouseUp   += (_, _) => { if (_chkStopKey.Checked) SendForce(ForceCommand.Stop()); };
        pnl.Controls.Add(_btnRight);

        // Center  X:304, Y:120, W:110, H:38
        _btnCenter = MakeBtn("Center", 304, 120, 110, 38, C_Btn, C_BtnBdr);
        _btnCenter.Click += (_, _) => SendForce(ForceCommand.Center());
        pnl.Controls.Add(_btnCenter);

        // Stop Force  X:424, Y:120, W:130, H:38
        _btnStop = MakeBtn("Stop Force", 424, 120, 130, 38, C_DangerBtn, C_BtnBdr);
        _btnStop.Click += (_, _) => { StopPresets(); SendForce(ForceCommand.Stop()); };
        pnl.Controls.Add(_btnStop);

        // Stop on key release checkbox  X:580, Y:126, W:160, H:24
        _chkStopKey = new CheckBox
        {
            Left = 580, Top = 126, Width = 160, Height = 24,
            Text = "Stop on key release",
            ForeColor = C_Dim, Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent, Checked = true,
        };
        pnl.Controls.Add(_chkStopKey);

        // Warning label  X:24, Y:166, W:400, H:16
        var warn = new Label
        {
            Left = 24, Top = 166, Width = 400, Height = 16,
            Text = "Keep hands clear during testing",
            ForeColor = C_Warn, Font = new Font("Segoe UI", 8f),
            BackColor = Color.Transparent, AutoSize = false,
        };
        pnl.Controls.Add(warn);
    }

    // ── SECTION 3: PRESET TEST PANEL ─────────────────────────────────────────
    // X:16, Y:292, W:748, H:130

    private void BuildPresetPanel()
    {
        var pnl = MakePanel(16, 292, 748, 130);
        Controls.Add(pnl);

        // Title  X:12, Y:8, W:120, H:18
        var title = MakeLabel("Test Presets", 12, 8, 120, 18);
        title.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        pnl.Controls.Add(title);

        // Preset buttons row 1  Y:42
        // Constant Left  X:20, Y:42, W:120, H:34
        _btnConstL = MakeBtn("Constant Left", 20, 42, 120, 34, C_Btn, C_BtnBdr);
        _btnConstL.Click += (_, _) => { StopPresets(); SendForce(ForceCommand.Left(Strength)); UpdateForceLabel(ForceDirection.Left); };
        pnl.Controls.Add(_btnConstL);

        // Constant Right  X:150, Y:42, W:120, H:34
        _btnConstR = MakeBtn("Constant Right", 150, 42, 120, 34, C_Btn, C_BtnBdr);
        _btnConstR.Click += (_, _) => { StopPresets(); SendForce(ForceCommand.Right(Strength)); UpdateForceLabel(ForceDirection.Right); };
        pnl.Controls.Add(_btnConstR);

        // Center Spring  X:280, Y:42, W:120, H:34
        _btnCenterSpr = MakeBtn("Center Spring", 280, 42, 120, 34, C_Btn, C_BtnBdr);
        _btnCenterSpr.Click += (_, _) => { StopPresets(); SendForce(ForceCommand.Center()); UpdateForceLabel(ForceDirection.Center); };
        pnl.Controls.Add(_btnCenterSpr);

        // Pulse Left  X:410, Y:42, W:100, H:34
        _btnPulseL = MakeBtn("Pulse Left", 410, 42, 100, 34, C_Btn, C_BtnBdr);
        _btnPulseL.Click += OnPulseLeft;
        pnl.Controls.Add(_btnPulseL);

        // Pulse Right  X:520, Y:42, W:100, H:34
        _btnPulseR = MakeBtn("Pulse Right", 520, 42, 100, 34, C_Btn, C_BtnBdr);
        _btnPulseR.Click += OnPulseRight;
        pnl.Controls.Add(_btnPulseR);

        // Oscillate  X:630, Y:42, W:90, H:34
        _btnOsc = MakeBtn("Oscillate", 630, 42, 90, 34, C_Btn, C_BtnBdr);
        _btnOsc.Click += OnOscillate;
        pnl.Controls.Add(_btnOsc);

        // Stop  X:20, Y:84, W:120, H:30
        _btnPreStop = MakeBtn("Stop", 20, 84, 120, 30, C_Btn, C_BtnBdr);
        _btnPreStop.Click += (_, _) => StopPresets();
        pnl.Controls.Add(_btnPreStop);

        // Description text  X:160, Y:90, W:560, H:16
        var desc = new Label
        {
            Left = 160, Top = 90, Width = 560, Height = 16,
            Text = "Presets apply simple test effects only. Start at low force.",
            ForeColor = C_DimAlt, Font = new Font("Segoe UI", 8f),
            BackColor = Color.Transparent, AutoSize = false,
        };
        pnl.Controls.Add(desc);
    }

    // ── SECTION 4: BOTTOM STATUS / HELP / EMERGENCY BAR ─────────────────────
    // X:16, Y:432, W:748, H:80

    private void BuildBottomBar()
    {
        var pnl = MakePanel(16, 432, 748, 80);
        Controls.Add(pnl);

        // Keyboard shortcut help  X:12, Y:10, W:500, H:16
        var kbHelp = new Label
        {
            Left = 12, Top = 10, Width = 500, Height = 16,
            Text = "Keys: Left/Right = force, Space = stop, C = center, Up/Down = strength",
            ForeColor = C_Dim, Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent, AutoSize = false,
        };
        pnl.Controls.Add(kbHelp);

        // Status line  X:12, Y:34, W:500, H:16
        _lblStatus = new Label
        {
            Left = 12, Top = 34, Width = 500, Height = 16,
            Text = "Ready",
            ForeColor = C_Dim, Font = new Font("Segoe UI", 9f),
            BackColor = Color.Transparent, AutoSize = false, AutoEllipsis = true,
        };
        pnl.Controls.Add(_lblStatus);

        // Emergency Stop  X:560, Y:18, W:160, H:40
        _btnEStop = new Button
        {
            Left = 560, Top = 18, Width = 160, Height = 40,
            Text = "EMERGENCY STOP",
            BackColor = C_EmergBtn, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        };
        _btnEStop.FlatAppearance.BorderColor = C_EmergBdr;
        _btnEStop.FlatAppearance.MouseOverBackColor = C_StopHov;
        _btnEStop.Click += (_, _) => { StopPresets(); EmergencyStop(); };
        pnl.Controls.Add(_btnEStop);
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private Panel MakePanel(int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Left = x, Top = y, Width = w, Height = h,
            BackColor = C_Panel,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(0),
        };
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(C_Border);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return p;
    }

    private static Label MakeLabel(string text, int x, int y, int w, int h, Color? color = null)
    {
        return new Label
        {
            Left = x, Top = y, Width = w, Height = h,
            Text = text,
            ForeColor = color ?? C_Text,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
        };
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

    private void OnDeviceSelectionChanged(object? sender, EventArgs e)
    {
        if (_cboDevice.SelectedItem is DeviceInstance dev)
            _lblSelected.Text = $"Selected: {dev.ProductName}";
        else
            _lblSelected.Text = "Selected: —";
    }

    private void OnConnectClick(object? sender, EventArgs e)
    {
        if (_wheel.FfbConnected) return;

        if (_cboDevice.SelectedItem is not DeviceInstance dev)
        {
            Log("No FFB device selected.");
            return;
        }

        bool ok = _wheel.ConnectFfb(dev, Handle);
        if (ok)
        {
            _btnConnect.Enabled    = false;
            _btnDisconnect.Enabled = true;
            _lblConnStatus.Text    = "Status: Connected";
            _lblConnStatus.ForeColor = C_Active;
            _lblSelected.Text      = $"Selected: {dev.ProductName}";
            Log($"Connected to {dev.ProductName}");
        }
        else
        {
            _lblConnStatus.Text     = "Status: Connection Failed";
            _lblConnStatus.ForeColor = C_Error;
            Log("Failed to acquire DirectInput device.");
        }
    }

    private void OnDisconnectClick(object? sender, EventArgs e)
    {
        StopPresets();
        _wheel.DisconnectFfb();
        _btnConnect.Enabled    = true;
        _btnDisconnect.Enabled = false;
        _lblConnStatus.Text    = "Status: Not Connected";
        _lblConnStatus.ForeColor = C_DimAlt;
        UpdateForceLabel(ForceDirection.Stopped);
        Log("Disconnected.");
    }

    // ── Force helpers ─────────────────────────────────────────────────────────

    private int Strength => _slider.Value;

    private void SendForce(ForceCommand cmd)
    {
        if (!_wheel.FfbConnected)
        {
            Log("Not connected — connect a device first.");
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
            ForceDirection.Left       => ("STATUS: LEFT FORCE",   C_Left),
            ForceDirection.Right      => ("STATUS: RIGHT FORCE",  C_Right),
            ForceDirection.Center     => ("STATUS: CENTERING",    C_Active),
            ForceDirection.PulseLeft  => ("STATUS: PULSE LEFT",   C_Left),
            ForceDirection.PulseRight => ("STATUS: PULSE RIGHT",  C_Right),
            ForceDirection.Oscillate  => ("STATUS: OSCILLATING",  C_Warn),
            _                         => ("STATUS: STOPPED",      C_ForceTxt),
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
                case Keys.Left when !_keyLeft:
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

    private void Log(string msg) => _lblStatus.Text = msg;

    // ── Form close ────────────────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopPresets();
        _wheel.EmergencyStop();
        _settings.TestStrength     = _slider.Value;
        _settings.StopOnKeyRelease = _chkStopKey.Checked;
        _settings.Save();
        _wheel.Dispose();
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
