using FFBWheelConfig.Controls;
using FFBWheelConfig.Models;
using FFBWheelConfig.Services;

namespace FFBWheelConfig.Forms;

/// <summary>
/// FFB Wheel Test Utility – main window.
/// Provides manual force test buttons, keyboard hotkeys, firmware flash, and safety controls.
/// </summary>
public sealed class MainForm : Form
{
    private const string AppTitle = "FFB Wheel Tester";

    // ── Colours (dark flat theme) ────────────────────────────────────────────
    private static readonly Color C_Root     = ColorTranslator.FromHtml("#1E2124");
    private static readonly Color C_Panel    = ColorTranslator.FromHtml("#2A2D31");
    private static readonly Color C_Border   = ColorTranslator.FromHtml("#3A3D42");
    private static readonly Color C_Ctrl     = ColorTranslator.FromHtml("#1A1D20");
    private static readonly Color C_CtrlBdr  = ColorTranslator.FromHtml("#4A4E55");
    private static readonly Color C_Btn      = ColorTranslator.FromHtml("#3A3F46");
    private static readonly Color C_BtnBdr   = ColorTranslator.FromHtml("#555B64");
    private static readonly Color C_Primary  = ColorTranslator.FromHtml("#2F5F87");
    private static readonly Color C_PrimBdr  = ColorTranslator.FromHtml("#4C83B1");
    private static readonly Color C_Danger   = ColorTranslator.FromHtml("#8B0000");
    private static readonly Color C_DangerHi = ColorTranslator.FromHtml("#C0392B");
    private static readonly Color C_Warning  = ColorTranslator.FromHtml("#E67E22");
    private static readonly Color C_Ok       = ColorTranslator.FromHtml("#27AE60");
    private static readonly Color C_Disabled = ColorTranslator.FromHtml("#2B2E33");
    private static readonly Color C_DisText  = ColorTranslator.FromHtml("#7E858D");
    private static readonly Color C_Text     = ColorTranslator.FromHtml("#E6E6E6");
    private static readonly Color C_SubText  = ColorTranslator.FromHtml("#A0A7B0");
    private static readonly Color C_Hint     = ColorTranslator.FromHtml("#9098A1");
    private static readonly Color C_Status   = ColorTranslator.FromHtml("#5BC8FA");

    // ── Services ─────────────────────────────────────────────────────────────
    private readonly WheelControllerService _service = new();
    private readonly AppSettings _appSettings = AppSettings.Load();

    // ── Top bar ───────────────────────────────────────────────────────────────
    private ComboBox _cboPort = null!;
    private Button   _btnRefresh = null!;
    private Button   _btnConnect = null!;
    private Button   _btnDisconnect = null!;
    private Button   _btnFlash = null!;
    private Label    _lblConnStatus = null!;

    // ── Status display ────────────────────────────────────────────────────────
    private Label _lblForceStatus = null!;
    private Label _lblAngle = null!;
    private Label _lblSafetyWarning = null!;

    // ── Force strength ────────────────────────────────────────────────────────
    private FlatSlider    _sldStrength = null!;
    private NumericUpDown _nudStrength = null!;

    // ── Manual buttons ────────────────────────────────────────────────────────
    private Button _btnLeft   = null!;
    private Button _btnRight  = null!;
    private Button _btnCenter = null!;
    private Button _btnStop   = null!;

    // ── Preset buttons ────────────────────────────────────────────────────────
    private Button _btnPulseLeft   = null!;
    private Button _btnPulseRight  = null!;
    private Button _btnOscillate   = null!;
    private Button _btnPresetStop  = null!;

    // ── Options / Emergency ───────────────────────────────────────────────────
    private CheckBox _chkStopOnRelease = null!;
    private Button   _btnEmergency     = null!;
    private Label    _lblStatusLine    = null!;

    // ── Keyboard / pulse state ────────────────────────────────────────────────
    private System.Windows.Forms.Timer? _pulseTimer;
    private System.Windows.Forms.Timer? _oscTimer;
    private bool _oscLeft;
    private readonly HashSet<Keys> _heldKeys = new();

    private bool _updatingControls;

    public MainForm()
    {
        InitializeComponent();
        WireEvents();
        RefreshPorts();
        UpdateForceStatus(ForceDirection.Stopped);
        SetConnected(false);
        SetStatus("Ready  -  Keep hands clear during testing");
        _sldStrength.Value = _appSettings.TestForceStrength;
        _nudStrength.Value = _appSettings.TestForceStrength;
        _chkStopOnRelease.Checked = _appSettings.StopOnKeyRelease;
    }

    // ── Build UI ──────────────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        SuspendLayout();

        Text            = AppTitle;
        BackColor       = C_Root;
        ForeColor       = C_Text;
        ClientSize      = new Size(760, 520);
        AutoScaleMode   = AutoScaleMode.None;
        Font            = UiFont(10f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;
        KeyPreview      = true;

        var topBar       = MakePanel(new Rectangle(14, 10, 732, 60));
        var statusPanel  = MakePanel(new Rectangle(14, 80, 732, 100));
        var controlPanel = MakePanel(new Rectangle(14, 190, 732, 190));
        var bottomBar    = MakePanel(new Rectangle(14, 390, 732, 116));

        BuildTopBar(topBar);
        BuildStatusPanel(statusPanel);
        BuildControlPanel(controlPanel);
        BuildBottomBar(bottomBar);

        Controls.AddRange(new Control[] { topBar, statusPanel, controlPanel, bottomBar });

        Size        = SizeFromClientSize(ClientSize);
        MinimumSize = Size;
        MaximumSize = Size;

        ResumeLayout(false);
    }

    private void BuildTopBar(Panel p)
    {
        p.Controls.Add(MakeLabel("COM Port", new Rectangle(12, 21, 64, 20), C_Text, 10f));

        _cboPort       = MakeComboBox(new Rectangle(82, 16, 110, 28));
        _btnRefresh    = MakeButton("Refresh",  new Rectangle(204, 16, 80, 28), ButtonKind.Normal);
        _btnConnect    = MakeButton("Connect",  new Rectangle(294, 16, 80, 28), ButtonKind.Primary);
        _btnDisconnect = MakeButton("Disconnect", new Rectangle(384, 16, 90, 28), ButtonKind.Normal);
        _btnFlash      = MakeButton("Flash EMC Hex", new Rectangle(486, 16, 120, 28), ButtonKind.Normal);
        _lblConnStatus = MakeLabel("Disconnected", new Rectangle(622, 20, 100, 20), C_SubText, 9f);

        p.Controls.AddRange(new Control[] {
            _cboPort, _btnRefresh, _btnConnect, _btnDisconnect, _btnFlash, _lblConnStatus });
    }

    private void BuildStatusPanel(Panel p)
    {
        _lblForceStatus = MakeLabel("STATUS: STOPPED", new Rectangle(16, 14, 440, 44), C_Status, 26f, bold: true);

        _lblAngle = MakeLabel("Angle: --", new Rectangle(16, 66, 200, 20), C_SubText, 9f);

        _lblSafetyWarning = MakeLabel(
            "Keep hands clear during testing",
            new Rectangle(450, 30, 268, 24),
            C_Warning, 10f, bold: true);
        _lblSafetyWarning.TextAlign = ContentAlignment.MiddleRight;

        p.Controls.AddRange(new Control[] { _lblForceStatus, _lblAngle, _lblSafetyWarning });
    }

    private void BuildControlPanel(Panel p)
    {
        // ── Force strength row ───────────────────────────────────────────────
        p.Controls.Add(MakeLabel("Force %", new Rectangle(14, 14, 64, 22), C_Text, 10f));
        _sldStrength = new FlatSlider
        {
            Bounds = new Rectangle(84, 14, 280, 24),
            Minimum = 0, Maximum = 100, Value = 20,
            TrackColor = ColorTranslator.FromHtml("#1E2023"),
            FillColor  = ColorTranslator.FromHtml("#3E89B8"),
            ThumbColor = C_Text,
            BorderColor= C_CtrlBdr,
        };
        _nudStrength = new NumericUpDown
        {
            Bounds = new Rectangle(376, 12, 56, 28),
            Minimum = 0, Maximum = 100, Value = 20,
            TextAlign = HorizontalAlignment.Center,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = C_Ctrl, ForeColor = C_Text,
            Font = UiFont(10f),
        };
        p.Controls.Add(_sldStrength);
        p.Controls.Add(_nudStrength);
        p.Controls.Add(MakeLabel("%", new Rectangle(436, 16, 20, 20), C_SubText, 9f));

        // ── Manual force buttons ─────────────────────────────────────────────
        p.Controls.Add(MakeLabel("Manual control", new Rectangle(14, 54, 120, 20), C_Hint, 9f));
        _btnLeft   = MakeButton("Turn Left",  new Rectangle(14,  76, 130, 36), ButtonKind.Primary);
        _btnRight  = MakeButton("Turn Right", new Rectangle(154, 76, 130, 36), ButtonKind.Primary);
        _btnCenter = MakeButton("Center",     new Rectangle(294, 76, 110, 36), ButtonKind.Normal);
        _btnStop   = MakeButton("Stop Force", new Rectangle(414, 76, 120, 36), ButtonKind.Normal);

        p.Controls.AddRange(new Control[] { _btnLeft, _btnRight, _btnCenter, _btnStop });

        // ── Preset buttons ───────────────────────────────────────────────────
        p.Controls.Add(MakeLabel("Test presets", new Rectangle(14, 128, 100, 20), C_Hint, 9f));
        _btnPulseLeft  = MakeButton("Pulse Left",  new Rectangle(14,  148, 110, 30), ButtonKind.Normal);
        _btnPulseRight = MakeButton("Pulse Right", new Rectangle(132, 148, 110, 30), ButtonKind.Normal);
        _btnOscillate  = MakeButton("Oscillate",   new Rectangle(250, 148, 110, 30), ButtonKind.Normal);
        _btnPresetStop = MakeButton("Stop",        new Rectangle(368, 148,  80, 30), ButtonKind.Normal);

        p.Controls.AddRange(new Control[] {
            _btnPulseLeft, _btnPulseRight, _btnOscillate, _btnPresetStop });
    }

    private void BuildBottomBar(Panel p)
    {
        p.Controls.Add(MakeLabel(
            "Left = left force   Right = right force   Space = stop   C = center   Up/Down = force +/-5%",
            new Rectangle(14, 12, 570, 18), C_Hint, 9f));

        _chkStopOnRelease = new CheckBox
        {
            Text = "Stop force on key release",
            Bounds = new Rectangle(14, 34, 210, 22),
            BackColor = Color.Transparent,
            ForeColor = C_Text,
            FlatStyle = FlatStyle.Flat,
            Font = UiFont(9f),
            Checked = true,
        };
        _chkStopOnRelease.FlatAppearance.BorderColor = C_CtrlBdr;
        _chkStopOnRelease.FlatAppearance.CheckedBackColor = C_Panel;
        _chkStopOnRelease.FlatAppearance.MouseOverBackColor = C_Panel;
        p.Controls.Add(_chkStopOnRelease);

        _lblStatusLine = MakeLabel("Ready", new Rectangle(14, 62, 530, 18), C_SubText, 9f);
        _lblStatusLine.AutoEllipsis = true;
        p.Controls.Add(_lblStatusLine);

        _btnEmergency = MakeButton("EMERGENCY STOP", new Rectangle(568, 14, 150, 80), ButtonKind.Danger);
        _btnEmergency.Font = UiFont(11f, bold: true);
        p.Controls.Add(_btnEmergency);
    }

    // ── Event wiring ──────────────────────────────────────────────────────────

    private void WireEvents()
    {
        _btnRefresh.Click    += (_, _) => RefreshPorts();
        _btnConnect.Click    += BtnConnect_Click;
        _btnDisconnect.Click += BtnDisconnect_Click;
        _btnFlash.Click      += BtnFlash_Click;
        _btnEmergency.Click  += (_, _) => EmergencyStop();

        _btnLeft.MouseDown   += (_, e) => { if (e.Button == MouseButtons.Left) SendForce(ForceDirection.Left); };
        _btnLeft.MouseUp     += (_, _) => { if (_chkStopOnRelease.Checked) SendForce(ForceDirection.Stopped); };
        _btnRight.MouseDown  += (_, e) => { if (e.Button == MouseButtons.Left) SendForce(ForceDirection.Right); };
        _btnRight.MouseUp    += (_, _) => { if (_chkStopOnRelease.Checked) SendForce(ForceDirection.Stopped); };
        _btnCenter.Click     += (_, _) => SendForce(ForceDirection.Center);
        _btnStop.Click       += (_, _) => SendForce(ForceDirection.Stopped);

        _btnPulseLeft.Click  += (_, _) => StartPulse(ForceDirection.Left);
        _btnPulseRight.Click += (_, _) => StartPulse(ForceDirection.Right);
        _btnOscillate.Click  += (_, _) => StartOscillate();
        _btnPresetStop.Click += (_, _) => { StopTimers(); SendForce(ForceDirection.Stopped); };

        LinkStrength(_sldStrength, _nudStrength);

        _chkStopOnRelease.CheckedChanged += (_, _) =>
        {
            _appSettings.StopOnKeyRelease = _chkStopOnRelease.Checked;
            _appSettings.Save();
        };

        _service.LiveAngleUpdated += angle =>
            SafeInvoke(() => _lblAngle.Text = $"Angle: {angle:F0} deg");
        _service.Disconnected += () => SafeInvoke(() =>
        {
            SetConnected(false);
            SetStatus("Device disconnected");
        });

        KeyDown += MainForm_KeyDown;
        KeyUp   += MainForm_KeyUp;
    }

    private void LinkStrength(FlatSlider sld, NumericUpDown nud)
    {
        sld.ValueChanged += (_, _) =>
        {
            if (_updatingControls) return;
            _updatingControls = true;
            nud.Value = sld.Value;
            _updatingControls = false;
            _appSettings.TestForceStrength = sld.Value;
            _appSettings.Save();
        };

        nud.ValueChanged += (_, _) =>
        {
            if (_updatingControls) return;
            _updatingControls = true;
            sld.Value = (int)nud.Value;
            _updatingControls = false;
            _appSettings.TestForceStrength = (int)nud.Value;
            _appSettings.Save();
        };
    }

    // ── Keyboard handlers ─────────────────────────────────────────────────────

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_heldKeys.Contains(e.KeyCode)) return;
        _heldKeys.Add(e.KeyCode);

        switch (e.KeyCode)
        {
            case Keys.Left:
                SendForce(ForceDirection.Left);
                e.Handled = true;
                break;
            case Keys.Right:
                SendForce(ForceDirection.Right);
                e.Handled = true;
                break;
            case Keys.Space:
                SendForce(ForceDirection.Stopped);
                e.Handled = true;
                break;
            case Keys.C:
                SendForce(ForceDirection.Center);
                e.Handled = true;
                break;
            case Keys.Up:
                ChangeStrength(+5);
                e.Handled = true;
                break;
            case Keys.Down:
                ChangeStrength(-5);
                e.Handled = true;
                break;
        }
    }

    private void MainForm_KeyUp(object? sender, KeyEventArgs e)
    {
        _heldKeys.Remove(e.KeyCode);

        if (_chkStopOnRelease.Checked &&
            (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
        {
            if (!_heldKeys.Contains(Keys.Left) && !_heldKeys.Contains(Keys.Right))
                SendForce(ForceDirection.Stopped);
        }
    }

    private void ChangeStrength(int delta)
    {
        int v = Math.Clamp(_sldStrength.Value + delta, 0, 100);
        _updatingControls = true;
        _sldStrength.Value = v;
        _nudStrength.Value = v;
        _updatingControls = false;
        _appSettings.TestForceStrength = v;
        _appSettings.Save();
    }

    // ── Force commands ────────────────────────────────────────────────────────

    private void SendForce(ForceDirection dir)
    {
        StopTimers();
        int strength = (int)_nudStrength.Value;

        bool ok = dir switch
        {
            ForceDirection.Left    => _service.TestForceLeft(strength),
            ForceDirection.Right   => _service.TestForceRight(strength),
            ForceDirection.Center  => _service.TestForceCenter(),
            ForceDirection.Stopped => _service.TestForceStop(),
            _                      => _service.TestForceStop(),
        };

        if (!ok && dir != ForceDirection.Stopped)
            SetStatus("Not connected - cannot send force command");

        UpdateForceStatus(dir);
    }

    private void EmergencyStop()
    {
        StopTimers();
        _service.TestForceStop();
        UpdateForceStatus(ForceDirection.Stopped);
        SetStatus("EMERGENCY STOP - all force output stopped");
    }

    private void StartPulse(ForceDirection dir)
    {
        StopTimers();
        _pulseTimer = new System.Windows.Forms.Timer { Interval = 300 };
        bool on = true;
        _pulseTimer.Tick += (_, _) =>
        {
            if (on) SendForce(dir);
            else    SendForce(ForceDirection.Stopped);
            on = !on;
        };
        SendForce(dir);
        _pulseTimer.Start();
        UpdateForceStatus(dir == ForceDirection.Left ? ForceDirection.PulseLeft : ForceDirection.PulseRight);
    }

    private void StartOscillate()
    {
        StopTimers();
        _oscLeft = true;
        _oscTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _oscTimer.Tick += (_, _) =>
        {
            _oscLeft = !_oscLeft;
            int strength = (int)_nudStrength.Value;
            if (_oscLeft) _service.TestForceLeft(strength);
            else          _service.TestForceRight(strength);
        };
        _service.TestForceLeft((int)_nudStrength.Value);
        _oscTimer.Start();
        UpdateForceStatus(ForceDirection.Oscillate);
    }

    private void StopTimers()
    {
        if (_pulseTimer != null)
        {
            _pulseTimer.Stop();
            _pulseTimer.Dispose();
            _pulseTimer = null;
        }
        if (_oscTimer != null)
        {
            _oscTimer.Stop();
            _oscTimer.Dispose();
            _oscTimer = null;
        }
    }

    // ── Connection ────────────────────────────────────────────────────────────

    private void RefreshPorts()
    {
        string? sel = _cboPort.SelectedItem?.ToString();
        _cboPort.Items.Clear();
        var ports = WheelControllerService.GetAvailablePorts();
        if (ports.Length == 0)
        {
            _cboPort.Items.Add("(none)");
            _cboPort.SelectedIndex = 0;
            return;
        }
        foreach (string port in ports) _cboPort.Items.Add(port);
        _cboPort.SelectedItem = sel != null && _cboPort.Items.Contains(sel)
            ? sel : _cboPort.Items[0];
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_cboPort.SelectedItem?.ToString() is not string port || port.StartsWith('('))
            return;

        _btnConnect.Enabled  = false;
        _lblConnStatus.Text  = "Connecting...";
        _lblConnStatus.ForeColor = C_Warning;
        SetStatus($"Connecting to {port}...");

        bool ok = await Task.Run(() => _service.Connect(port));

        if (!ok)
        {
            SetConnected(false);
            SetStatus($"Could not connect to {port}");
            return;
        }

        _appSettings.LastPort = port;
        _appSettings.Save();
        SetConnected(true);
        SetStatus($"Connected to {port}  -  Start with low force strength");
        _service.ReadSettings();
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        EmergencyStop();
        _service.Disconnect();
        SetConnected(false);
        SetStatus("Disconnected");
    }

    private async void BtnFlash_Click(object? sender, EventArgs e)
    {
        if (_cboPort.SelectedItem?.ToString() is not string port || port.StartsWith('('))
        {
            SetStatus("Select a COM port before flashing");
            return;
        }

        string? hexPath = FirmwareFlasher.FindHexPath();
        if (hexPath == null)
        {
            SetStatus("Could not locate .hex file - build the firmware first");
            MessageBox.Show(
                "The firmware .hex file was not found.\n\n" +
                "Build it first:\n  arduino-cli compile --fqbn arduino:avr:leonardo ...\n\n" +
                "Or run: build_versioned_release.bat",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        EmergencyStop();
        _service.Disconnect();
        SetConnected(false);

        var result = MessageBox.Show(
            $"Flash EMC-compatible hex to {port}?\n\n" +
            $"Hex: {hexPath}\n\n" +
            "The board will reset.\nMake sure the wheel is NOT moving.",
            AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes) return;

        _btnFlash.Enabled = false;
        SetStatus("Flashing firmware...");

        var progress = new Progress<string>(line => SafeInvoke(() => SetStatus(line)));
        var (success, log) = await FirmwareFlasher.FlashAsync(hexPath, port,
            progress, CancellationToken.None);

        _btnFlash.Enabled = true;

        if (success)
        {
            SetStatus("Flash complete - unplug and replug the wheel, then click Connect");
            MessageBox.Show(
                "Flash complete!\n\nUnplug and replug the wheel, then click Connect.",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            SetStatus("Flash failed - see log below");
            MessageBox.Show($"Flash failed.\n\nLog:\n{log}",
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private void UpdateForceStatus(ForceDirection dir)
    {
        (string text, Color color) = dir switch
        {
            ForceDirection.Left       => ("STATUS: LEFT FORCE",   ColorTranslator.FromHtml("#5BC8FA")),
            ForceDirection.Right      => ("STATUS: RIGHT FORCE",  ColorTranslator.FromHtml("#5BC8FA")),
            ForceDirection.Center     => ("STATUS: CENTERING",    ColorTranslator.FromHtml("#2ECC71")),
            ForceDirection.PulseLeft  => ("STATUS: PULSE LEFT",   ColorTranslator.FromHtml("#F39C12")),
            ForceDirection.PulseRight => ("STATUS: PULSE RIGHT",  ColorTranslator.FromHtml("#F39C12")),
            ForceDirection.Oscillate  => ("STATUS: OSCILLATING",  ColorTranslator.FromHtml("#F39C12")),
            _                         => ("STATUS: STOPPED",       ColorTranslator.FromHtml("#E74C3C")),
        };
        _lblForceStatus.Text      = text;
        _lblForceStatus.ForeColor = color;
    }

    private void SetConnected(bool connected)
    {
        _btnConnect.Enabled    = !connected;
        _btnDisconnect.Enabled = connected;
        _btnLeft.Enabled       = connected;
        _btnRight.Enabled      = connected;
        _btnCenter.Enabled     = connected;
        _btnStop.Enabled       = connected;
        _btnPulseLeft.Enabled  = connected;
        _btnPulseRight.Enabled = connected;
        _btnOscillate.Enabled  = connected;
        _btnPresetStop.Enabled = connected;

        _lblConnStatus.Text      = connected ? "Connected" : "Disconnected";
        _lblConnStatus.ForeColor = connected ? C_Ok : C_SubText;

        if (!connected)
        {
            _lblAngle.Text = "Angle: --";
            UpdateForceStatus(ForceDirection.Stopped);
        }
    }

    private void SetStatus(string msg) => _lblStatusLine.Text = msg;

    private void SafeInvoke(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    // ── Widget factories ──────────────────────────────────────────────────────

    private enum ButtonKind { Normal, Primary, Danger }

    private Panel MakePanel(Rectangle bounds)
    {
        var p = new Panel { Bounds = bounds, BackColor = C_Panel };
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(C_Border);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return p;
    }

    private static Label MakeLabel(string text, Rectangle bounds, Color color,
                                    float px, bool bold = false)
    {
        return new Label
        {
            Text      = text,
            Bounds    = bounds,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font      = UiFont(px, bold),
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private ComboBox MakeComboBox(Rectangle bounds)
    {
        var cb = new ComboBox
        {
            Bounds        = bounds,
            DrawMode      = DrawMode.OwnerDrawFixed,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle     = FlatStyle.Flat,
            BackColor     = C_Ctrl,
            ForeColor     = C_Text,
            Font          = UiFont(10f),
        };

        cb.DrawItem += (_, e) =>
        {
            e.DrawBackground();
            using var backBrush = new SolidBrush(C_Ctrl);
            using var selBrush  = new SolidBrush(C_Primary);
            using var textBrush = new SolidBrush(C_Text);
            bool sel = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.FillRectangle(sel ? selBrush : backBrush, e.Bounds);
            string txt = e.Index >= 0 ? cb.Items[e.Index]?.ToString() ?? "" : cb.Text;
            e.Graphics.DrawString(txt, cb.Font, textBrush, e.Bounds.X + 4, e.Bounds.Y + 5);
            e.DrawFocusRectangle();
        };

        return cb;
    }

    private static Button MakeButton(string text, Rectangle bounds, ButtonKind kind)
    {
        Color back = kind switch
        {
            ButtonKind.Primary => C_Primary,
            ButtonKind.Danger  => C_Danger,
            _                  => C_Btn,
        };
        Color border = kind switch
        {
            ButtonKind.Primary => C_PrimBdr,
            ButtonKind.Danger  => C_DangerHi,
            _                  => C_BtnBdr,
        };

        var btn = new Button
        {
            Text     = text,
            Bounds   = bounds,
            BackColor= back,
            ForeColor= Color.White,
            FlatStyle= FlatStyle.Flat,
            Font     = UiFont(10f),
            UseVisualStyleBackColor = false,
        };

        btn.FlatAppearance.BorderSize         = 1;
        btn.FlatAppearance.BorderColor        = border;
        btn.FlatAppearance.MouseOverBackColor = back;
        btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back, 0.08f);

        void ApplyState()
        {
            btn.BackColor = btn.Enabled ? back : C_Disabled;
            btn.ForeColor = btn.Enabled ? Color.White : C_DisText;
        }

        btn.EnabledChanged += (_, _) => ApplyState();
        ApplyState();
        return btn;
    }

    private static Font UiFont(float px, bool bold = false) =>
        new("Segoe UI", px, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        EmergencyStop();
        _service.Disconnect();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _service.Dispose();
        base.OnFormClosed(e);
    }
}
