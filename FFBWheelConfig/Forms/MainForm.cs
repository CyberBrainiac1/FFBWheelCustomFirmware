using FFBWheelConfig.Models;
using FFBWheelConfig.Services;

namespace FFBWheelConfig.Forms;

/// <summary>
/// Main application window — compact, dark, EMC-style wheel configuration utility.
/// Layout (top to bottom):
///   1. Top bar      – COM port selector, Connect/Disconnect, firmware/status labels
///   2. Steering     – large live-angle readout + steering range + Center button
///   3. Force/Tuning – sliders for force, damping, friction, spring + invert checkboxes
///   4. Bottom bar   – Read / Apply / Save / Reset buttons + status line
/// </summary>
public sealed class MainForm : Form
{
    // ── Service ──────────────────────────────────────────────────────────────
    private readonly WheelControllerService _service = new();

    // ── Top-bar controls ────────────────────────────────────────────────────
    private ComboBox _cboPort = null!;
    private Button _btnRefresh = null!;
    private Button _btnConnect = null!;
    private Button _btnDisconnect = null!;
    private Label _lblFirmware = null!;
    private Label _lblStatusDot = null!;
    private Label _lblStatusText = null!;

    // ── Steering-panel controls ──────────────────────────────────────────────
    private Label _lblAngle = null!;
    private Label _lblRawCounts = null!;
    private TrackBar _sldRange = null!;
    private NumericUpDown _nudRange = null!;
    private Button _btnCenter = null!;

    // ── Settings-panel controls ──────────────────────────────────────────────
    private TrackBar _sldForce = null!, _sldMinForce = null!, _sldDamping = null!,
                     _sldFriction = null!, _sldSpring = null!;
    private NumericUpDown _nudForce = null!, _nudMinForce = null!, _nudDamping = null!,
                          _nudFriction = null!, _nudSpring = null!;
    private CheckBox _chkInvertEncoder = null!, _chkInvertMotor = null!;

    // ── Bottom-bar controls ──────────────────────────────────────────────────
    private Button _btnRead = null!, _btnApply = null!, _btnSave = null!, _btnReset = null!;
    private Label _lblStatus = null!;

    // ── Colour palette ────────────────────────────────────────────────────────
    private static readonly Color BgDark   = Color.FromArgb(0x1E, 0x1E, 0x1E);
    private static readonly Color BgPanel  = Color.FromArgb(0x2A, 0x2A, 0x2A);
    private static readonly Color BgBar    = Color.FromArgb(0x22, 0x22, 0x22);
    private static readonly Color BgCtrl   = Color.FromArgb(0x38, 0x38, 0x38);
    private static readonly Color BgBtn    = Color.FromArgb(0x3C, 0x3C, 0x3C);
    private static readonly Color BgAccent = Color.FromArgb(0x00, 0x7F, 0x8A);
    private static readonly Color Accent   = Color.FromArgb(0x00, 0xBC, 0xD4);
    private static readonly Color TextMain = Color.FromArgb(0xE0, 0xE0, 0xE0);
    private static readonly Color TextDim  = Color.FromArgb(0x88, 0x88, 0x88);
    private static readonly Color BorderC  = Color.FromArgb(0x44, 0x44, 0x44);

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainForm()
    {
        InitializeComponent();
        WireServiceEvents();
        RefreshPorts();
        SetConnected(false);
    }

    // =========================================================================
    // UI CONSTRUCTION
    // =========================================================================

    private void InitializeComponent()
    {
        SuspendLayout();

        // ── Form ──────────────────────────────────────────────────────────────
        Text            = "FFB Wheel Config";
        BackColor       = BgDark;
        ForeColor       = TextMain;
        ClientSize      = new Size(700, 532);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;
        Font            = new Font("Segoe UI", 9f);

        // ── TOP BAR ──────────────────────────────────────────────────────────
        var topBar = new Panel { Bounds = new Rectangle(0, 0, 700, 50), BackColor = BgBar };

        var lblPort = Lbl("Port:", 8, 16, TextDim);
        _cboPort = new ComboBox
        {
            Bounds        = new Rectangle(46, 13, 90, 24),
            BackColor     = BgCtrl,
            ForeColor     = TextMain,
            FlatStyle     = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _btnRefresh    = Btn("↺",          141,  13, 28, 26);
        _btnConnect    = Btn("Connect",    174,  13, 78, 26, Accent,   Color.Black);
        _btnDisconnect = Btn("Disconnect", 257,  13, 90, 26);
        _lblFirmware   = Lbl("Firmware: —", 370, 16, TextDim);
        _lblStatusDot  = Lbl("●",          590, 16, Color.Gray);
        _lblStatusDot.Font = new Font("Segoe UI", 10f);
        _lblStatusText = Lbl("Disconnected", 606, 16, TextDim);

        topBar.Controls.AddRange(new Control[]
            { lblPort, _cboPort, _btnRefresh, _btnConnect, _btnDisconnect,
              _lblFirmware, _lblStatusDot, _lblStatusText });

        // ── STEERING PANEL ───────────────────────────────────────────────────
        var steeringPanel = new Panel
            { Bounds = new Rectangle(8, 54, 684, 190), BackColor = BgPanel };

        var lblSteeringHdr = Lbl("STEERING", 10, 7, Accent);
        lblSteeringHdr.Font = new Font("Segoe UI", 8f, FontStyle.Bold);

        // Large live-angle label — the centerpiece of the whole UI
        _lblAngle = new Label
        {
            Text      = "—",
            Bounds    = new Rectangle(60, 14, 440, 100),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextMain,
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 68f, FontStyle.Bold),
        };

        // Degree symbol label to the right of the big number, same row
        var lblDegSymbol = new Label
        {
            Text      = "°",
            Bounds    = new Rectangle(500, 56, 40, 40),
            ForeColor = TextDim,
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 30f),
        };

        _lblRawCounts = new Label
        {
            Text      = "Raw: —",
            Bounds    = new Rectangle(60, 116, 440, 16),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = TextDim,
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 8f),
        };

        // Steering-range row
        var lblRangeHdr = Lbl("Range:", 10, 147);
        _sldRange = new TrackBar
        {
            Bounds        = new Rectangle(66, 138, 408, 32),
            Minimum       = 90,
            Maximum       = 1800,
            Value         = 900,
            TickFrequency = 90,
            BackColor     = BgPanel,
            AutoSize      = false,
        };
        _nudRange = new NumericUpDown
        {
            Bounds      = new Rectangle(480, 143, 68, 22),
            Minimum     = 90,
            Maximum     = 1800,
            Value       = 900,
            BackColor   = BgCtrl,
            ForeColor   = TextMain,
            BorderStyle = BorderStyle.FixedSingle,
        };
        var lblRangeDeg = Lbl("°", 551, 146);
        _btnCenter = Btn("Center", 572, 141, 80, 26);

        steeringPanel.Controls.AddRange(new Control[]
            { lblSteeringHdr, _lblAngle, lblDegSymbol, _lblRawCounts,
              lblRangeHdr, _sldRange, _nudRange, lblRangeDeg, _btnCenter });

        // ── FORCE & TUNING PANEL ──────────────────────────────────────────────
        var tuningPanel = new Panel
            { Bounds = new Rectangle(8, 250, 684, 200), BackColor = BgPanel };

        var lblTuningHdr = Lbl("FORCE & TUNING", 10, 7, Accent);
        lblTuningHdr.Font = new Font("Segoe UI", 8f, FontStyle.Bold);

        // Row layout constants
        const int LX = 10, SX = 120, SW = 300, NX = 425, NW = 54, CX = 490;
        const int R0 = 26, RH = 32;

        (_sldForce,    _nudForce)    = AddSettingRow(tuningPanel, "Overall Force", LX, SX, SW, NX, NW, R0 + RH * 0, 0, 100, 60);
        (_sldMinForce, _nudMinForce) = AddSettingRow(tuningPanel, "Min Force",     LX, SX, SW, NX, NW, R0 + RH * 1, 0, 100,  5);
        (_sldDamping,  _nudDamping)  = AddSettingRow(tuningPanel, "Damping",       LX, SX, SW, NX, NW, R0 + RH * 2, 0, 100, 10);
        (_sldFriction, _nudFriction) = AddSettingRow(tuningPanel, "Friction",      LX, SX, SW, NX, NW, R0 + RH * 3, 0, 100,  4);
        (_sldSpring,   _nudSpring)   = AddSettingRow(tuningPanel, "Spring",        LX, SX, SW, NX, NW, R0 + RH * 4, 0, 100, 15);

        _chkInvertEncoder = new CheckBox
        {
            Text      = "Invert Encoder",
            Bounds    = new Rectangle(CX, R0 + RH * 0, 150, 24),
            ForeColor = TextMain,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
        };
        _chkInvertMotor = new CheckBox
        {
            Text      = "Invert Motor",
            Bounds    = new Rectangle(CX, R0 + RH * 1, 150, 24),
            ForeColor = TextMain,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
        };

        tuningPanel.Controls.AddRange(new Control[]
            { lblTuningHdr, _chkInvertEncoder, _chkInvertMotor });

        // ── BOTTOM BAR ────────────────────────────────────────────────────────
        var bottomBar = new Panel
            { Bounds = new Rectangle(0, 456, 700, 76), BackColor = BgBar };

        _btnRead  = Btn("Read from Wheel", 8,   10, 132, 28);
        _btnApply = Btn("Apply",          146,  10,  80, 28, BgAccent, Color.White);
        _btnSave  = Btn("Save to Wheel",  232,  10, 118, 28, BgAccent, Color.White);
        _btnReset = Btn("Reset Defaults", 356,  10, 120, 28);

        _lblStatus = new Label
        {
            Text      = "Ready",
            Bounds    = new Rectangle(8, 44, 680, 18),
            ForeColor = TextDim,
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 8f),
        };

        bottomBar.Controls.AddRange(new Control[]
            { _btnRead, _btnApply, _btnSave, _btnReset, _lblStatus });

        // ── Assemble ─────────────────────────────────────────────────────────
        Controls.AddRange(new Control[] { topBar, steeringPanel, tuningPanel, bottomBar });

        ResumeLayout(false);
    }

    // ── Helper: add a labelled slider+NUD row to a parent panel ──────────────
    private (TrackBar slider, NumericUpDown nud) AddSettingRow(
        Panel parent, string label,
        int lx, int sx, int sw, int nx, int nw,
        int y, int min, int max, int val)
    {
        parent.Controls.Add(Lbl(label, lx, y + 5));

        var sldr = new TrackBar
        {
            Bounds        = new Rectangle(sx, y, sw, 26),
            Minimum       = min,
            Maximum       = max,
            Value         = val,
            TickFrequency = 10,
            BackColor     = BgPanel,
            AutoSize      = false,
        };

        var nud = new NumericUpDown
        {
            Bounds      = new Rectangle(nx, y + 3, nw, 22),
            Minimum     = min,
            Maximum     = max,
            Value       = val,
            BackColor   = BgCtrl,
            ForeColor   = TextMain,
            BorderStyle = BorderStyle.FixedSingle,
        };

        parent.Controls.Add(sldr);
        parent.Controls.Add(nud);
        return (sldr, nud);
    }

    // ── Tiny factory helpers ──────────────────────────────────────────────────
    private static Label Lbl(string text, int x, int y, Color? color = null)
        => new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            ForeColor = color ?? TextMain,
            BackColor = Color.Transparent,
            AutoSize  = true,
        };

    private static Button Btn(string text, int x, int y, int w, int h,
                               Color? back = null, Color? fore = null)
    {
        var b = new Button
        {
            Text      = text,
            Bounds    = new Rectangle(x, y, w, h),
            BackColor = back ?? BgBtn,
            ForeColor = fore ?? TextMain,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = BorderC;
        b.FlatAppearance.BorderSize  = 1;
        return b;
    }

    // =========================================================================
    // EVENT WIRING
    // =========================================================================

    private void WireServiceEvents()
    {
        // Top-bar buttons
        _btnRefresh.Click    += (_, _) => RefreshPorts();
        _btnConnect.Click    += BtnConnect_Click;
        _btnDisconnect.Click += BtnDisconnect_Click;

        // Bottom-bar buttons
        _btnRead.Click  += (_, _) => ReadSettings();
        _btnApply.Click += (_, _) => ApplySettings();
        _btnSave.Click  += (_, _) => SaveSettings();
        _btnReset.Click += (_, _) => ResetDefaults();
        _btnCenter.Click += (_, _) => SetCenter();

        // Range slider ↔ NUD
        LinkSliderNud(_sldRange, _nudRange, 90, 1800);

        // Force/tuning sliders ↔ NUDs
        LinkSliderNud(_sldForce,    _nudForce,    0, 100);
        LinkSliderNud(_sldMinForce, _nudMinForce, 0, 100);
        LinkSliderNud(_sldDamping,  _nudDamping,  0, 100);
        LinkSliderNud(_sldFriction, _nudFriction, 0, 100);
        LinkSliderNud(_sldSpring,   _nudSpring,   0, 100);

        // Service events — all must be marshalled to the UI thread
        _service.SettingsUpdated  += s   => SafeInvoke(() => LoadSettingsIntoUI(s));
        _service.LiveAngleUpdated += ang => SafeInvoke(() => _lblAngle.Text = $"{ang:F0}");
        _service.RawCountsUpdated += cnt => SafeInvoke(() => _lblRawCounts.Text = $"Raw: {cnt}");
        _service.Disconnected     += ()  => SafeInvoke(() =>
        {
            SetConnected(false);
            SetStatus("Device disconnected", Color.OrangeRed);
        });
    }

    private static void LinkSliderNud(TrackBar s, NumericUpDown n, int min, int max)
    {
        s.ValueChanged += (_, _) =>
        {
            int v = Math.Clamp(s.Value, min, max);
            if (n.Value != v) n.Value = v;
        };
        n.ValueChanged += (_, _) =>
        {
            int v = Math.Clamp((int)n.Value, min, max);
            if (s.Value != v) s.Value = v;
        };
    }

    private void SafeInvoke(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    // =========================================================================
    // BUTTON HANDLERS
    // =========================================================================

    private void RefreshPorts()
    {
        string? current = _cboPort.SelectedItem?.ToString();
        _cboPort.Items.Clear();
        var ports = WheelControllerService.GetAvailablePorts();
        if (ports.Length == 0)
        {
            _cboPort.Items.Add("(no ports)");
            _cboPort.SelectedIndex = 0;
        }
        else
        {
            foreach (var p in ports) _cboPort.Items.Add(p);
            _cboPort.SelectedItem = current != null && _cboPort.Items.Contains(current)
                ? current
                : _cboPort.Items[0];
        }
    }

    private void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_cboPort.SelectedItem?.ToString() is not string port
            || port.StartsWith('(')) return;

        SetStatus($"Connecting to {port}…", TextDim);

        if (_service.Connect(port))
        {
            SetConnected(true);
            SetStatus($"Connected to {port}", Accent);
            _service.ReadSettings();
        }
        else
        {
            SetStatus($"Failed to connect to {port}", Color.OrangeRed);
        }
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        _service.Disconnect();
        SetConnected(false);
        SetStatus("Disconnected", TextDim);
    }

    private void ReadSettings()
    {
        if (!_service.IsConnected) return;
        SetStatus("Reading settings…", TextDim);
        if (_service.ReadSettings())
            SetStatus("Waiting for settings…", TextDim);
        else
            SetStatus("Read failed", Color.OrangeRed);
    }

    private void ApplySettings()
    {
        if (!_service.IsConnected) return;
        SetStatus("Applying settings…", TextDim);
        if (_service.ApplySettings(GatherSettings()))
            SetStatus("Settings applied", Accent);
        else
            SetStatus("Apply failed", Color.OrangeRed);
    }

    private void SaveSettings()
    {
        if (!_service.IsConnected) return;
        SetStatus("Saving to EEPROM…", TextDim);
        if (_service.SaveSettings())
            SetStatus("Saved to EEPROM", Accent);
        else
            SetStatus("Save failed", Color.OrangeRed);
    }

    private void ResetDefaults()
    {
        if (!_service.IsConnected) return;
        SetStatus("Resetting to defaults…", TextDim);
        if (_service.ResetDefaults())
            SetStatus("Defaults loaded — waiting for settings…", TextDim);
        else
            SetStatus("Reset failed", Color.OrangeRed);
    }

    private void SetCenter()
    {
        if (!_service.IsConnected) return;
        SetStatus("Setting wheel center…", TextDim);
        if (_service.SetCenter())
            SetStatus("Center set", Accent);
        else
            SetStatus("Center failed", Color.OrangeRed);
    }

    // =========================================================================
    // UI STATE HELPERS
    // =========================================================================

    private void SetConnected(bool connected)
    {
        _btnConnect.Enabled    = !connected;
        _btnDisconnect.Enabled =  connected;
        _btnRead.Enabled  = connected;
        _btnApply.Enabled = connected;
        _btnSave.Enabled  = connected;
        _btnReset.Enabled = connected;
        _btnCenter.Enabled = connected;
        _sldRange.Enabled  = connected;
        _nudRange.Enabled  = connected;
        _sldForce.Enabled    = connected; _nudForce.Enabled    = connected;
        _sldMinForce.Enabled = connected; _nudMinForce.Enabled = connected;
        _sldDamping.Enabled  = connected; _nudDamping.Enabled  = connected;
        _sldFriction.Enabled = connected; _nudFriction.Enabled = connected;
        _sldSpring.Enabled   = connected; _nudSpring.Enabled   = connected;
        _chkInvertEncoder.Enabled = connected;
        _chkInvertMotor.Enabled   = connected;

        if (connected)
        {
            _lblStatusDot.ForeColor  = Accent;
            _lblStatusText.Text      = "Connected";
            _lblStatusText.ForeColor = Accent;
        }
        else
        {
            _lblStatusDot.ForeColor  = Color.Gray;
            _lblStatusText.Text      = "Disconnected";
            _lblStatusText.ForeColor = TextDim;
            _lblAngle.Text           = "—";
            _lblRawCounts.Text       = "Raw: —";
            _lblFirmware.Text        = "Firmware: —";
        }
    }

    private void LoadSettingsIntoUI(WheelSettings s)
    {
        SafeSetSliderNud(_sldRange,     _nudRange,     Math.Clamp(s.SteeringRange,  90, 1800));
        SafeSetSliderNud(_sldForce,     _nudForce,     Math.Clamp(s.OverallForce,    0,  100));
        SafeSetSliderNud(_sldMinForce,  _nudMinForce,  Math.Clamp(s.MinimumForce,    0,  100));
        SafeSetSliderNud(_sldDamping,   _nudDamping,   Math.Clamp(s.Damping,         0,  100));
        SafeSetSliderNud(_sldFriction,  _nudFriction,  Math.Clamp(s.Friction,        0,  100));
        SafeSetSliderNud(_sldSpring,    _nudSpring,    Math.Clamp(s.Spring,          0,  100));
        _chkInvertEncoder.Checked = s.InvertEncoder;
        _chkInvertMotor.Checked   = s.InvertMotor;
        if (!string.IsNullOrEmpty(s.FirmwareVersion))
            _lblFirmware.Text = $"Firmware: {s.FirmwareVersion}";
        SetStatus("Settings loaded", TextMain);
    }

    private static void SafeSetSliderNud(TrackBar sldr, NumericUpDown nud, int value)
    {
        if (sldr.Value != value) sldr.Value = value;
        if (nud.Value  != value) nud.Value  = value;
    }

    private WheelSettings GatherSettings() => new()
    {
        OverallForce   = (int)_nudForce.Value,
        MinimumForce   = (int)_nudMinForce.Value,
        Damping        = (int)_nudDamping.Value,
        Friction       = (int)_nudFriction.Value,
        Spring         = (int)_nudSpring.Value,
        SteeringRange  = (int)_nudRange.Value,
        InvertEncoder  = _chkInvertEncoder.Checked,
        InvertMotor    = _chkInvertMotor.Checked,
    };

    private void SetStatus(string msg, Color? color = null)
    {
        _lblStatus.Text      = msg;
        _lblStatus.ForeColor = color ?? TextDim;
    }

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _service.Dispose();
        base.OnFormClosed(e);
    }
}
