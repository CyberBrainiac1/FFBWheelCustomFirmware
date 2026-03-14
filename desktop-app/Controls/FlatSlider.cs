using System.ComponentModel;

namespace FFBWheelConfig.Controls;

public sealed class FlatSlider : Control
{
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private bool _dragging;

    public event EventHandler? ValueChanged;

    [DefaultValue(0)]
    public int Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum == value)
                return;

            _minimum = value;
            if (_maximum < _minimum)
                _maximum = _minimum;

            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    [DefaultValue(100)]
    public int Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum == value)
                return;

            _maximum = Math.Max(value, _minimum);
            Value = Math.Clamp(_value, _minimum, _maximum);
            Invalidate();
        }
    }

    [DefaultValue(0)]
    public int Value
    {
        get => _value;
        set => SetValue(value, true);
    }

    public Color TrackColor { get; set; } = Color.FromArgb(0x1E, 0x20, 0x23);
    public Color FillColor { get; set; } = Color.FromArgb(0x3E, 0x89, 0xB8);
    public Color ThumbColor { get; set; } = Color.FromArgb(0xC9, 0xD2, 0xDB);
    public Color BorderColor { get; set; } = Color.FromArgb(0x4A, 0x4E, 0x55);

    public FlatSlider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.UserPaint, true);

        TabStop = true;
        Size = new Size(220, 24);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(Parent?.BackColor ?? Color.Transparent);

        Rectangle trackRect = GetTrackRectangle();
        Rectangle fillRect = trackRect;
        fillRect.Width = GetFillWidth(trackRect);

        Color trackColor = Enabled ? TrackColor : ControlPaint.Dark(TrackColor, 0.12f);
        Color fillColor = Enabled ? FillColor : ControlPaint.Dark(FillColor, 0.25f);
        Color thumbColor = Enabled ? ThumbColor : ControlPaint.Dark(ThumbColor, 0.20f);
        Color borderColor = Enabled ? BorderColor : ControlPaint.Dark(BorderColor, 0.20f);

        using var trackBrush = new SolidBrush(trackColor);
        using var fillBrush = new SolidBrush(fillColor);
        using var thumbBrush = new SolidBrush(thumbColor);
        using var borderPen = new Pen(borderColor);

        e.Graphics.FillRectangle(trackBrush, trackRect);
        if (fillRect.Width > 0)
            e.Graphics.FillRectangle(fillBrush, fillRect);
        e.Graphics.DrawRectangle(borderPen, trackRect);

        Rectangle thumbRect = GetThumbRectangle(trackRect);
        e.Graphics.FillRectangle(thumbBrush, thumbRect);
        e.Graphics.DrawRectangle(borderPen, thumbRect);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!Enabled || e.Button != MouseButtons.Left)
            return;

        Focus();
        _dragging = true;
        Capture = true;
        SetValueFromPoint(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!Enabled || !_dragging)
            return;

        SetValueFromPoint(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button != MouseButtons.Left)
            return;

        _dragging = false;
        Capture = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!Enabled)
            return;

        int step = Math.Max(1, (_maximum - _minimum) / 100);
        Value += e.Delta > 0 ? step : -step;
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown
            || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!Enabled)
            return;

        int largeStep = Math.Max(1, (_maximum - _minimum) / 10);
        switch (e.KeyCode)
        {
            case Keys.Left:
                Value--;
                e.Handled = true;
                break;
            case Keys.Right:
                Value++;
                e.Handled = true;
                break;
            case Keys.PageDown:
                Value -= largeStep;
                e.Handled = true;
                break;
            case Keys.PageUp:
                Value += largeStep;
                e.Handled = true;
                break;
            case Keys.Home:
                Value = _minimum;
                e.Handled = true;
                break;
            case Keys.End:
                Value = _maximum;
                e.Handled = true;
                break;
        }
    }

    private Rectangle GetTrackRectangle()
    {
        const int trackHeight = 8;
        return new Rectangle(1, (Height - trackHeight) / 2, Math.Max(4, Width - 2), trackHeight);
    }

    private Rectangle GetThumbRectangle(Rectangle trackRect)
    {
        const int thumbWidth = 10;
        const int thumbHeight = 16;

        int usableWidth = Math.Max(1, trackRect.Width - thumbWidth);
        int thumbX = trackRect.X + (int)Math.Round(GetNormalizedValue() * usableWidth);
        int thumbY = (Height - thumbHeight) / 2;

        return new Rectangle(thumbX, thumbY, thumbWidth, thumbHeight);
    }

    private int GetFillWidth(Rectangle trackRect)
    {
        if (_maximum <= _minimum)
            return 0;

        return Math.Clamp((int)Math.Round(GetNormalizedValue() * trackRect.Width), 0, trackRect.Width);
    }

    private double GetNormalizedValue()
    {
        if (_maximum <= _minimum)
            return 0;

        return (double)(_value - _minimum) / (_maximum - _minimum);
    }

    private void SetValueFromPoint(int x)
    {
        Rectangle trackRect = GetTrackRectangle();
        const int thumbWidth = 10;

        int usableWidth = Math.Max(1, trackRect.Width - thumbWidth);
        int relativeX = Math.Clamp(x - trackRect.X - (thumbWidth / 2), 0, usableWidth);
        int newValue = _minimum + (int)Math.Round((double)relativeX * (_maximum - _minimum) / usableWidth);
        SetValue(newValue, true);
    }

    private void SetValue(int value, bool raiseEvent)
    {
        int clamped = Math.Clamp(value, _minimum, _maximum);
        if (_value == clamped)
            return;

        _value = clamped;
        Invalidate();

        if (raiseEvent)
            ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}
