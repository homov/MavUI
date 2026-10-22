using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 circular progress indicator with determinate, indeterminate, and Expressive wavy modes.
/// </summary>
public class MavCircularProgress : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavCircularProgress, double>(nameof(Value), 0.65);

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MavCircularProgress, bool>(nameof(IsIndeterminate), true);

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<MavCircularProgress, double>(nameof(Size), 44.0);

    public static readonly StyledProperty<double> StrokeWidthProperty =
        AvaloniaProperty.Register<MavCircularProgress, double>(nameof(StrokeWidth), 4.0);

    public static readonly StyledProperty<bool> IsWavyProperty =
        AvaloniaProperty.Register<MavCircularProgress, bool>(nameof(IsWavy));

    public static readonly StyledProperty<Color?> CustomActiveColorProperty =
        AvaloniaProperty.Register<MavCircularProgress, Color?>(nameof(CustomActiveColor));

    private double _spinPhase;
    private bool _running;
    private readonly MavArcCache _trackArc = new();
    private readonly MavArcCache _activeArc = new();

    static MavCircularProgress()
    {
        ValueProperty.Changed.AddClassHandler<MavCircularProgress>((p, _) => p.InvalidateVisual());
        IsIndeterminateProperty.Changed.AddClassHandler<MavCircularProgress>((p, _) => p.UpdateLoop());
        IsVisibleProperty.Changed.AddClassHandler<MavCircularProgress>((p, _) => p.UpdateLoop());
        SizeProperty.Changed.AddClassHandler<MavCircularProgress>((p, e) =>
        {
            var sz = e.GetNewValue<double>();
            p.Width = sz;
            p.Height = sz;
            p.InvalidateVisual();
        });
        StrokeWidthProperty.Changed.AddClassHandler<MavCircularProgress>((p, _) => p.InvalidateVisual());
        CustomActiveColorProperty.Changed.AddClassHandler<MavCircularProgress>((p, _) => p.InvalidateVisual());
        IsWavyProperty.Changed.AddClassHandler<MavCircularProgress>((p, _) =>
        {
            p.UpdateLoop();
            p.InvalidateVisual();
        });
    }

    /// <summary>Creates an indeterminate circular progress indicator of 44 dp.</summary>
    public MavCircularProgress() : this(44, true, 0.65)
    {
    }

    /// <summary>Creates a circular progress indicator of the given size, mode and value.</summary>
    public MavCircularProgress(double size = 44, bool indeterminate = true, double value = 0.65)
    {
        Size = size;
        Width = size;
        Height = size;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Value = Math.Clamp(value, 0, 1);
        IsIndeterminate = indeterminate;

        AttachedToVisualTree += (_, _) => UpdateLoop();
        DetachedFromVisualTree += (_, _) => StopLoop();

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    /// <summary>Creates a circular progress indicator of the given size and stroke width.</summary>
    public MavCircularProgress(double size, double strokeWidth)
        : this(size, indeterminate: true, value: 0.65)
    {
        StrokeWidth = strokeWidth;
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, 0, 1));
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double Diameter
    {
        get => Size;
        set => Size = value;
    }

    public Color? CustomActiveColor
    {
        get => GetValue(CustomActiveColorProperty);
        set => SetValue(CustomActiveColorProperty, value);
    }

    public double StrokeWidth
    {
        get => GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public bool IsWavy
    {
        get => GetValue(IsWavyProperty);
        set => SetValue(IsWavyProperty, value);
    }

    /// <summary>
    /// Measures the indicator as a square of the configured size.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    private void UpdateLoop()
    {
        if ((!IsIndeterminate && !IsWavy) || !IsVisible || TopLevel.GetTopLevel(this) is null)
        {
            StopLoop();
            InvalidateVisual();
            return;
        }

        if (_running) return;
        _running = true;
        RunStep();
    }

    private void StopLoop()
    {
        _running = false;
        MavMotion.Cancel(this, "circ_prog");
    }

    private void RunStep()
    {
        if (!_running) return;
        MavMotion.Animate(this, "circ_prog", 0, 1, 1350, MavMotion.Linear, v =>
        {
            _spinPhase = v;
            InvalidateVisual();
        }, () =>
        {
            if (_running) RunStep();
        });
    }

    /// <summary>Draws the ring track and the arc for the current value or animation phase.</summary>
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var c = MavTheme.Current.Color;
        var center = new Point(w / 2.0, h / 2.0);
        var strokeW = StrokeWidth;
        var r = Math.Max(4, Math.Min(w, h) / 2.0 - strokeW - (IsWavy ? 2.5 : 0));

        var activePen = MavTheme.PenOf(CustomActiveColor ?? c.Primary, strokeW, PenLineCap.Round, PenLineJoin.Round);
        var trackPen = MavTheme.PenOf(c.SecondaryContainer, strokeW, PenLineCap.Round);

        if (IsIndeterminate)
        {
            var startAngle = _spinPhase * 360.0 - 90.0;
            var sweep = 45.0 + 220.0 * (0.5 - 0.5 * Math.Cos(_spinPhase * Math.PI * 2.0));
            DrawCircularSegment(context, activePen, center, r, startAngle, sweep);
            return;
        }

        var v = Math.Clamp(Value, 0, 1);
        var activeSweep = 360.0 * v;
        const double gapDeg = 14.0;

        if (v < 0.99)
        {
            var trackStart = -90.0 + activeSweep + (v > 0.01 ? gapDeg : 0);
            var trackSweep = Math.Max(0, 360.0 - activeSweep - (v > 0.01 ? gapDeg * 2 : 0));
            if (trackSweep > 2)
                context.DrawGeometry(null, trackPen, _trackArc.Get(center, r, trackStart, trackSweep));
        }

        if (activeSweep > 1)
            DrawCircularSegment(context, activePen, center, r, -90.0, activeSweep, cacheable: true);
    }

    private void DrawCircularSegment(
        DrawingContext context,
        IPen pen,
        Point center,
        double r,
        double startDeg,
        double sweepDeg,
        bool cacheable = false)
    {
        if (!IsWavy)
        {
            var geometry = cacheable
                ? _activeArc.Get(center, r, startDeg, sweepDeg)
                : MavGeometryHelpers.CreateArc(center, r, startDeg, sweepDeg);
            context.DrawGeometry(null, pen, geometry);
            return;
        }

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            int steps = Math.Max(16, (int)(sweepDeg * 0.6));
            for (int i = 0; i <= steps; i++)
            {
                var t = (double)i / steps;
                var deg = startDeg + sweepDeg * t;
                var rad = deg * Math.PI / 180.0;
                var wave = Math.Sin(rad * 8.0 - _spinPhase * Math.PI * 2.0) * 1.8;
                var cr = r + wave;
                var pt = new Point(center.X + cr * Math.Cos(rad), center.Y + cr * Math.Sin(rad));
                if (i == 0) ctx.BeginFigure(pt, isFilled: false);
                else ctx.LineTo(pt);
            }
            ctx.EndFigure(isClosed: false);
        }
        context.DrawGeometry(null, pen, geom);
    }
}
