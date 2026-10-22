using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 single-thumb slider with 16dp track, vertical bar thumb, gap cutouts,
/// discrete tick marks, centered zero-origin mode, and value indicator bubble.
/// </summary>
public class MdSlider : Control, IMdDraggableControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MdSlider, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MdSlider, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MdSlider, double>(nameof(Value), 50.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> DivisionsProperty =
        AvaloniaProperty.Register<MdSlider, int>(nameof(Divisions), 0);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MdSlider, double>(nameof(Step), 0.0);

    public static readonly StyledProperty<bool> ShowValueIndicatorProperty =
        AvaloniaProperty.Register<MdSlider, bool>(nameof(ShowValueIndicator), true);

    public static readonly StyledProperty<bool> CenteredProperty =
        AvaloniaProperty.Register<MdSlider, bool>(nameof(Centered));

    private bool _dragging;
    private double _bubbleScale;

    public event EventHandler<double>? ValueChanged;

    static MdSlider()
    {
        MinimumProperty.Changed.AddClassHandler<MdSlider>((s, _) => s.InvalidateVisual());
        MaximumProperty.Changed.AddClassHandler<MdSlider>((s, _) => s.InvalidateVisual());
        ValueProperty.Changed.AddClassHandler<MdSlider>((s, e) =>
        {
            s.InvalidateVisual();
            s.ValueChanged?.Invoke(s, e.GetNewValue<double>());
        });
        DivisionsProperty.Changed.AddClassHandler<MdSlider>((s, _) => s.InvalidateVisual());
        CenteredProperty.Changed.AddClassHandler<MdSlider>((s, _) => s.InvalidateVisual());
    }

    public MdSlider() : this(0, 100, 50, 0)
    {
    }

    public MdSlider(double min = 0, double max = 100, double value = 50, int divisions = 0)
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
        MdAutomation.SetControlType(this, AutomationControlType.Slider);

        MdTheme.Observe(this, (self, _) => self.InvalidateVisual());
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

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 240;
        return new Size(w, 48);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        if (MdGestureZones.ShouldDeferToSystem(this, e)) return;
        _dragging = true;
        e.Pointer.Capture(this);
        e.PreventGestureRecognition();
        AnimateBubble(1);
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || !IsEnabled) return;
        e.PreventGestureRecognition();
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);
        AnimateBubble(0);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_dragging)
        {
            _dragging = false;
            AnimateBubble(0);
        }
    }

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
        MdMotion.Animate(this, "sl_bubble", _bubbleScale, target, MdMotion.Short3, MdMotion.CubicOut, v =>
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

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var t = MdTheme.Current;
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
        var activeBrush = MdTheme.BrushOf(activeColor);
        var inactiveBrush = MdTheme.BrushOf(inactiveColor);

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
                context.DrawEllipse(MdTheme.BrushOf(dotCol.WithAlpha(0.75f)), null, new Point(tx, cy), 2, 2);
            }
        }

        var thumbW = _dragging ? 2.5 : 4.0;
        const double thumbH = 44.0;
        var thumbRect = new RoundedRect(new Rect(thumbX - thumbW / 2.0, cy - thumbH / 2.0, thumbW, thumbH), thumbW / 2.0);
        context.DrawRectangle(activeBrush, null, thumbRect);

        if (ShowValueIndicator && _bubbleScale > 0.02)
        {
            var text = (ValueFormatter ?? (v => Math.Round(v).ToString(CultureInfo.InvariantCulture)))(Value);
            var ft = MdTextMetrics.GetFormattedText(text, 12, MdFonts.RobotoMediumTypeface, c.OnPrimary);

            var bw = Math.Max(28.0, ft.Width + 16.0) * _bubbleScale;
            var bh = 28.0 * _bubbleScale;
            var by = cy - thumbH / 2.0 - bh - 4.0;
            var bx = Math.Clamp(thumbX - bw / 2.0, 2, Math.Max(2, w - bw - 2));
            var bRect = new RoundedRect(new Rect(bx, by, bw, bh), bh / 2.0);
            using (context.PushOpacity(_bubbleScale))
                context.DrawRectangle(MdTheme.BrushOf(c.Primary), null, bRect);
            using (context.PushOpacity(_bubbleScale))
                context.DrawText(ft, new Point(bx + (bw - ft.Width) / 2.0, by + (bh - ft.Height) / 2.0));
        }
    }

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

/// <summary>
/// Material Design 3 dual-thumb range slider.
/// </summary>
public class MdRangeSlider : Control, IMdDraggableControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MdRangeSlider, double>(nameof(Minimum), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MdRangeSlider, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<double> StartValueProperty =
        AvaloniaProperty.Register<MdRangeSlider, double>(nameof(StartValue), 25.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> EndValueProperty =
        AvaloniaProperty.Register<MdRangeSlider, double>(nameof(EndValue), 75.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> DivisionsProperty =
        AvaloniaProperty.Register<MdRangeSlider, int>(nameof(Divisions), 0);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MdRangeSlider, double>(nameof(Step), 0.0);

    public static readonly StyledProperty<bool> ShowValueIndicatorProperty =
        AvaloniaProperty.Register<MdRangeSlider, bool>(nameof(ShowValueIndicator), true);

    private int _activeThumb = -1;
    private int _lastFocusedThumb;
    private double _bubbleScale;

    public event EventHandler<(double Start, double End)>? RangeChanged;

    static MdRangeSlider()
    {
        MinimumProperty.Changed.AddClassHandler<MdRangeSlider>((s, _) => s.InvalidateVisual());
        MaximumProperty.Changed.AddClassHandler<MdRangeSlider>((s, _) => s.InvalidateVisual());
        StartValueProperty.Changed.AddClassHandler<MdRangeSlider>((s, _) =>
        {
            s.InvalidateVisual();
            s.RangeChanged?.Invoke(s, (s.StartValue, s.EndValue));
        });
        EndValueProperty.Changed.AddClassHandler<MdRangeSlider>((s, _) =>
        {
            s.InvalidateVisual();
            s.RangeChanged?.Invoke(s, (s.StartValue, s.EndValue));
        });
        DivisionsProperty.Changed.AddClassHandler<MdRangeSlider>((s, _) => s.InvalidateVisual());
    }

    public MdRangeSlider() : this(0, 100, 25, 75, 0)
    {
    }

    public MdRangeSlider(double min = 0, double max = 100, double start = 25, double end = 75, int divisions = 0)
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
        StartValue = Math.Clamp(start, min, max);
        EndValue = Math.Clamp(end, StartValue, max);
        MdAutomation.SetControlType(this, AutomationControlType.Slider);

        MdTheme.Observe(this, (self, _) => self.InvalidateVisual());
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

    public double StartValue
    {
        get => GetValue(StartValueProperty);
        set => SetValue(StartValueProperty, Math.Clamp(Snap(value), Minimum, EndValue));
    }

    public double EndValue
    {
        get => GetValue(EndValueProperty);
        set => SetValue(EndValueProperty, Math.Clamp(Snap(value), StartValue, Maximum));
    }

    public double LowerValue
    {
        get => StartValue;
        set => StartValue = value;
    }

    public double UpperValue
    {
        get => EndValue;
        set => EndValue = value;
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

    public Func<double, string>? ValueFormatter { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 240;
        return new Size(w, 48);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled) return;
        if (MdGestureZones.ShouldDeferToSystem(this, e)) return;

        var x = e.GetPosition(this).X;
        const double pad = 12.0;
        var trackW = Math.Max(1.0, Bounds.Width - pad * 2);
        var range = Math.Max(1e-6, Maximum - Minimum);
        var startX = pad + trackW * ((StartValue - Minimum) / range);
        var endX = pad + trackW * ((EndValue - Minimum) / range);

        _activeThumb = Math.Abs(x - startX) <= Math.Abs(x - endX) ? 0 : 1;
        _lastFocusedThumb = _activeThumb;
        e.Pointer.Capture(this);
        e.PreventGestureRecognition();
        AnimateBubble(1);
        UpdateFromX(x);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_activeThumb < 0 || !IsEnabled) return;
        e.PreventGestureRecognition();
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_activeThumb < 0) return;
        _activeThumb = -1;
        e.Pointer.Capture(null);
        AnimateBubble(0);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_activeThumb >= 0)
        {
            _activeThumb = -1;
            AnimateBubble(0);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEnabled) return;

        var range = Math.Max(1e-6, Maximum - Minimum);
        var step = Step > 0 ? Step : (Divisions > 0 ? range / Divisions : range * 0.05);
        if (e.Key is Key.Left or Key.Down)
        {
            if (_lastFocusedThumb == 0)
                StartValue = Math.Max(Minimum, StartValue - step);
            else
                EndValue = Math.Max(StartValue, EndValue - step);
            e.Handled = true;
        }
        else if (e.Key is Key.Right or Key.Up)
        {
            if (_lastFocusedThumb == 0)
                StartValue = Math.Min(EndValue, StartValue + step);
            else
                EndValue = Math.Min(Maximum, EndValue + step);
            e.Handled = true;
        }
        else if (e.Key == Key.PageUp)
        {
            _lastFocusedThumb = 1;
            e.Handled = true;
        }
        else if (e.Key == Key.PageDown)
        {
            _lastFocusedThumb = 0;
            e.Handled = true;
        }
    }

    private void AnimateBubble(double target)
    {
        MdMotion.Animate(this, "rsl_bubble", _bubbleScale, target, MdMotion.Short3, MdMotion.CubicOut, v =>
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
        var val = Snap(Minimum + norm * (Maximum - Minimum));

        if (_activeThumb == 0)
            StartValue = Math.Min(val, EndValue);
        else
            EndValue = Math.Max(val, StartValue);
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

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var t = MdTheme.Current;
        var c = t.Color;
        const double pad = 12.0;
        const double trackH = 16.0;
        const double gap = 6.0;
        var cy = h / 2.0 + 4.0;
        var trackLeft = pad;
        var trackRight = Math.Max(pad + 1, w - pad);
        var trackW = trackRight - trackLeft;

        var range = Math.Max(1e-6, Maximum - Minimum);
        var n0 = Math.Clamp((StartValue - Minimum) / range, 0, 1);
        var n1 = Math.Clamp((EndValue - Minimum) / range, 0, 1);
        var x0 = trackLeft + trackW * n0;
        var x1 = trackLeft + trackW * n1;

        var activeBrush = MdTheme.BrushOf(IsEnabled ? c.Primary : c.OnSurface.WithAlpha(0.38f));
        var inactiveBrush = MdTheme.BrushOf(IsEnabled ? c.SecondaryContainer : c.OnSurface.WithAlpha(0.12f));

        MdSlider.DrawTrackSegment(context, inactiveBrush, trackLeft, x0 - gap, cy, trackH, 8, 2);
        MdSlider.DrawTrackSegment(context, activeBrush, x0 + gap, x1 - gap, cy, trackH, 2, 2);
        MdSlider.DrawTrackSegment(context, inactiveBrush, x1 + gap, trackRight, cy, trackH, 2, 8);

        if (Divisions > 0)
        {
            for (int i = 0; i <= Divisions; i++)
            {
                var tf = (double)i / Divisions;
                var tx = trackLeft + trackW * tf;
                if (Math.Abs(tx - x0) < gap + 3 || Math.Abs(tx - x1) < gap + 3) continue;
                var onActive = tx >= x0 && tx <= x1;
                var dotCol = onActive ? c.OnPrimary : c.OnSecondaryContainer;
                context.DrawEllipse(MdTheme.BrushOf(dotCol.WithAlpha(0.75f)), null, new Point(tx, cy), 2, 2);
            }
        }

        const double thumbH = 44.0;
        var w0 = _activeThumb == 0 ? 2.5 : 4.0;
        var w1 = _activeThumb == 1 ? 2.5 : 4.0;
        context.DrawRectangle(activeBrush, null, new RoundedRect(new Rect(x0 - w0 / 2.0, cy - thumbH / 2.0, w0, thumbH), w0 / 2.0));
        context.DrawRectangle(activeBrush, null, new RoundedRect(new Rect(x1 - w1 / 2.0, cy - thumbH / 2.0, w1, thumbH), w1 / 2.0));

        if (ShowValueIndicator && _bubbleScale > 0.02 && _activeThumb >= 0)
        {
            var val = _activeThumb == 0 ? StartValue : EndValue;
            var tx = _activeThumb == 0 ? x0 : x1;
            var text = (ValueFormatter ?? (v => Math.Round(v).ToString(CultureInfo.InvariantCulture)))(val);
            var ft = MdTextMetrics.GetFormattedText(text, 12, MdFonts.RobotoMediumTypeface, c.OnPrimary);

            var bw = Math.Max(28.0, ft.Width + 16.0) * _bubbleScale;
            var bh = 28.0 * _bubbleScale;
            var by = cy - thumbH / 2.0 - bh - 4.0;
            var bx = Math.Clamp(tx - bw / 2.0, 2, Math.Max(2, w - bw - 2));
            using (context.PushOpacity(_bubbleScale))
                context.DrawRectangle(
                    MdTheme.BrushOf(c.Primary),
                    null,
                    new RoundedRect(new Rect(bx, by, bw, bh), bh / 2.0));
            using (context.PushOpacity(_bubbleScale))
                context.DrawText(ft, new Point(bx + (bw - ft.Width) / 2.0, by + (bh - ft.Height) / 2.0));
        }
    }
}
