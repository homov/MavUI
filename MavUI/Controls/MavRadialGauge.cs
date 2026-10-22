using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Platform;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Interactive circular 270° Material Design 3 radial gauge with draggable arc/thumb,
/// animated value sweep, optional tick marks, and center readout.
/// </summary>
public class MavRadialGauge : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavRadialGauge, double>(nameof(Value), 65.0);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MavRadialGauge, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MavRadialGauge, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MavRadialGauge, bool>(nameof(IsInteractive), true);

    public static readonly StyledProperty<double> StepSizeProperty =
        AvaloniaProperty.Register<MavRadialGauge, double>(nameof(StepSize), 0.0);

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<MavRadialGauge, string?>(nameof(Unit), "%");

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MavRadialGauge, string?>(nameof(Label));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<MavRadialGauge, double>(nameof(Size), 176.0);

    public static readonly StyledProperty<int> TickCountProperty =
        AvaloniaProperty.Register<MavRadialGauge, int>(nameof(TickCount), 0);

    private double _displayValue = 65.0;
    private bool _dragging;
    private readonly MavArcCache _trackArc = new();
    private readonly MavArcCache _activeArc = new();

    public event EventHandler<double>? ValueChanged;

    static MavRadialGauge()
    {
        ValueProperty.Changed.AddClassHandler<MavRadialGauge>((g, e) =>
        {
            var next = e.GetNewValue<double>();
            if (g._dragging)
            {
                MavMotion.Cancel(g, "gauge_val");
                g._displayValue = next;
                g.InvalidateVisual();
            }
            else
            {
                g.AnimateTo(next);
            }
            g.ValueChanged?.Invoke(g, next);
        });
        MinimumProperty.Changed.AddClassHandler<MavRadialGauge>((g, _) => g.InvalidateVisual());
        MaximumProperty.Changed.AddClassHandler<MavRadialGauge>((g, _) => g.InvalidateVisual());
        UnitProperty.Changed.AddClassHandler<MavRadialGauge>((g, _) => g.InvalidateVisual());
        LabelProperty.Changed.AddClassHandler<MavRadialGauge>((g, _) => g.InvalidateVisual());
        SizeProperty.Changed.AddClassHandler<MavRadialGauge>((g, e) =>
        {
            var sz = e.GetNewValue<double>();
            g.Width = sz;
            g.Height = sz;
            g.InvalidateVisual();
        });
        TickCountProperty.Changed.AddClassHandler<MavRadialGauge>((g, _) => g.InvalidateVisual());
    }

    /// <summary>Creates a radial gauge with a value of 65 on a scale from 0 to 100.</summary>
    public MavRadialGauge() : this(65, 0, 100, "%", null, 176)
    {
    }

    /// <summary>Creates a radial gauge with the given range, value, unit and size.</summary>
    public MavRadialGauge(
        double value = 65,
        double minimum = 0,
        double maximum = 100,
        string? unit = "%",
        string? label = null,
        double size = 176)
    {
        Size = size;
        Width = size;
        Height = size;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Cursor = new Cursor(StandardCursorType.Hand);

        Minimum = minimum;
        Maximum = maximum;
        Unit = unit;
        Label = label;
        _displayValue = value;
        Value = value;

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    public double StepSize
    {
        get => GetValue(StepSizeProperty);
        set => SetValue(StepSizeProperty, Math.Max(0, value));
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public int TickCount
    {
        get => GetValue(TickCountProperty);
        set => SetValue(TickCountProperty, Math.Max(0, value));
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    /// <summary>Starts dragging the gauge handle to change the value.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled || !IsInteractive) return;
        if (MavGestureZones.ShouldDeferToSystem(this, e)) return;

        var pos = e.GetPosition(this);
        if (TryUpdateFromPoint(pos, isInitialPress: true))
        {
            _dragging = true;
            e.Pointer.Capture(this);
            e.PreventGestureRecognition();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    /// <summary>Updates the value while the handle is dragged.</summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || !IsEnabled || !IsInteractive) return;

        e.PreventGestureRecognition();
        TryUpdateFromPoint(e.GetPosition(this), isInitialPress: false);
        e.Handled = true;
    }

    /// <summary>Ends the drag and keeps the value at the handle position.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;

        e.PreventGestureRecognition();
        TryUpdateFromPoint(e.GetPosition(this), isInitialPress: false);
        _dragging = false;
        if (ReferenceEquals(e.Pointer.Captured, this))
            e.Pointer.Capture(null);
        InvalidateVisual();
        e.Handled = true;
    }

    /// <summary>Ends the drag when the pointer capture is lost.</summary>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_dragging)
        {
            _dragging = false;
            InvalidateVisual();
        }
    }

    private bool TryUpdateFromPoint(Point pt, bool isInitialPress)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0 || Maximum <= Minimum) return false;

        var cx = w / 2.0;
        var cy = h / 2.0;
        var dx = pt.X - cx;
        var dy = pt.Y - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 8.0) return false;

        var deg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        if (deg < 0) deg += 360.0;

        // Arc starts at 135° (bottom-left) and sweeps 270° clockwise to 45° (bottom-right).
        var rel = deg - 135.0;
        if (rel < 0) rel += 360.0;

        double fraction;
        if (rel <= 270.0)
        {
            fraction = rel / 270.0;
        }
        else
        {
            if (isInitialPress && rel > 285.0 && rel < 345.0)
                return false;

            fraction = rel < 315.0 ? 1.0 : 0.0;
        }

        var raw = Minimum + fraction * (Maximum - Minimum);
        if (StepSize > 0)
        {
            var steps = Math.Round((raw - Minimum) / StepSize, MidpointRounding.AwayFromZero);
            raw = Minimum + steps * StepSize;
        }
        var clamped = Math.Clamp(raw, Minimum, Maximum);

        MavMotion.Cancel(this, "gauge_val");
        _displayValue = clamped;
        SetValue(ValueProperty, clamped);
        InvalidateVisual();
        return true;
    }

    private void AnimateTo(double target)
    {
        MavMotion.Animate(this, "gauge_val", _displayValue, target, MavMotion.Medium2, MavMotion.CubicOut, v =>
        {
            _displayValue = v;
            InvalidateVisual();
        });
    }

    /// <summary>Draws the gauge arc, the value arc and the centered value text.</summary>
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        // Ensure entire gauge bounds are hit-testable for touch/mouse dragging.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var t = MavTheme.Current;
        var c = t.Color;
        var center = new Point(w / 2.0, h / 2.0);
        var strokeW = Math.Max(10.0, Math.Min(w, h) * 0.085);
        var r = Math.Min(w, h) / 2.0 - strokeW - 6.0;

        const double startDeg = 135.0;
        const double totalSweep = 270.0;

        var trackPen = MavTheme.PenOf(c.SecondaryContainer, strokeW, PenLineCap.Round);
        context.DrawGeometry(null, trackPen, _trackArc.Get(center, r, startDeg, totalSweep));

        var range = Math.Max(1e-6, Maximum - Minimum);
        var norm = Math.Clamp((_displayValue - Minimum) / range, 0, 1);
        var activeSweep = totalSweep * norm;

        if (activeSweep > 0.5)
        {
            var activePen = MavTheme.PenOf(c.Primary, strokeW, PenLineCap.Round);
            context.DrawGeometry(null, activePen, _activeArc.Get(center, r, startDeg, activeSweep));
        }

        if (TickCount > 1)
        {
            var tickInner = r - strokeW * 0.95;
            var tickOuter = r - strokeW * 0.58;
            var tickPen = MavTheme.PenOf(c.OnSurfaceVariant.WithAlpha(0.55f), 1.5, PenLineCap.Round);
            for (int i = 0; i < TickCount; i++)
            {
                var tf = (double)i / (TickCount - 1);
                var deg = startDeg + totalSweep * tf;
                var rad = deg * Math.PI / 180.0;
                var p1 = new Point(center.X + tickInner * Math.Cos(rad), center.Y + tickInner * Math.Sin(rad));
                var p2 = new Point(center.X + tickOuter * Math.Cos(rad), center.Y + tickOuter * Math.Sin(rad));
                context.DrawLine(tickPen, p1, p2);
            }
        }

        // Draw thumb indicator on the arc when interactive
        if (IsInteractive)
        {
            var thumbDeg = startDeg + activeSweep;
            var thumbRad = thumbDeg * Math.PI / 180.0;
            var thumbPos = new Point(center.X + r * Math.Cos(thumbRad), center.Y + r * Math.Sin(thumbRad));
            var thumbR = _dragging ? strokeW * 0.78 : strokeW * 0.65;

            if (_dragging)
            {
                context.DrawEllipse(MavTheme.BrushOf(c.Primary.WithAlpha(0.18f)), null, thumbPos, thumbR * 1.65, thumbR * 1.65);
            }
            context.DrawEllipse(MavTheme.BrushOf(c.Primary), MavTheme.PenOf(c.Surface, 2.5), thumbPos, thumbR, thumbR);
        }

        var valStr = $"{Math.Round(_displayValue)}{Unit}";
        var valFt = MavTextMetrics.GetFormattedText(
            valStr,
            Math.Round(Math.Min(w, h) * 0.18),
            MavFonts.RobotoSemiBoldTypeface,
            c.OnSurface);

        var hasLabel = !string.IsNullOrEmpty(Label);
        var valY = hasLabel ? center.Y - valFt.Height * 0.65 : center.Y - valFt.Height / 2.0;
        context.DrawText(valFt, new Point(center.X - valFt.Width / 2.0, valY));

        if (hasLabel)
        {
            var lblFt = MavTextMetrics.GetFormattedText(
                Label!,
                Math.Round(Math.Min(w, h) * 0.085),
                MavFonts.RobotoMediumTypeface,
                c.OnSurfaceVariant);
            context.DrawText(lblFt, new Point(center.X - lblFt.Width / 2.0, valY + valFt.Height + 2));
        }
    }
}
