using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 single-thumb slider with 16dp track, vertical bar thumb, gap cutouts,
/// discrete tick marks, centered zero-origin mode, and value indicator bubble.
/// </summary>
public class MavSlider : Control, IMavDraggableControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MavSlider, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MavSlider, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavSlider, double>(nameof(Value), 50.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> DivisionsProperty =
        AvaloniaProperty.Register<MavSlider, int>(nameof(Divisions), 0);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MavSlider, double>(nameof(Step), 0.0);

    public static readonly StyledProperty<bool> ShowValueIndicatorProperty =
        AvaloniaProperty.Register<MavSlider, bool>(nameof(ShowValueIndicator), true);

    public static readonly StyledProperty<bool> CenteredProperty =
        AvaloniaProperty.Register<MavSlider, bool>(nameof(Centered));

    private bool _dragging;
    private double _bubbleScale;

    public event EventHandler<double>? ValueChanged;

    static MavSlider()
    {
        MinimumProperty.Changed.AddClassHandler<MavSlider>((s, _) => s.InvalidateVisual());
        MaximumProperty.Changed.AddClassHandler<MavSlider>((s, _) => s.InvalidateVisual());
        ValueProperty.Changed.AddClassHandler<MavSlider>((s, e) =>
        {
            s.InvalidateVisual();
            s.ValueChanged?.Invoke(s, e.GetNewValue<double>());
        });
        DivisionsProperty.Changed.AddClassHandler<MavSlider>((s, _) => s.InvalidateVisual());
        CenteredProperty.Changed.AddClassHandler<MavSlider>((s, _) => s.InvalidateVisual());
    }

    /// <summary>Creates a slider from 0 to 100 at 50.</summary>
    public MavSlider() : this(0, 100, 50, 0)
    {
    }

    /// <summary>Creates a slider with the given range, value and number of discrete steps.</summary>
    public MavSlider(double min = 0, double max = 100, double value = 50, int divisions = 0)
    {
        Height = 48;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;
        ClipToBounds = false;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);

        Minimum = min;
        Maximum = max;
        Divisions = divisions;
        Value = Math.Clamp(value, min, max);
        MavAutomation.SetControlType(this, AutomationControlType.Slider);

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
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

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Snap(value));
    }

    public int Divisions
    {
        get => GetValue(DivisionsProperty);
        set => SetValue(DivisionsProperty, Math.Max(0, value));
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, Math.Max(0, value));
    }

    public bool ShowValueIndicator
    {
        get => GetValue(ShowValueIndicatorProperty);
        set => SetValue(ShowValueIndicatorProperty, value);
    }

    public bool ShowLabel
    {
        get => ShowValueIndicator;
        set => ShowValueIndicator = value;
    }

    public bool ShowValueLabel
    {
        get => ShowValueIndicator;
        set => ShowValueIndicator = value;
    }

    public bool Centered
    {
        get => GetValue(CenteredProperty);
        set => SetValue(CenteredProperty, value);
    }

    public Func<double, string>? ValueFormatter { get; set; }

    public Func<double, string>? LabelFormatter
    {
        get => ValueFormatter;
        set => ValueFormatter = value;
    }

    /// <summary>Measures the track and the thumb to fill the available width.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 240;
        return new Size(w, 48);
    }

    /// <summary>Starts dragging the thumb, or jumps the thumb to the pointer position.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        if (MavGestureZones.ShouldDeferToSystem(this, e)) return;
        _dragging = true;
        e.Pointer.Capture(this);
        e.PreventGestureRecognition();
        AnimateBubble(1);
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    /// <summary>Updates the value while the thumb is dragged.</summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || !IsEnabled) return;
        e.PreventGestureRecognition();
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    /// <summary>Ends the drag and commits the value.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);
        AnimateBubble(0);
        e.Handled = true;
    }

    /// <summary>Ends the drag when the pointer capture is lost.</summary>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_dragging)
        {
            _dragging = false;
            AnimateBubble(0);
        }
    }

    /// <summary>Changes the value with the arrow, Home and End keys.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEnabled) return;
        var range = Math.Max(1e-6, Maximum - Minimum);
        var step = Step > 0 ? Step : (Divisions > 0 ? range / Divisions : range * 0.05);
        if (e.Key is Key.Left or Key.Down)
        {
            Value = Math.Max(Minimum, Value - step);
            e.Handled = true;
        }
        else if (e.Key is Key.Right or Key.Up)
        {
            Value = Math.Min(Maximum, Value + step);
            e.Handled = true;
        }
    }

    private void AnimateBubble(double target)
    {
        MavMotion.Animate(this, "sl_bubble", _bubbleScale, target, MavMotion.Short3, MavMotion.CubicOut, v =>
        {
            _bubbleScale = v;
            InvalidateVisual();
        });
    }

    private void UpdateFromX(double x)
    {
        const double pad = 12.0;
        var trackW = Math.Max(1.0, Bounds.Width - pad * 2);
        var norm = Math.Clamp((x - pad) / trackW, 0.0, 1.0);
        var raw = Minimum + norm * (Maximum - Minimum);
        Value = Snap(raw);
    }

    private double Snap(double v)
    {
        var clamped = Math.Clamp(v, Minimum, Maximum);
        var range = Maximum - Minimum;
        if (range <= 0) return Minimum;

        if (Step > 0)
        {
            var steps = Math.Round((clamped - Minimum) / Step);
            return Math.Clamp(Minimum + steps * Step, Minimum, Maximum);
        }

        if (Divisions > 0)
        {
            var step = range / Divisions;
            var steps = Math.Round((clamped - Minimum) / step);
            return Math.Clamp(Minimum + steps * step, Minimum, Maximum);
        }

        return clamped;
    }

    /// <summary>Draws the active and inactive track segments, the tick marks and the thumb.</summary>
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var t = MavTheme.Current;
        var c = t.Color;
        const double pad = 12.0;
        const double trackH = 16.0;
        const double gap = 6.0;
        var cy = h / 2.0 + 4.0;
        var trackLeft = pad;
        var trackRight = Math.Max(pad + 1, w - pad);
        var trackW = trackRight - trackLeft;

        var range = Math.Max(1e-6, Maximum - Minimum);
        var norm = Math.Clamp((Value - Minimum) / range, 0, 1);
        var thumbX = trackLeft + trackW * norm;

        var activeColor = IsEnabled ? c.Primary : c.OnSurface.WithAlpha(0.38f);
        var inactiveColor = IsEnabled ? c.SecondaryContainer : c.OnSurface.WithAlpha(0.12f);
        var activeBrush = MavTheme.BrushOf(activeColor);
        var inactiveBrush = MavTheme.BrushOf(inactiveColor);

        if (Centered)
        {
            var centerX = trackLeft + trackW * 0.5;
            var leftActive = Math.Min(centerX, thumbX - gap);
            var rightActive = Math.Max(centerX, thumbX + gap);

            if (thumbX < centerX - gap)
            {
                DrawTrackSegment(context, inactiveBrush, trackLeft, thumbX - gap, cy, trackH, 8, 2);
                DrawTrackSegment(context, activeBrush, thumbX + gap, centerX, cy, trackH, 2, 2);
                DrawTrackSegment(context, inactiveBrush, centerX, trackRight, cy, trackH, 2, 8);
            }
            else if (thumbX > centerX + gap)
            {
                DrawTrackSegment(context, inactiveBrush, trackLeft, centerX, cy, trackH, 8, 2);
                DrawTrackSegment(context, activeBrush, centerX, thumbX - gap, cy, trackH, 2, 2);
                DrawTrackSegment(context, inactiveBrush, thumbX + gap, trackRight, cy, trackH, 2, 8);
            }
            else
            {
                DrawTrackSegment(context, inactiveBrush, trackLeft, leftActive, cy, trackH, 8, 2);
                DrawTrackSegment(context, inactiveBrush, rightActive, trackRight, cy, trackH, 2, 8);
            }
        }
        else
        {
            var activeRight = Math.Max(trackLeft, thumbX - gap);
            if (activeRight > trackLeft + 1)
                DrawTrackSegment(context, activeBrush, trackLeft, activeRight, cy, trackH, 8, 2);

            var inactiveLeft = Math.Min(trackRight, thumbX + gap);
            if (trackRight > inactiveLeft + 1)
                DrawTrackSegment(context, inactiveBrush, inactiveLeft, trackRight, cy, trackH, 2, 8);

            if (norm < 0.99)
                context.DrawEllipse(activeBrush, null, new Point(trackRight - 4, cy), 2, 2);
        }

        if (Divisions > 0)
        {
            for (int i = 0; i <= Divisions; i++)
            {
                var tf = (double)i / Divisions;
                var tx = trackLeft + trackW * tf;
                if (Math.Abs(tx - thumbX) < gap + 3) continue;
                var onActive = Centered
                    ? (tx >= Math.Min(trackLeft + trackW * 0.5, thumbX) && tx <= Math.Max(trackLeft + trackW * 0.5, thumbX))
                    : tx <= thumbX;
                var dotCol = onActive ? c.OnPrimary : c.OnSecondaryContainer;
                context.DrawEllipse(MavTheme.BrushOf(dotCol.WithAlpha(0.75f)), null, new Point(tx, cy), 2, 2);
            }
        }

        var thumbW = _dragging ? 2.5 : 4.0;
        const double thumbH = 44.0;
        var thumbRect = new RoundedRect(new Rect(thumbX - thumbW / 2.0, cy - thumbH / 2.0, thumbW, thumbH), thumbW / 2.0);
        context.DrawRectangle(activeBrush, null, thumbRect);

        if (ShowValueIndicator && _bubbleScale > 0.02)
        {
            var text = (ValueFormatter ?? (v => Math.Round(v).ToString(CultureInfo.InvariantCulture)))(Value);
            var ft = MavTextMetrics.GetFormattedText(text, 12, MavFonts.RobotoMediumTypeface, c.OnPrimary);

            var bw = Math.Max(28.0, ft.Width + 16.0) * _bubbleScale;
            var bh = 28.0 * _bubbleScale;
            var by = cy - thumbH / 2.0 - bh - 4.0;
            var bx = Math.Clamp(thumbX - bw / 2.0, 2, Math.Max(2, w - bw - 2));
            var bRect = new RoundedRect(new Rect(bx, by, bw, bh), bh / 2.0);
            using (context.PushOpacity(_bubbleScale))
                context.DrawRectangle(MavTheme.BrushOf(c.Primary), null, bRect);
            using (context.PushOpacity(_bubbleScale))
                context.DrawText(ft, new Point(bx + (bw - ft.Width) / 2.0, by + (bh - ft.Height) / 2.0));
        }
    }

    /// <summary>Draws one track segment between two positions, with rounded ends.</summary>
    internal static void DrawTrackSegment(
        DrawingContext context,
        IBrush brush,
        double x0,
        double x1,
        double cy,
        double h,
        double rLeft,
        double rRight)
    {
        var w = x1 - x0;
        if (w <= 0.5) return;
        var rect = new Rect(x0, cy - h / 2.0, w, h);
        var rrect = new RoundedRect(
            rect,
            new Vector(rLeft, rLeft),
            new Vector(rRight, rRight),
            new Vector(rRight, rRight),
            new Vector(rLeft, rLeft));
        context.DrawRectangle(brush, null, rrect);
    }
}
