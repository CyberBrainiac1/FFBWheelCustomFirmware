using FFBWheelConfig.Controls;
using FFBWheelConfig.Models;
using FFBWheelConfig.Services;

namespace FFBWheelConfig.Forms;

public sealed class MainForm : Form
{
    private const string BaseTitle = "DIY Wheel Config";

    private static readonly Color RootBackColor = ColorTranslator.FromHtml("#202225");
    private static readonly Color PanelBackColor = ColorTranslator.FromHtml("#2A2D31");
    private static readonly Color PanelBorderColor = ColorTranslator.FromHtml("#3A3D42");
    private static readonly Color ControlBackColor = ColorTranslator.FromHtml("#1F2124");
    private static readonly Color ControlBorderColor = ColorTranslator.FromHtml("#4A4E55");
    private static readonly Color ButtonBackColor = ColorTranslator.FromHtml("#3A3F46");
    private static readonly Color ButtonBorderColor = ColorTranslator.FromHtml("#555B64");
    private static readonly Color PrimaryButtonBackColor = ColorTranslator.FromHtml("#2F5F87");
    private static readonly Color PrimaryButtonBorderColor = ColorTranslator.FromHtml("#4C83B1");
    private static readonly Color DisabledButtonBackColor = ColorTranslator.FromHtml("#2B2E33");
    private static readonly Color DisabledTextColor = ColorTranslator.FromHtml("#7E858D");
    private static readonly Color MainTextColor = ColorTranslator.FromHtml("#E6E6E6");
    private static readonly Color SecondaryTextColor = ColorTranslator.FromHtml("#C8CDD4");
    private static readonly Color HelperTextColor = ColorTranslator.FromHtml("#9098A1");
    private static readonly Color RawTextColor = ColorTranslator.FromHtml("#AAB1B9");
    private static readonly Color AngleTextColor = ColorTranslator.FromHtml("#EAF6FF");
    private static readonly Color SliderTrackColor = ColorTranslator.FromHtml("#1E2023");
    private static readonly Color SliderFillColor = ColorTranslator.FromHtml("#3E89B8");

    private readonly WheelControllerService _service = new();

    private ComboBox _cboPort = null!;
    private Button _btnRefresh = null!;
    private Button _btnConnect = null!;
    private Button _btnDisconnect = null!;
    private Label _lblFirmware = null!;
    private Label _lblConnectionStatus = null!;
    private Label _lblAngle = null!;
    private Label _lblRawCounts = null!;
    private FlatSlider _sldRange = null!;
    private NumericUpDown _nudRange = null!;
    private Button _btnCenter = null!;
    private FlatSlider _sldForce = null!;
    private NumericUpDown _nudForce = null!;
    private FlatSlider _sldMinForce = null!;
    private NumericUpDown _nudMinForce = null!;
    private FlatSlider _sldDamping = null!;
    private NumericUpDown _nudDamping = null!;
    private FlatSlider _sldFriction = null!;
    private NumericUpDown _nudFriction = null!;
    private FlatSlider _sldSpring = null!;
    private NumericUpDown _nudSpring = null!;
    private CheckBox _chkInvertEncoder = null!;
    private CheckBox _chkInvertMotor = null!;
    private Button _btnRead = null!;
    private Button _btnApply = null!;
    private Button _btnSave = null!;
    private Button _btnReset = null!;
    private Label _lblStatusLine = null!;

    private bool _updatingControls;
    private bool _isDirty;

    public MainForm()
    {
        InitializeComponent();
        WireServiceEvents();
        RefreshPorts();
        SetConnected(false);
        SetStatusLine("Ready");
        SetDirty(false);
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = BaseTitle;
        BackColor = RootBackColor;
        ForeColor = MainTextColor;
        ClientSize = new Size(760, 540);
        AutoScaleMode = AutoScaleMode.None;
        Font = UiFont(10f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        var topBar = CreatePanel(new Rectangle(16, 12, 728, 58));
        var steeringPanel = CreatePanel(new Rectangle(16, 80, 728, 190));
        var settingsPanel = CreatePanel(new Rectangle(16, 280, 728, 170));
        var bottomBar = CreatePanel(new Rectangle(16, 460, 728, 56));

        BuildTopBar(topBar);
        BuildSteeringPanel(steeringPanel);
        BuildSettingsPanel(settingsPanel);
        BuildBottomBar(bottomBar);

        Controls.AddRange(new Control[] { topBar, steeringPanel, settingsPanel, bottomBar });

        Size = SizeFromClientSize(ClientSize);
        MinimumSize = Size;
        MaximumSize = Size;

        ResumeLayout(false);
    }

    private void BuildTopBar(Panel parent)
    {
        parent.Controls.Add(CreateLabel("COM Port", new Rectangle(12, 19, 64, 20), MainTextColor, 10f));

        _cboPort = CreateComboBox(new Rectangle(82, 14, 110, 28));
        _btnRefresh = CreateButton("Refresh", new Rectangle(204, 14, 72, 28), primary: false);
        _btnConnect = CreateButton("Connect", new Rectangle(286, 14, 78, 28), primary: true);
        _btnDisconnect = CreateButton("Disconnect", new Rectangle(374, 14, 88, 28), primary: false);
        _lblFirmware = CreateLabel("Firmware: —", new Rectangle(492, 10, 220, 16), SecondaryTextColor, 9f);
        _lblConnectionStatus = CreateLabel("Status: Disconnected", new Rectangle(492, 28, 220, 16), SecondaryTextColor, 9f);

        parent.Controls.AddRange(new Control[]
        {
            _cboPort,
            _btnRefresh,
            _btnConnect,
            _btnDisconnect,
            _lblFirmware,
            _lblConnectionStatus
        });
    }

    private void BuildSteeringPanel(Panel parent)
    {
        parent.Controls.Add(CreateLabel("Steering", new Rectangle(12, 8, 80, 18), MainTextColor, 10f, bold: true));

        _lblAngle = CreateLabel("0°", new Rectangle(20, 36, 300, 88), AngleTextColor, 52f, bold: true);
        _lblAngle.TextAlign = ContentAlignment.MiddleLeft;
        _lblRawCounts = CreateLabel("Raw Counts: 0", new Rectangle(24, 128, 240, 18), RawTextColor, 9f);

        parent.Controls.Add(_lblAngle);
        parent.Controls.Add(_lblRawCounts);

        parent.Controls.Add(CreateLabel("Steering Range", new Rectangle(356, 40, 120, 20), MainTextColor, 10f));

        _sldRange = CreateSlider(new Rectangle(356, 70, 240, 28), 90, 1440, 900);
        _nudRange = CreateNumeric(new Rectangle(610, 68, 72, 28), 90, 1440, 900, HorizontalAlignment.Center);
        _btnCenter = CreateButton("Set Center", new Rectangle(356, 118, 118, 30), primary: false);

        parent.Controls.Add(_sldRange);
        parent.Controls.Add(_nudRange);
        parent.Controls.Add(CreateLabel("°", new Rectangle(688, 72, 18, 20), SecondaryTextColor, 10f));
        parent.Controls.Add(_btnCenter);
        parent.Controls.Add(CreateLabel(
            "Uses same processed steering value as device output",
            new Rectangle(356, 156, 320, 16),
            HelperTextColor,
            8f));
    }

    private void BuildSettingsPanel(Panel parent)
    {
        parent.Controls.Add(CreateLabel("Force / Settings", new Rectangle(12, 8, 120, 18), MainTextColor, 10f, bold: true));

        (_sldForce, _nudForce) = AddSettingRow(parent, "Overall Force", 34, 0, 100, 60);
        (_sldMinForce, _nudMinForce) = AddSettingRow(parent, "Minimum Force", 60, 0, 20, 5);
        (_sldDamping, _nudDamping) = AddSettingRow(parent, "Damping", 86, 0, 100, 10);
        (_sldFriction, _nudFriction) = AddSettingRow(parent, "Friction", 112, 0, 100, 4);
        (_sldSpring, _nudSpring) = AddSettingRow(parent, "Spring", 138, 0, 100, 15);

        _chkInvertEncoder = CreateCheckBox("Invert Encoder", new Rectangle(470, 50, 160, 24));
        _chkInvertMotor = CreateCheckBox("Invert Motor", new Rectangle(470, 84, 160, 24));

        parent.Controls.Add(_chkInvertEncoder);
        parent.Controls.Add(_chkInvertMotor);
    }

    private void BuildBottomBar(Panel parent)
    {
        _btnRead = CreateButton("Read from Wheel", new Rectangle(12, 14, 118, 28), primary: false);
        _btnApply = CreateButton("Apply", new Rectangle(140, 14, 72, 28), primary: true);
        _btnSave = CreateButton("Save to Wheel", new Rectangle(222, 14, 102, 28), primary: false);
        _btnReset = CreateButton("Reset Defaults", new Rectangle(334, 14, 108, 28), primary: false);
        _lblStatusLine = CreateLabel("Ready", new Rectangle(460, 18, 248, 18), SecondaryTextColor, 9f);
        _lblStatusLine.TextAlign = ContentAlignment.MiddleRight;
        _lblStatusLine.AutoEllipsis = true;

        parent.Controls.AddRange(new Control[]
        {
            _btnRead,
            _btnApply,
            _btnSave,
            _btnReset,
            _lblStatusLine
        });
    }

    private (FlatSlider slider, NumericUpDown numeric) AddSettingRow(Panel parent, string labelText, int y, int min, int max, int value)
    {
        parent.Controls.Add(CreateLabel(labelText, new Rectangle(16, y, 110, 20), MainTextColor, 10f));

        var slider = CreateSlider(new Rectangle(132, y - 2, 220, 24), min, max, value);
        var numeric = CreateNumeric(new Rectangle(364, y - 4, 54, 28), min, max, value, HorizontalAlignment.Center);

        parent.Controls.Add(slider);
        parent.Controls.Add(numeric);

        return (slider, numeric);
    }

    private static Panel CreatePanel(Rectangle bounds)
    {
        var panel = new Panel
        {
            Bounds = bounds,
            BackColor = PanelBackColor
        };

        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(PanelBorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        return panel;
    }

    private static Label CreateLabel(string text, Rectangle bounds, Color color, float pixelSize, bool bold = false)
    {
        return new Label
        {
            Text = text,
            Bounds = bounds,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = UiFont(pixelSize, bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static ComboBox CreateComboBox(Rectangle bounds)
    {
        var combo = new ComboBox
        {
            Bounds = bounds,
            DrawMode = DrawMode.OwnerDrawFixed,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = ControlBackColor,
            ForeColor = MainTextColor,
            Font = UiFont(10f)
        };

        combo.DrawItem += (_, e) =>
        {
            e.DrawBackground();
            using var backBrush = new SolidBrush(ControlBackColor);
            using var selectBrush = new SolidBrush(PrimaryButtonBackColor);
            using var textBrush = new SolidBrush(MainTextColor);

            var back = (e.State & DrawItemState.Selected) == DrawItemState.Selected ? selectBrush : backBrush;
            e.Graphics.FillRectangle(back, e.Bounds);

            string text = e.Index >= 0
                ? combo.Items[e.Index]?.ToString() ?? string.Empty
                : combo.Text;

            e.Graphics.DrawString(text, combo.Font, textBrush, e.Bounds.X + 4, e.Bounds.Y + 5);

            e.DrawFocusRectangle();
        };

        return combo;
    }

    private static Button CreateButton(string text, Rectangle bounds, bool primary)
    {
        Color backColor = primary ? PrimaryButtonBackColor : ButtonBackColor;
        Color borderColor = primary ? PrimaryButtonBorderColor : ButtonBorderColor;

        var button = new Button
        {
            Text = text,
            Bounds = bounds,
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UiFont(10f),
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = borderColor;
        button.FlatAppearance.MouseOverBackColor = backColor;
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(backColor, 0.08f);

        void ApplyVisualState()
        {
            button.BackColor = button.Enabled ? backColor : DisabledButtonBackColor;
            button.ForeColor = button.Enabled ? (primary ? Color.White : MainTextColor) : DisabledTextColor;
        }

        button.EnabledChanged += (_, _) => ApplyVisualState();
        ApplyVisualState();
        return button;
    }

    private static NumericUpDown CreateNumeric(Rectangle bounds, int min, int max, int value, HorizontalAlignment alignment)
    {
        return new NumericUpDown
        {
            Bounds = bounds,
            Minimum = min,
            Maximum = max,
            Value = value,
            TextAlign = alignment,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ControlBackColor,
            ForeColor = MainTextColor,
            Font = UiFont(10f)
        };
    }

    private static CheckBox CreateCheckBox(string text, Rectangle bounds)
    {
        var checkBox = new CheckBox
        {
            Text = text,
            Bounds = bounds,
            BackColor = Color.Transparent,
            ForeColor = MainTextColor,
            FlatStyle = FlatStyle.Flat,
            Font = UiFont(10f)
        };

        checkBox.FlatAppearance.BorderColor = ControlBorderColor;
        checkBox.FlatAppearance.CheckedBackColor = PanelBackColor;
        checkBox.FlatAppearance.MouseDownBackColor = PanelBackColor;
        checkBox.FlatAppearance.MouseOverBackColor = PanelBackColor;
        return checkBox;
    }

    private static FlatSlider CreateSlider(Rectangle bounds, int min, int max, int value)
    {
        return new FlatSlider
        {
            Bounds = bounds,
            Minimum = min,
            Maximum = max,
            Value = value,
            TrackColor = SliderTrackColor,
            FillColor = SliderFillColor,
            ThumbColor = MainTextColor,
            BorderColor = ControlBorderColor
        };
    }

    private static Font UiFont(float pixelSize, bool bold = false)
    {
        return new Font("Segoe UI", pixelSize, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
    }

    private void WireServiceEvents()
    {
        _btnRefresh.Click += (_, _) => RefreshPorts();
        _btnConnect.Click += BtnConnect_Click;
        _btnDisconnect.Click += BtnDisconnect_Click;
        _btnRead.Click += (_, _) => ReadSettings();
        _btnApply.Click += (_, _) => ApplySettings();
        _btnSave.Click += (_, _) => SaveSettings();
        _btnReset.Click += (_, _) => ResetDefaults();
        _btnCenter.Click += (_, _) => SetCenter();

        LinkSliderAndNumeric(_sldRange, _nudRange, 90, 1440);
        LinkSliderAndNumeric(_sldForce, _nudForce, 0, 100);
        LinkSliderAndNumeric(_sldMinForce, _nudMinForce, 0, 20);
        LinkSliderAndNumeric(_sldDamping, _nudDamping, 0, 100);
        LinkSliderAndNumeric(_sldFriction, _nudFriction, 0, 100);
        LinkSliderAndNumeric(_sldSpring, _nudSpring, 0, 100);

        _chkInvertEncoder.CheckedChanged += (_, _) => MarkDirty();
        _chkInvertMotor.CheckedChanged += (_, _) => MarkDirty();

        _service.SettingsUpdated += settings => SafeInvoke(() => LoadSettingsIntoUi(settings));
        _service.LiveAngleUpdated += angle => SafeInvoke(() => _lblAngle.Text = $"{angle:F0}°");
        _service.RawCountsUpdated += counts => SafeInvoke(() => _lblRawCounts.Text = $"Raw Counts: {counts}");
        _service.Disconnected += () => SafeInvoke(() =>
        {
            SetConnected(false);
            SetStatusLine("Device disconnected");
        });
    }

    private void LinkSliderAndNumeric(FlatSlider slider, NumericUpDown numeric, int min, int max)
    {
        slider.ValueChanged += (_, _) =>
        {
            if (_updatingControls)
                return;

            _updatingControls = true;
            numeric.Value = Math.Clamp(slider.Value, min, max);
            _updatingControls = false;
            MarkDirty();
        };

        numeric.ValueChanged += (_, _) =>
        {
            if (_updatingControls)
                return;

            _updatingControls = true;
            slider.Value = Math.Clamp((int)numeric.Value, min, max);
            _updatingControls = false;
            MarkDirty();
        };
    }

    private void SafeInvoke(Action action)
    {
        if (IsDisposed)
            return;

        if (InvokeRequired)
            BeginInvoke(action);
        else
            action();
    }

    private void RefreshPorts()
    {
        string? selected = _cboPort.SelectedItem?.ToString();
        _cboPort.Items.Clear();

        string[] ports = WheelControllerService.GetAvailablePorts();
        if (ports.Length == 0)
        {
            _cboPort.Items.Add("(none)");
            _cboPort.SelectedIndex = 0;
            return;
        }

        foreach (string port in ports)
            _cboPort.Items.Add(port);

        _cboPort.SelectedItem = selected != null && _cboPort.Items.Contains(selected)
            ? selected
            : _cboPort.Items[0];
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_cboPort.SelectedItem?.ToString() is not string port || port.StartsWith('('))
            return;

        _btnConnect.Enabled = false;
        _lblConnectionStatus.Text = "Status: Connecting...";
        SetStatusLine($"Connecting to {port}");

        bool connected = await Task.Run(() => _service.Connect(port));

        if (!connected)
        {
            SetConnected(false);
            SetStatusLine($"Unable to connect to {port}");
            return;
        }

        SetConnected(true);
        SetStatusLine($"Connected to {port}");
        _service.ReadSettings();
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        _service.Disconnect();
        SetConnected(false);
        SetStatusLine("Device disconnected");
    }

    private void ReadSettings()
    {
        if (!_service.IsConnected)
            return;

        SetStatusLine("Reading from wheel");
        if (!_service.ReadSettings())
            SetStatusLine("Invalid response");
    }

    private void ApplySettings()
    {
        if (!_service.IsConnected)
            return;

        if (_service.ApplySettings(GatherSettings()))
            SetStatusLine("Settings applied");
        else
            SetStatusLine("Invalid response");
    }

    private void SaveSettings()
    {
        if (!_service.IsConnected)
            return;

        if (_service.SaveSettings())
        {
            SetStatusLine("EEPROM saved");
            SetDirty(false);
        }
        else
        {
            SetStatusLine("Invalid response");
        }
    }

    private void ResetDefaults()
    {
        if (!_service.IsConnected)
            return;

        var result = MessageBox.Show(
            this,
            "Reset all settings to defaults?",
            BaseTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
            return;

        if (_service.ResetDefaults())
            SetStatusLine("Defaults restored");
        else
            SetStatusLine("Invalid response");
    }

    private void SetCenter()
    {
        if (!_service.IsConnected)
            return;

        if (_service.SetCenter())
            SetStatusLine("Center set");
        else
            SetStatusLine("Invalid response");
    }

    private void LoadSettingsIntoUi(WheelSettings settings)
    {
        _updatingControls = true;
        _sldRange.Value = Math.Clamp(settings.SteeringRange, 90, 1440);
        _nudRange.Value = Math.Clamp(settings.SteeringRange, 90, 1440);
        _sldForce.Value = Math.Clamp(settings.OverallForce, 0, 100);
        _nudForce.Value = Math.Clamp(settings.OverallForce, 0, 100);
        _sldMinForce.Value = Math.Clamp(settings.MinimumForce, 0, 20);
        _nudMinForce.Value = Math.Clamp(settings.MinimumForce, 0, 20);
        _sldDamping.Value = Math.Clamp(settings.Damping, 0, 100);
        _nudDamping.Value = Math.Clamp(settings.Damping, 0, 100);
        _sldFriction.Value = Math.Clamp(settings.Friction, 0, 100);
        _nudFriction.Value = Math.Clamp(settings.Friction, 0, 100);
        _sldSpring.Value = Math.Clamp(settings.Spring, 0, 100);
        _nudSpring.Value = Math.Clamp(settings.Spring, 0, 100);
        _chkInvertEncoder.Checked = settings.InvertEncoder;
        _chkInvertMotor.Checked = settings.InvertMotor;
        _updatingControls = false;

        _lblFirmware.Text = string.IsNullOrWhiteSpace(settings.FirmwareVersion)
            ? "Firmware: —"
            : $"Firmware: {settings.FirmwareVersion}";

        SetDirty(false);
        SetStatusLine("Ready");
    }

    private WheelSettings GatherSettings()
    {
        return new WheelSettings
        {
            SteeringRange = (int)_nudRange.Value,
            OverallForce = (int)_nudForce.Value,
            MinimumForce = (int)_nudMinForce.Value,
            Damping = (int)_nudDamping.Value,
            Friction = (int)_nudFriction.Value,
            Spring = (int)_nudSpring.Value,
            InvertEncoder = _chkInvertEncoder.Checked,
            InvertMotor = _chkInvertMotor.Checked
        };
    }

    private void SetConnected(bool connected)
    {
        _btnConnect.Enabled = !connected;
        _btnDisconnect.Enabled = connected;

        bool settingsEnabled = connected;
        _btnRead.Enabled = settingsEnabled;
        _btnApply.Enabled = settingsEnabled;
        _btnSave.Enabled = settingsEnabled;
        _btnReset.Enabled = settingsEnabled;
        _sldRange.Enabled = settingsEnabled;
        _nudRange.Enabled = settingsEnabled;
        _btnCenter.Enabled = settingsEnabled;
        _sldForce.Enabled = settingsEnabled;
        _nudForce.Enabled = settingsEnabled;
        _sldMinForce.Enabled = settingsEnabled;
        _nudMinForce.Enabled = settingsEnabled;
        _sldDamping.Enabled = settingsEnabled;
        _nudDamping.Enabled = settingsEnabled;
        _sldFriction.Enabled = settingsEnabled;
        _nudFriction.Enabled = settingsEnabled;
        _sldSpring.Enabled = settingsEnabled;
        _nudSpring.Enabled = settingsEnabled;
        _chkInvertEncoder.Enabled = settingsEnabled;
        _chkInvertMotor.Enabled = settingsEnabled;

        _lblConnectionStatus.Text = connected ? "Status: Connected" : "Status: Disconnected";

        if (!connected)
        {
            _lblAngle.Text = "0°";
            _lblRawCounts.Text = "Raw Counts: 0";
            _lblFirmware.Text = "Firmware: —";
        }
    }

    private void SetStatusLine(string message)
    {
        _lblStatusLine.Text = message;
    }

    private void MarkDirty()
    {
        if (_updatingControls)
            return;

        SetDirty(true);
    }

    private void SetDirty(bool dirty)
    {
        if (_isDirty == dirty)
            return;

        _isDirty = dirty;
        Text = dirty ? $"{BaseTitle} *" : BaseTitle;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _service.Dispose();
        base.OnFormClosed(e);
    }
}
