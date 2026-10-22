using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdTimeDialMode
{
    Hour,
    Minute,
}

/// <summary>
/// Material Design 3 256dp analog clock dial with 12h / 24h double-ring support,
/// selector arm, and automatic hour-to-minute transition on pointer release.
/// </summary>
public class MdTimeDial : Control
{
    public static readonly StyledProperty<int> HourProperty =
        AvaloniaProperty.Register<MdTimeDial, int>(nameof(Hour), 9, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MinuteProperty =
        AvaloniaProperty.Register<MdTimeDial, int>(nameof(Minute), 30, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> Is24HourProperty =
        AvaloniaProperty.Register<MdTimeDial, bool>(nameof(Is24Hour));

    public static readonly StyledProperty<MdTimeDialMode> ModeProperty =
        AvaloniaProperty.Register<MdTimeDial, MdTimeDialMode>(nameof(Mode), MdTimeDialMode.Hour);

    private int _hour = 9;
    private int _minute = 30;
    private bool _is24Hour;
    private MdTimeDialMode _mode = MdTimeDialMode.Hour;
    private double _displayAngleDeg;
    private bool _innerRingSelected;
    private bool _dragging;

    public event EventHandler<(int Hour, int Minute)>? TimeChanged;
    public event EventHandler? HourSelectionCompleted;

    static MdTimeDial()
    {
        HourProperty.Changed.AddClassHandler<MdTimeDial>((d, e) =>
        {
            var clamped = Math.Clamp(e.GetNewValue<int>(), 0, 23);
            if (d._hour == clamped) return;
            d._hour = clamped;
            d.SyncHandFromTime(animate: !d._dragging);
            d.TimeChanged?.Invoke(d, (d._hour, d._minute));
        });
        MinuteProperty.Changed.AddClassHandler<MdTimeDial>((d, e) =>
        {
            var clamped = Math.Clamp(e.GetNewValue<int>(), 0, 59);
            if (d._minute == clamped) return;
            d._minute = clamped;
            d.SyncHandFromTime(animate: !d._dragging);
            d.TimeChanged?.Invoke(d, (d._hour, d._minute));
        });
        Is24HourProperty.Changed.AddClassHandler<MdTimeDial>((d, e) =>
        {
            var val = e.GetNewValue<bool>();
            if (d._is24Hour == val) return;
            d._is24Hour = val;
            d.SyncHandFromTime(animate: false);
            d.InvalidateVisual();
        });
        ModeProperty.Changed.AddClassHandler<MdTimeDial>((d, e) =>
        {
            var val = e.GetNewValue<MdTimeDialMode>();
            if (d._mode == val) return;
            d._mode = val;
            d.SyncHandFromTime(animate: true);
        });
    }

    public MdTimeDial() : this(9, 30, false)
    {
    }

    public MdTimeDial(int hour = 9, int minute = 30, bool is24Hour = false)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Cursor = new Cursor(StandardCursorType.Hand);

        _hour = Math.Clamp(hour, 0, 23);
        _minute = Math.Clamp(minute, 0, 59);
        _is24Hour = is24Hour;
        Hour = _hour;
        Minute = _minute;
        Is24Hour = _is24Hour;
        SyncHandFromTime(animate: false);

        MdTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public int Hour
    {
        get => _hour;
        set => SetValue(HourProperty, Math.Clamp(value, 0, 23));
    }

    public int Minute
    {
        get => _minute;
        set => SetValue(MinuteProperty, Math.Clamp(value, 0, 59));
    }

    public bool Is24Hour
    {
        get => _is24Hour;
        set => SetValue(Is24HourProperty, value);
    }

    public MdTimeDialMode Mode
    {
        get => _mode;
        set => SetValue(ModeProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = 256.0;
        if (double.IsFinite(availableSize.Width))
            side = Math.Min(side, Math.Max(0, availableSize.Width));
        if (double.IsFinite(availableSize.Height))
            side = Math.Min(side, Math.Max(0, availableSize.Height));
        return new Size(side, side);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (MdGestureZones.ShouldDeferToSystem(this, e)) return;
        _dragging = true;
        e.Pointer.Capture(this);
        e.PreventGestureRecognition();
        UpdateFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging) return;
        e.PreventGestureRecognition();
        UpdateFromPoint(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);
        UpdateFromPoint(e.GetPosition(this));

        if (_mode == MdTimeDialMode.Hour)
        {
            HourSelectionCompleted?.Invoke(this, EventArgs.Empty);
        }
        e.Handled = true;
    }

    private void UpdateFromPoint(Point pt)
    {
        var cx = Bounds.Width / 2.0;
        var cy = Bounds.Height / 2.0;
        var dx = pt.X - cx;
        var dy = pt.Y - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 6) return;

        var deg = Math.Atan2(dy, dx) * 180.0 / Math.PI + 90.0;
        if (deg < 0) deg += 360.0;

        if (_mode == MdTimeDialMode.Hour)
        {
            var step = (int)Math.Round(deg / 30.0) % 12;
            if (_is24Hour)
            {
                var dialR = Math.Min(cx, cy);
                var inner = dist < dialR * 0.64;
                _innerRingSelected = inner;
                if (inner)
                {
                    _hour = step == 0 ? 0 : step + 12;
                }
                else
                {
                    _hour = step == 0 ? 12 : step;
                }
            }
            else
            {
                var isPm = _hour >= 12;
                var h12 = step;
                _hour = isPm ? (h12 == 0 ? 12 : h12 + 12) : (h12 == 12 ? 0 : h12);
                _innerRingSelected = false;
            }
            _displayAngleDeg = step * 30.0;
        }
        else
        {
            var min = (int)Math.Round(deg / 6.0) % 60;
            _minute = min;
            _displayAngleDeg = min * 6.0;
            _innerRingSelected = false;
        }

        SetValue(HourProperty, _hour);
        SetValue(MinuteProperty, _minute);
        InvalidateVisual();
        TimeChanged?.Invoke(this, (_hour, _minute));
    }

    private void SyncHandFromTime(bool animate)
    {
        double targetDeg;
        if (_mode == MdTimeDialMode.Hour)
        {
            var h12 = _hour % 12;
            targetDeg = h12 * 30.0;
            _innerRingSelected = _is24Hour && (_hour == 0 || _hour > 12);
        }
        else
        {
            targetDeg = _minute * 6.0;
            _innerRingSelected = false;
        }

        if (animate)
        {
            var diff = targetDeg - _displayAngleDeg;
            while (diff > 180) diff -= 360;
            while (diff < -180) diff += 360;
            var start = _displayAngleDeg;
            MdMotion.Animate(this, "dial_hand", 0, 1, MdMotion.Short4, MdMotion.CubicOut, v =>
            {
                var a = (start + diff * v) % 360.0;
                if (a < 0) a += 360.0;
                _displayAngleDeg = a;
                InvalidateVisual();
            });
        }
        else
        {
            _displayAngleDeg = targetDeg;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var t = MdTheme.Current;
        var c = t.Color;
        var cx = w / 2.0;
        var cy = h / 2.0;
        var dialR = Math.Min(cx, cy) - 2.0;

        context.DrawEllipse(MdTheme.BrushOf(c.SurfaceContainerHighest), null, new Point(cx, cy), dialR, dialR);

        var outerOrbit = dialR - 28.0;
        var innerOrbit = dialR - 64.0;
        var activeOrbit = (_mode == MdTimeDialMode.Hour && _innerRingSelected) ? innerOrbit : outerOrbit;

        var handRad = (_displayAngleDeg - 90.0) * Math.PI / 180.0;
        var tipX = cx + activeOrbit * Math.Cos(handRad);
        var tipY = cy + activeOrbit * Math.Sin(handRad);

        var primaryBrush = MdTheme.BrushOf(c.Primary);
        context.DrawLine(MdTheme.PenOf(c.Primary, 2), new Point(cx, cy), new Point(tipX, tipY));
        context.DrawEllipse(primaryBrush, null, new Point(cx, cy), 4, 4);
        context.DrawEllipse(primaryBrush, null, new Point(tipX, tipY), 20, 20);

        if (_mode == MdTimeDialMode.Minute && _minute % 5 != 0)
        {
            context.DrawEllipse(MdTheme.BrushOf(c.OnPrimary), null, new Point(tipX, tipY), 3, 3);
        }

        if (_mode == MdTimeDialMode.Hour)
        {
            for (int i = 1; i <= 12; i++)
            {
                var step = i % 12;
                var deg = step * 30.0;
                var rad = (deg - 90.0) * Math.PI / 180.0;
                var x = cx + outerOrbit * Math.Cos(rad);
                var y = cy + outerOrbit * Math.Sin(rad);
                var selected = !_innerRingSelected && Math.Abs(AngleDelta(deg, _displayAngleDeg)) < 10.0;
                DrawDialNumber(context, t, i.ToString(CultureInfo.InvariantCulture), x, y, 15, selected);
            }

            if (_is24Hour)
            {
                for (int i = 0; i < 12; i++)
                {
                    var val = i == 0 ? 0 : i + 12;
                    var deg = i * 30.0;
                    var rad = (deg - 90.0) * Math.PI / 180.0;
                    var x = cx + innerOrbit * Math.Cos(rad);
                    var y = cy + innerOrbit * Math.Sin(rad);
                    var selected = _innerRingSelected && Math.Abs(AngleDelta(deg, _displayAngleDeg)) < 10.0;
                    DrawDialNumber(context, t, val.ToString("00", CultureInfo.InvariantCulture), x, y, 12, selected);
                }
            }
        }
        else
        {
            for (int i = 0; i < 12; i++)
            {
                var minVal = i * 5;
                var deg = minVal * 6.0;
                var rad = (deg - 90.0) * Math.PI / 180.0;
                var x = cx + outerOrbit * Math.Cos(rad);
                var y = cy + outerOrbit * Math.Sin(rad);
                var selected = _minute == minVal;
                DrawDialNumber(context, t, minVal.ToString("00", CultureInfo.InvariantCulture), x, y, 14, selected);
            }
        }
    }

    private static double AngleDelta(double a, double b)
    {
        var d = (a - b) % 360.0;
        if (d > 180) d -= 360;
        if (d < -180) d += 360;
        return d;
    }

    private static void DrawDialNumber(
        DrawingContext context,
        MdTheme t,
        string text,
        double x,
        double y,
        double fontSize,
        bool selected)
    {
        var fg = selected ? t.Color.OnPrimary : t.Color.OnSurface;
        var ft = MdTextMetrics.GetFormattedText(
            text,
            fontSize,
            selected ? MdFonts.RobotoSemiBoldTypeface : MdFonts.RobotoRegularTypeface,
            fg);
        context.DrawText(ft, new Point(x - ft.Width / 2.0, y - ft.Height / 2.0));
    }
}
