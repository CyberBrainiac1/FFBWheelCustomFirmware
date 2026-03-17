using System.Globalization;
using FFBWheelConfig.Controls;
using FFBWheelConfig.Models;
using FFBWheelConfig.Services;

namespace FFBWheelConfig.Forms;

public sealed class MainForm : Form
{
    private const string BaseTitle = "EMC Utility Lite - DIY";

    private static readonly Color RootBackColor = ColorTranslator.FromHtml("#14161A");
    private static readonly Color PanelBackColor = ColorTranslator.FromHtml("#23262B");
    private static readonly Color PanelBorderColor = ColorTranslator.FromHtml("#343841");
    private static readonly Color ControlBackColor = ColorTranslator.FromHtml("#181B20");
    private static readonly Color ControlBorderColor = ColorTranslator.FromHtml("#4A4E55");
    private static readonly Color ButtonBackColor = ColorTranslator.FromHtml("#2D3138");
    private static readonly Color ButtonBorderColor = ColorTranslator.FromHtml("#4E545E");
    private static readonly Color PrimaryButtonBackColor = ColorTranslator.FromHtml("#169BD7");
    private static readonly Color PrimaryButtonBorderColor = ColorTranslator.FromHtml("#4EBBE8");
    private static readonly Color DisabledButtonBackColor = ColorTranslator.FromHtml("#24282E");
    private static readonly Color DisabledTextColor = ColorTranslator.FromHtml("#6F7782");
    private static readonly Color MainTextColor = ColorTranslator.FromHtml("#E6EAF0");
    private static readonly Color SecondaryTextColor = ColorTranslator.FromHtml("#AEB6C2");
    private static readonly Color HelperTextColor = ColorTranslator.FromHtml("#7F8792");
    private static readonly Color AngleTextColor = ColorTranslator.FromHtml("#F3F7FB");
    private static readonly Color SliderTrackColor = ColorTranslator.FromHtml("#1D2025");
    private static readonly Color SliderFillColor = ColorTranslator.FromHtml("#169BD7");

    private readonly WheelControllerService _service = new();
    private readonly ProfileStore _profileStore = new();

    private ComboBox _cboPort = null!;
    private Button _btnRefresh = null!;
    private Button _btnConnect = null!;
    private Button _btnDisconnect = null!;
    private Button _btnSteering = null!;
    private Button _btnPedal = null!;
    private Button _btnForce = null!;
    private Button _btnSettings = null!;

    private Panel _pageHost = null!;
    private Panel _pageSteering = null!;
    private Panel _pagePedal = null!;
    private Panel _pageForce = null!;
    private Panel _settingsOverlay = null!;

    private Label _lblAngle = null!;
    private Label _lblRawCounts = null!;
    private FlatSlider _sldAnglePreview = null!;
    private Button _btnCenter = null!;
    private ComboBox _cboProfile = null!;
    private Button _btnProfileLoad = null!;
    private Button _btnProfileSave = null!;
    private Button _btnProfileDelete = null!;
    private Label _lblProduct = null!;

    private FlatSlider _sldOverallForce = null!;
    private NumericUpDown _nudOverallForce = null!;
    private FlatSlider _sldSoftLock = null!;
    private NumericUpDown _nudSoftLock = null!;
    private FlatSlider _sldDamper = null!;
    private NumericUpDown _nudDamper = null!;
    private FlatSlider _sldFriction = null!;
    private NumericUpDown _nudFriction = null!;
    private FlatSlider _sldSpring = null!;
    private NumericUpDown _nudSpring = null!;
    private FlatSlider _sldMinForce = null!;
    private NumericUpDown _nudMinForce = null!;
    private Button _btnRead = null!;
    private Button _btnApply = null!;
    private Button _btnSave = null!;
    private Button _btnReset = null!;

    private TextBox _txtCpr = null!;
    private ComboBox _cboHBridge = null!;
    private CheckBox _chkInvertEncoder = null!;
    private CheckBox _chkInvertMotor = null!;
    private Button _btnSettingsApply = null!;
    private Button _btnSettingsClose = null!;

    private Label _lblFirmware = null!;
    private Label _lblConnectionStatus = null!;
    private Label _lblStatusLine = null!;

    private bool _updatingControls;
    private bool _isDirty;

    public MainForm()
    {
        InitializeComponent();
        WireServiceEvents();
        RefreshPorts();
        RefreshProfiles();
        LoadSettingsIntoUi(new WheelSettings());
        ShowPage(_pageSteering, _btnSteering);
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
        ClientSize = new Size(348, 496);
        AutoScaleMode = AutoScaleMode.None;
        Font = UiFont(10f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(CreateLabel("EMC Utility Lite", new Rectangle(16, 12, 220, 20), MainTextColor, 16f, bold: true));
        Controls.Add(CreateLabel("DIY serial build based on legacy EMC Lite workflow", new Rectangle(16, 32, 300, 16), HelperTextColor, 8f));

        _cboPort = CreateComboBox(new Rectangle(16, 56, 116, 26));
        _btnRefresh = CreateButton("Refresh", new Rectangle(140, 56, 58, 26), primary: false);
        _btnConnect = CreateButton("Connect", new Rectangle(204, 56, 58, 26), primary: true);
        _btnDisconnect = CreateButton("Disconnect", new Rectangle(268, 56, 64, 26), primary: false);
        Controls.AddRange(new Control[] { _cboPort, _btnRefresh, _btnConnect, _btnDisconnect });

        _btnSteering = CreateNavButton("Steering", new Rectangle(16, 94, 74, 28));
        _btnPedal = CreateNavButton("Pedal", new Rectangle(96, 94, 74, 28));
        _btnForce = CreateNavButton("Force", new Rectangle(176, 94, 74, 28));
        _btnSettings = CreateNavButton("Settings", new Rectangle(256, 94, 76, 28));
        Controls.AddRange(new Control[] { _btnSteering, _btnPedal, _btnForce, _btnSettings });

        _pageHost = CreatePanel(new Rectangle(16, 132, 316, 286));
        Controls.Add(_pageHost);

        BuildSteeringPage();
        BuildPedalPage();
        BuildForcePage();
        BuildSettingsOverlay();

        _pageHost.Controls.AddRange(new Control[] { _pageSteering, _pagePedal, _pageForce, _settingsOverlay });

        _lblFirmware = CreateLabel("Firmware ver : Unknown", new Rectangle(16, 428, 200, 18), SecondaryTextColor, 9f);
        _lblConnectionStatus = CreateLabel("Status: Disconnected", new Rectangle(16, 446, 160, 18), SecondaryTextColor, 9f);
        _lblStatusLine = CreateLabel("Ready", new Rectangle(178, 446, 154, 18), SecondaryTextColor, 9f);
        _lblStatusLine.TextAlign = ContentAlignment.MiddleRight;
        _lblStatusLine.AutoEllipsis = true;
        Controls.AddRange(new Control[] { _lblFirmware, _lblConnectionStatus, _lblStatusLine });

        Size = SizeFromClientSize(ClientSize);
        MinimumSize = Size;
        MaximumSize = Size;

        ResumeLayout(false);
    }

    private void BuildSteeringPage()
    {
        _pageSteering = CreatePagePanel();

        _lblAngle = CreateLabel("0°", new Rectangle(16, 14, 280, 58), AngleTextColor, 44f, bold: true);
        _lblRawCounts = CreateLabel("Raw Counts: 0", new Rectangle(18, 74, 220, 16), HelperTextColor, 8f);
        _sldAnglePreview = CreateSlider(new Rectangle(18, 98, 264, 18), -450, 450, 0);
        _sldAnglePreview.Enabled = false;
        _btnCenter = CreateButton("Center", new Rectangle(18, 126, 96, 26), primary: false);

        _pageSteering.Controls.Add(_lblAngle);
        _pageSteering.Controls.Add(_lblRawCounts);
        _pageSteering.Controls.Add(_sldAnglePreview);
        _pageSteering.Controls.Add(_btnCenter);

        _pageSteering.Controls.Add(CreateLabel("Profile", new Rectangle(18, 168, 80, 16), SecondaryTextColor, 9f));
        _cboProfile = CreateComboBox(new Rectangle(18, 188, 264, 26), editable: true);
        _btnProfileLoad = CreateButton("Load", new Rectangle(18, 224, 82, 26), primary: false);
        _btnProfileSave = CreateButton("Save", new Rectangle(109, 224, 82, 26), primary: true);
        _btnProfileDelete = CreateButton("Delete", new Rectangle(200, 224, 82, 26), primary: false);
        _lblProduct = CreateLabel(string.Empty, new Rectangle(18, 258, 264, 18), HelperTextColor, 8f);

        _pageSteering.Controls.AddRange(new Control[]
        {
            _cboProfile,
            _btnProfileLoad,
            _btnProfileSave,
            _btnProfileDelete,
            _lblProduct
        });
    }

    private void BuildPedalPage()
    {
        _pagePedal = CreatePagePanel();

        _pagePedal.Controls.Add(CreateLabel(
            "Legacy EMC Lite exposed pedal calibration here.\r\nThis serial firmware currently ships wheel setup only.",
            new Rectangle(18, 16, 270, 36),
            HelperTextColor,
            8f));

        AddPedalRow(_pagePedal, "Throttle", 66);
        AddPedalRow(_pagePedal, "Brake", 112);
        AddPedalToggleRow(_pagePedal, "Clutch", 166);
        AddPedalToggleRow(_pagePedal, "Handbrake", 208);
    }

    private void BuildForcePage()
    {
        _pageForce = CreatePagePanel();

        (_sldSoftLock, _nudSoftLock) = AddSettingRow(_pageForce, "Soft Lock", 14, 90, 1440, 900);
        (_sldOverallForce, _nudOverallForce) = AddSettingRow(_pageForce, "Overall", 50, 0, 100, 60);
        (_sldDamper, _nudDamper) = AddSettingRow(_pageForce, "Damper", 86, 0, 100, 10);
        (_sldFriction, _nudFriction) = AddSettingRow(_pageForce, "Friction", 122, 0, 100, 4);
        (_sldSpring, _nudSpring) = AddSettingRow(_pageForce, "Spring", 158, 0, 100, 15);
        (_sldMinForce, _nudMinForce) = AddSettingRow(_pageForce, "Min Force", 194, 0, 20, 5);

        _btnRead = CreateButton("Read", new Rectangle(18, 236, 64, 26), primary: false);
        _btnApply = CreateButton("Apply", new Rectangle(88, 236, 64, 26), primary: true);
        _btnSave = CreateButton("Save", new Rectangle(158, 236, 64, 26), primary: false);
        _btnReset = CreateButton("Reset", new Rectangle(228, 236, 64, 26), primary: false);

        _pageForce.Controls.AddRange(new Control[] { _btnRead, _btnApply, _btnSave, _btnReset });
    }

    private void BuildSettingsOverlay()
    {
        _settingsOverlay = CreatePagePanel();
        _settingsOverlay.Visible = false;

        _settingsOverlay.Controls.Add(CreateLabel("Settings", new Rectangle(18, 16, 120, 18), MainTextColor, 12f, bold: true));
        _settingsOverlay.Controls.Add(CreateLabel(
            "Read-only legacy EMC Lite fields are shown here for reference.\r\nThis serial firmware fixes CPR and H-bridge mode at build time.",
            new Rectangle(18, 42, 272, 34),
            HelperTextColor,
            8f));

        _settingsOverlay.Controls.Add(CreateLabel("Encoder CPR", new Rectangle(18, 92, 86, 18), MainTextColor, 9f));
        _txtCpr = CreateTextBox(new Rectangle(116, 88, 82, 26), readOnly: true);
        _settingsOverlay.Controls.Add(_txtCpr);

        _settingsOverlay.Controls.Add(CreateLabel("H-Bridge", new Rectangle(18, 128, 86, 18), MainTextColor, 9f));
        _cboHBridge = CreateComboBox(new Rectangle(116, 124, 160, 26));
        _cboHBridge.Items.AddRange(new object[] { "2PWM + 1DIR", "2PWM + 2DIR", "1PWM + 1DIR", "1PWM + 2DIR" });
        _cboHBridge.Enabled = false;
        _settingsOverlay.Controls.Add(_cboHBridge);

        _chkInvertEncoder = CreateCheckBox("Invert Encoder", new Rectangle(18, 172, 130, 22));
        _chkInvertMotor = CreateCheckBox("Invert Motor", new Rectangle(18, 198, 130, 22));
        _settingsOverlay.Controls.AddRange(new Control[] { _chkInvertEncoder, _chkInvertMotor });

        _btnSettingsApply = CreateButton("Apply", new Rectangle(116, 238, 72, 26), primary: true);
        _btnSettingsClose = CreateButton("Close", new Rectangle(198, 238, 78, 26), primary: false);
        _settingsOverlay.Controls.AddRange(new Control[] { _btnSettingsApply, _btnSettingsClose });
    }

    private (FlatSlider slider, NumericUpDown numeric) AddSettingRow(Panel parent, string labelText, int y, int min, int max, int value)
    {
        parent.Controls.Add(CreateLabel(labelText, new Rectangle(18, y + 2, 70, 18), MainTextColor, 9f));

        var slider = CreateSlider(new Rectangle(92, y, 112, 24), min, max, value);
        var numeric = CreateNumeric(new Rectangle(214, y - 2, 62, 26), min, max, value);
        parent.Controls.AddRange(new Control[] { slider, numeric });
        return (slider, numeric);
    }

    private void AddPedalRow(Panel parent, string label, int y)
    {
        parent.Controls.Add(CreateLabel(label, new Rectangle(18, y, 68, 18), MainTextColor, 9f));

        var barFrame = new Panel
        {
            Bounds = new Rectangle(92, y + 2, 120, 12),
            BackColor = ControlBackColor
        };
        barFrame.Paint += (_, e) =>
        {
            using var pen = new Pen(ControlBorderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, barFrame.Width - 1, barFrame.Height - 1);
        };

        var fill = new Panel
        {
            Bounds = new Rectangle(1, 1, 0, 10),
            BackColor = PrimaryButtonBackColor
        };
        barFrame.Controls.Add(fill);

        var btnMin = CreateButton("Min", new Rectangle(220, y - 4, 40, 24), primary: false);
        var btnMax = CreateButton("Max", new Rectangle(266, y - 4, 40, 24), primary: false);
        btnMin.Enabled = false;
        btnMax.Enabled = false;

        parent.Controls.AddRange(new Control[] { barFrame, btnMin, btnMax });
    }

    private void AddPedalToggleRow(Panel parent, string label, int y)
    {
        var checkBox = CreateCheckBox(label, new Rectangle(18, y, 96, 22));
        checkBox.Enabled = false;
        var btnMin = CreateButton("Min", new Rectangle(220, y - 2, 40, 24), primary: false);
        var btnMax = CreateButton("Max", new Rectangle(266, y - 2, 40, 24), primary: false);
        btnMin.Enabled = false;
        btnMax.Enabled = false;

        parent.Controls.AddRange(new Control[] { checkBox, btnMin, btnMax });
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

    private static Panel CreatePagePanel()
    {
        return new Panel
        {
            Bounds = new Rectangle(1, 1, 314, 284),
            BackColor = PanelBackColor
        };
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

    private static Button CreateNavButton(string text, Rectangle bounds)
    {
        return CreateButton(text, bounds, primary: false);
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
            ForeColor = primary ? Color.White : MainTextColor,
            FlatStyle = FlatStyle.Flat,
            Font = UiFont(9f),
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

    private static ComboBox CreateComboBox(Rectangle bounds, bool editable = false)
    {
        return new ComboBox
        {
            Bounds = bounds,
            DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = ControlBackColor,
            ForeColor = MainTextColor,
            Font = UiFont(9f)
        };
    }

    private static TextBox CreateTextBox(Rectangle bounds, bool readOnly = false)
    {
        return new TextBox
        {
            Bounds = bounds,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ControlBackColor,
            ForeColor = MainTextColor,
            Font = UiFont(9f),
            ReadOnly = readOnly
        };
    }

    private static NumericUpDown CreateNumeric(Rectangle bounds, int min, int max, int value)
    {
        return new NumericUpDown
        {
            Bounds = bounds,
            Minimum = min,
            Maximum = max,
            Value = value,
            TextAlign = HorizontalAlignment.Center,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ControlBackColor,
            ForeColor = MainTextColor,
            Font = UiFont(9f)
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
            Font = UiFont(9f)
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

        _btnSteering.Click += (_, _) => ShowPage(_pageSteering, _btnSteering);
        _btnPedal.Click += (_, _) => ShowPage(_pagePedal, _btnPedal);
        _btnForce.Click += (_, _) => ShowPage(_pageForce, _btnForce);
        _btnSettings.Click += (_, _) => ToggleSettingsOverlay(true);
        _btnSettingsClose.Click += (_, _) => ToggleSettingsOverlay(false);
        _btnSettingsApply.Click += (_, _) =>
        {
            ToggleSettingsOverlay(false);
            ApplySettings();
        };

        _btnCenter.Click += (_, _) => SetCenter();
        _btnProfileLoad.Click += (_, _) => LoadSelectedProfile();
        _btnProfileSave.Click += (_, _) => SaveSelectedProfile();
        _btnProfileDelete.Click += (_, _) => DeleteSelectedProfile();

        _btnRead.Click += (_, _) => ReadSettings();
        _btnApply.Click += (_, _) => ApplySettings();
        _btnSave.Click += (_, _) => SaveSettings();
        _btnReset.Click += (_, _) => ResetDefaults();

        LinkSliderAndNumeric(_sldSoftLock, _nudSoftLock);
        LinkSliderAndNumeric(_sldOverallForce, _nudOverallForce);
        LinkSliderAndNumeric(_sldDamper, _nudDamper);
        LinkSliderAndNumeric(_sldFriction, _nudFriction);
        LinkSliderAndNumeric(_sldSpring, _nudSpring);
        LinkSliderAndNumeric(_sldMinForce, _nudMinForce);

        _sldSoftLock.ValueChanged += (_, _) => SyncAnglePreviewBounds();
        _nudSoftLock.ValueChanged += (_, _) => SyncAnglePreviewBounds();

        _chkInvertEncoder.CheckedChanged += (_, _) => MarkDirty();
        _chkInvertMotor.CheckedChanged += (_, _) => MarkDirty();

        _service.SettingsUpdated += settings => SafeInvoke(() => LoadSettingsIntoUi(settings));
        _service.LiveAngleUpdated += angle => SafeInvoke(() => UpdateLiveAngle(angle));
        _service.RawCountsUpdated += counts => SafeInvoke(() => _lblRawCounts.Text = $"Raw Counts: {counts}");
        _service.Disconnected += () => SafeInvoke(() =>
        {
            SetConnected(false);
            SetStatusLine("Device disconnected");
        });
    }

    private void LinkSliderAndNumeric(FlatSlider slider, NumericUpDown numeric)
    {
        slider.ValueChanged += (_, _) =>
        {
            if (_updatingControls)
                return;

            _updatingControls = true;
            numeric.Value = Math.Clamp(slider.Value, (int)numeric.Minimum, (int)numeric.Maximum);
            _updatingControls = false;
            MarkDirty();
        };

        numeric.ValueChanged += (_, _) =>
        {
            if (_updatingControls)
                return;

            _updatingControls = true;
            slider.Value = Math.Clamp((int)numeric.Value, slider.Minimum, slider.Maximum);
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

    private void ShowPage(Panel page, Button activeButton)
    {
        _pageSteering.Visible = ReferenceEquals(page, _pageSteering);
        _pagePedal.Visible = ReferenceEquals(page, _pagePedal);
        _pageForce.Visible = ReferenceEquals(page, _pageForce);
        _settingsOverlay.Visible = false;

        StyleNavButton(_btnSteering, ReferenceEquals(activeButton, _btnSteering));
        StyleNavButton(_btnPedal, ReferenceEquals(activeButton, _btnPedal));
        StyleNavButton(_btnForce, ReferenceEquals(activeButton, _btnForce));
        StyleNavButton(_btnSettings, false);
    }

    private void ToggleSettingsOverlay(bool visible)
    {
        _settingsOverlay.Visible = visible;
        if (visible)
        {
            _settingsOverlay.BringToFront();
            StyleNavButton(_btnSettings, true);
        }
        else
        {
            StyleNavButton(_btnSettings, false);
        }
    }

    private static void StyleNavButton(Button button, bool active)
    {
        button.BackColor = active ? PrimaryButtonBackColor : ButtonBackColor;
        button.ForeColor = active ? Color.White : MainTextColor;
        button.FlatAppearance.BorderColor = active ? PrimaryButtonBorderColor : ButtonBorderColor;
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

    private void RefreshProfiles()
    {
        string selected = _cboProfile.Text.Trim();
        _cboProfile.Items.Clear();

        foreach (WheelProfile profile in _profileStore.LoadAll())
            _cboProfile.Items.Add(profile.Name);

        if (!string.IsNullOrWhiteSpace(selected))
        {
            _cboProfile.Text = selected;
        }
        else if (_cboProfile.Items.Count > 0)
        {
            _cboProfile.SelectedIndex = 0;
        }
        else
        {
            _cboProfile.Text = "Default";
        }
    }

    private void LoadSelectedProfile()
    {
        string name = NormalizeProfileName(_cboProfile.Text);
        WheelProfile? profile = _profileStore.Load(name);
        if (profile == null)
        {
            SetStatusLine($"Profile {name} not found");
            return;
        }

        LoadSettingsIntoUi(profile.Settings);
        _cboProfile.Text = profile.Name;
        SetStatusLine($"Loaded profile {profile.Name}");
    }

    private void SaveSelectedProfile()
    {
        string name = NormalizeProfileName(_cboProfile.Text);
        _profileStore.Save(name, GatherSettings());
        RefreshProfiles();
        _cboProfile.Text = name;
        SetStatusLine($"Saved profile {name}");
    }

    private void DeleteSelectedProfile()
    {
        string name = NormalizeProfileName(_cboProfile.Text);
        if (!_profileStore.Delete(name))
        {
            SetStatusLine($"Profile {name} not found");
            return;
        }

        RefreshProfiles();
        SetStatusLine($"Deleted profile {name}");
    }

    private static string NormalizeProfileName(string? name)
    {
        string trimmed = (name ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "Default" : trimmed;
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
            "Reset wheel settings to compiled defaults?",
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

    private void UpdateLiveAngle(double angle)
    {
        int shown = (int)Math.Round(angle);
        _lblAngle.Text = $"{shown}°";
        _sldAnglePreview.Value = Math.Clamp(shown, _sldAnglePreview.Minimum, _sldAnglePreview.Maximum);
    }

    private void SyncAnglePreviewBounds()
    {
        int halfRange = Math.Max(45, (int)_nudSoftLock.Value / 2);
        int current = _sldAnglePreview.Value;
        _sldAnglePreview.Minimum = -halfRange;
        _sldAnglePreview.Maximum = halfRange;
        _sldAnglePreview.Value = Math.Clamp(current, -halfRange, halfRange);
    }

    private void LoadSettingsIntoUi(WheelSettings settings)
    {
        _updatingControls = true;
        _sldSoftLock.Value = Math.Clamp(settings.SteeringRange, 90, 1440);
        _nudSoftLock.Value = Math.Clamp(settings.SteeringRange, 90, 1440);
        _sldOverallForce.Value = Math.Clamp(settings.OverallForce, 0, 100);
        _nudOverallForce.Value = Math.Clamp(settings.OverallForce, 0, 100);
        _sldDamper.Value = Math.Clamp(settings.Damping, 0, 100);
        _nudDamper.Value = Math.Clamp(settings.Damping, 0, 100);
        _sldFriction.Value = Math.Clamp(settings.Friction, 0, 100);
        _nudFriction.Value = Math.Clamp(settings.Friction, 0, 100);
        _sldSpring.Value = Math.Clamp(settings.Spring, 0, 100);
        _nudSpring.Value = Math.Clamp(settings.Spring, 0, 100);
        _sldMinForce.Value = Math.Clamp(settings.MinimumForce, 0, 20);
        _nudMinForce.Value = Math.Clamp(settings.MinimumForce, 0, 20);
        _chkInvertEncoder.Checked = settings.InvertEncoder;
        _chkInvertMotor.Checked = settings.InvertMotor;
        _txtCpr.Text = settings.EncoderCpr.ToString(CultureInfo.InvariantCulture);
        _cboHBridge.SelectedItem = settings.HBridgeMode;
        if (_cboHBridge.SelectedItem == null)
            _cboHBridge.Text = settings.HBridgeMode;
        _updatingControls = false;

        SyncAnglePreviewBounds();

        _lblFirmware.Text = string.IsNullOrWhiteSpace(settings.FirmwareVersion)
            ? "Firmware ver : Unknown"
            : $"Firmware ver : {settings.FirmwareVersion}";
        _lblProduct.Text = string.IsNullOrWhiteSpace(settings.ProductName)
            ? string.Empty
            : $"{settings.ProductName} | {settings.UsbMode}";

        SetDirty(false);
        SetStatusLine("Ready");
    }

    private WheelSettings GatherSettings()
    {
        int encoderCpr = 2400;
        _ = int.TryParse(_txtCpr.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out encoderCpr);
        if (encoderCpr <= 0)
            encoderCpr = 2400;

        return new WheelSettings
        {
            SteeringRange = (int)_nudSoftLock.Value,
            OverallForce = (int)_nudOverallForce.Value,
            MinimumForce = (int)_nudMinForce.Value,
            Damping = (int)_nudDamper.Value,
            Friction = (int)_nudFriction.Value,
            Spring = (int)_nudSpring.Value,
            EncoderCpr = encoderCpr,
            InvertEncoder = _chkInvertEncoder.Checked,
            InvertMotor = _chkInvertMotor.Checked,
            HBridgeMode = _cboHBridge.Text
        };
    }

    private void SetConnected(bool connected)
    {
        _btnConnect.Enabled = !connected;
        _btnDisconnect.Enabled = connected;
        _btnCenter.Enabled = connected;
        _btnRead.Enabled = connected;
        _btnApply.Enabled = connected;
        _btnSave.Enabled = connected;
        _btnReset.Enabled = connected;
        _btnSettingsApply.Enabled = connected;

        _lblConnectionStatus.Text = connected ? "Status: Connected" : "Status: Disconnected";

        if (!connected)
        {
            _lblAngle.Text = "0°";
            _lblRawCounts.Text = "Raw Counts: 0";
            _sldAnglePreview.Value = 0;
            _lblFirmware.Text = "Firmware ver : Unknown";
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
