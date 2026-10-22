using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Reuses a single arc geometry while its parameters stay unchanged. Instances are
/// owned by controls so a cached geometry never outlives its platform render interface.
/// </summary>
internal sealed class MavArcCache
{
    private ArcKey _key;
    private Geometry? _geometry;
    private bool _hasKey;

    public Geometry Get(Point center, double radius, double startAngleDeg, double sweepAngleDeg)
    {
        var key = new ArcKey(center.X, center.Y, radius, startAngleDeg, sweepAngleDeg);
        if (_hasKey && key == _key && _geometry is not null)
            return _geometry;

        _key = key;
        _hasKey = true;
        _geometry = MavGeometryHelpers.CreateArc(center, radius, startAngleDeg, sweepAngleDeg);
        return _geometry;
    }

    private readonly record struct ArcKey(
        double CenterX,
        double CenterY,
        double Radius,
        double StartAngle,
        double SweepAngle);
}

internal static class MavGeometryHelpers
{
    public static Geometry CreateArc(Point center, double radius, double startAngleDeg, double sweepAngleDeg)
    {
        var geom = new StreamGeometry();
        if (radius <= 0 || Math.Abs(sweepAngleDeg) < 0.1)
            return geom;

        var clampedSweep = Math.Clamp(sweepAngleDeg, -359.9, 359.9);
        using var ctx = geom.Open();

        // Split sweeps >= 180° into two segments so SVG/Skia arc parameterization is never degenerate.
        int segments = Math.Abs(clampedSweep) > 179.0 ? 2 : 1;
        double segSweep = clampedSweep / segments;

        var startRad = startAngleDeg * Math.PI / 180.0;
        var startPt = new Point(
            center.X + radius * Math.Cos(startRad),
            center.Y + radius * Math.Sin(startRad));
        ctx.BeginFigure(startPt, isFilled: false);

        var sweepDir = clampedSweep >= 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
        for (int i = 1; i <= segments; i++)
        {
            var endDeg = startAngleDeg + segSweep * i;
            var endRad = endDeg * Math.PI / 180.0;
            var endPt = new Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));
            ctx.ArcTo(
                endPt,
                new Size(radius, radius),
                0,
                Math.Abs(segSweep) > 180.0,
                sweepDir);
        }

        ctx.EndFigure(isClosed: false);
        return geom;
    }
}

/// <summary>
/// Material Design 3 linear progress indicator with determinate, indeterminate, stop-dot, and Expressive wavy modes.
/// </summary>
public class MavLinearProgress : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavLinearProgress, double>(nameof(Value), 0.5);

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<MavLinearProgress, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<bool> ShowStopIndicatorProperty =
        AvaloniaProperty.Register<MavLinearProgress, bool>(nameof(ShowStopIndicator), true);

    public static readonly StyledProperty<bool> IsWavyProperty =
        AvaloniaProperty.Register<MavLinearProgress, bool>(nameof(IsWavy));

    public static readonly StyledProperty<double> WaveAmplitudeProperty =
        AvaloniaProperty.Register<MavLinearProgress, double>(nameof(WaveAmplitude), 3.0);

    public static readonly StyledProperty<double> WaveLengthProperty =
        AvaloniaProperty.Register<MavLinearProgress, double>(nameof(WaveLength), 24.0);

    private double _phase;
    private double _wavePhase;
    private bool _running;

    static MavLinearProgress()
    {
        ValueProperty.Changed.AddClassHandler<MavLinearProgress>((p, _) => p.InvalidateVisual());
        IsIndeterminateProperty.Changed.AddClassHandler<MavLinearProgress>((p, _) => p.UpdateLoop());
        ShowStopIndicatorProperty.Changed.AddClassHandler<MavLinearProgress>((p, _) => p.InvalidateVisual());
        WaveAmplitudeProperty.Changed.AddClassHandler<MavLinearProgress>((p, _) => p.InvalidateVisual());
        WaveLengthProperty.Changed.AddClassHandler<MavLinearProgress>((p, _) => p.InvalidateVisual());
        IsVisibleProperty.Changed.AddClassHandler<MavLinearProgress>((p, _) => p.UpdateLoop());
        IsWavyProperty.Changed.AddClassHandler<MavLinearProgress>((p, _) =>
        {
            p.Height = p.IsWavy ? 12 : 4;
            p.UpdateLoop();
            p.InvalidateVisual();
        });
    }

    public MavLinearProgress() : this(0.5, false, false)
    {
    }

    public MavLinearProgress(double value = 0.5, bool indeterminate = false, bool wavy = false)
    {
        Height = wavy ? 12 : 4;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;

        Value = Math.Clamp(value, 0, 1);
        IsIndeterminate = indeterminate;
        IsWavy = wavy;

        AttachedToVisualTree += (_, _) => UpdateLoop();
        DetachedFromVisualTree += (_, _) => StopLoop();

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
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

    public bool ShowStopIndicator
    {
        get => GetValue(ShowStopIndicatorProperty);
        set => SetValue(ShowStopIndicatorProperty, value);
    }

    public bool IsWavy
    {
        get => GetValue(IsWavyProperty);
        set => SetValue(IsWavyProperty, value);
    }

    public double WaveAmplitude
    {
        get => GetValue(WaveAmplitudeProperty);
        set => SetValue(WaveAmplitudeProperty, value);
    }

    public double WaveLength
    {
        get => GetValue(WaveLengthProperty);
        set => SetValue(WaveLengthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 200;
        return new Size(w, IsWavy ? 12 : 4);
    }

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
        MavMotion.Cancel(this, "lin_prog");
    }

    private void RunStep()
    {
        if (!_running) return;
        MavMotion.Animate(this, "lin_prog", 0, 1, 1400, MavMotion.Linear, v =>
        {
            _phase = v;
            _wavePhase = v * Math.PI * 2.0;
            InvalidateVisual();
        }, () =>
        {
            if (_running) RunStep();
        });
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var c = MavTheme.Current.Color;
        var activeBrush = MavTheme.BrushOf(c.Primary);
        var trackBrush = MavTheme.BrushOf(c.SecondaryContainer);
        var strokeW = 4.0;
        var cy = h / 2.0;
        const double gap = 4.0;

        if (IsIndeterminate)
        {
            var rrect = new RoundedRect(new Rect(0, cy - strokeW / 2.0, w, strokeW), strokeW / 2.0);
            context.DrawRectangle(trackBrush, null, rrect);

            var segStart = Math.Max(0, (_phase * 1.4 - 0.4) * w);
            var segEnd = Math.Min(w, _phase * 1.4 * w);
            if (segEnd > segStart + 2)
            {
                DrawSegment(context, c.Primary, activeBrush, segStart, segEnd, cy, strokeW);
            }
            return;
        }

        var v = Math.Clamp(Value, 0, 1);
        var activeEnd = w * v;

        if (activeEnd > 1)
            DrawSegment(context, c.Primary, activeBrush, 0, activeEnd, cy, strokeW);

        var trackStart = activeEnd > 1 ? Math.Min(w, activeEnd + gap) : 0;
        if (w - trackStart > 2)
        {
            var trackRect = new RoundedRect(new Rect(trackStart, cy - strokeW / 2.0, w - trackStart, strokeW), strokeW / 2.0);
            context.DrawRectangle(trackBrush, null, trackRect);
        }

        if (ShowStopIndicator && v < 0.99)
        {
            context.DrawEllipse(activeBrush, null, new Point(w - strokeW / 2.0, cy), strokeW / 2.0, strokeW / 2.0);
        }
    }

    private void DrawSegment(DrawingContext context, Color activeColor, IBrush brush, double x0, double x1, double cy, double strokeW)
    {
        if (!IsWavy)
        {
            var rrect = new RoundedRect(new Rect(x0, cy - strokeW / 2.0, Math.Max(0, x1 - x0), strokeW), strokeW / 2.0);
            context.DrawRectangle(brush, null, rrect);
            return;
        }

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            double waveLen = Math.Max(8.0, WaveLength);
            double amp = Math.Max(0.5, WaveAmplitude);
            bool first = true;
            for (double x = x0; x <= x1; x += 2.0)
            {
                var y = cy + Math.Sin((x / waveLen) * Math.PI * 2.0 - _wavePhase) * amp;
                var pt = new Point(x, y);
                if (first)
                {
                    ctx.BeginFigure(pt, isFilled: false);
                    first = false;
                }
                else
                {
                    ctx.LineTo(pt);
                }
            }
            ctx.EndFigure(isClosed: false);
        }

        var pen = MavTheme.PenOf(activeColor, strokeW, PenLineCap.Round, PenLineJoin.Round);
        context.DrawGeometry(null, pen, geom);
    }
}

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

    public MavCircularProgress() : this(44, true, 0.65)
    {
    }

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

public enum MavSkeletonShape
{
    Rounded,
    Circle,
    Text,
}

/// <summary>
/// Pulsing placeholder skeleton block for loading states.
/// </summary>
public class MavSkeleton : Border
{
    public static readonly StyledProperty<MavSkeletonShape> ShapeProperty =
        AvaloniaProperty.Register<MavSkeleton, MavSkeletonShape>(nameof(Shape), MavSkeletonShape.Rounded);

    private double _pulse = 0.5;
    private bool _running;

    static MavSkeleton()
    {
        IsVisibleProperty.Changed.AddClassHandler<MavSkeleton>((s, _) => s.UpdatePulse());
        ShapeProperty.Changed.AddClassHandler<MavSkeleton>((s, _) => s.ApplyShape());
        WidthProperty.Changed.AddClassHandler<MavSkeleton>((s, _) => s.ApplyShape());
        HeightProperty.Changed.AddClassHandler<MavSkeleton>((s, _) => s.ApplyShape());
    }

    public MavSkeleton() : this(double.NaN, 16, 8)
    {
    }

    public MavSkeleton(double width = 120, double height = 16, double cornerRadius = 8)
    {
        if (!double.IsNaN(width) && width > 0) Width = width;
        Height = height;
        CornerRadius = new CornerRadius(cornerRadius);
        HorizontalAlignment = (!double.IsNaN(width) && width > 0) ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;

        AttachedToVisualTree += (_, _) => UpdatePulse();
        DetachedFromVisualTree += (_, _) => StopPulse();

        MavTheme.Observe(this, (self, theme) => self.UpdateBrush(theme));
    }

    public MavSkeletonShape Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    private void ApplyShape()
    {
        if (Shape == MavSkeletonShape.Circle)
        {
            var sz = !double.IsNaN(Width) && Width > 0 ? Width : (!double.IsNaN(Height) && Height > 0 ? Height : 40);
            CornerRadius = new CornerRadius(sz / 2.0);
        }
        else if (Shape == MavSkeletonShape.Text)
        {
            var h = !double.IsNaN(Height) && Height > 0 ? Height : 14;
            CornerRadius = new CornerRadius(h / 2.0);
        }
    }

    private void UpdatePulse()
    {
        if (!IsVisible || TopLevel.GetTopLevel(this) is null)
        {
            StopPulse();
            return;
        }
        StartPulse();
    }

    private void StartPulse()
    {
        if (_running) return;
        _running = true;
        PulseStep(true);
    }

    private void StopPulse()
    {
        _running = false;
        MavMotion.Cancel(this, "skel_pulse");
    }

    private void PulseStep(bool up)
    {
        if (!_running) return;
        MavMotion.Animate(this, "skel_pulse", up ? 0.35 : 0.85, up ? 0.85 : 0.35, 750, MavMotion.SinInOut, v =>
        {
            _pulse = v;
            UpdateBrush(MavTheme.Current);
        }, () =>
        {
            if (_running) PulseStep(!up);
        });
    }

    private void UpdateBrush(MavTheme t)
    {
        Background = MavTheme.BrushOf(t.Color.SurfaceContainerHighest.WithAlpha((float)_pulse));
    }

    public static MavSkeleton Circle(double size = 40) =>
        new(size, size, size / 2.0);

    public static MavSkeleton Text(double width = 160, double height = 14) =>
        new(width, height, height / 2.0);

    public static Control ListRow(double avatarSize = 40, int lines = 2)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 12,
        };
        row.Add(Circle(avatarSize), 0, 0);
        var col = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var count = Math.Max(1, lines);
        for (var i = 0; i < count; i++)
            col.Children.Add(Text(i == 0 ? 180 : 120, i == 0 ? 14 : 12));
        row.Add(col, 1, 0);
        return row;
    }
}

/// <summary>
/// Material Design 3 Expressive Segmented Progress Bar with discrete rounded segments and gap spacing.
/// </summary>
public class MavSegmentedProgressBar : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavSegmentedProgressBar, double>(nameof(Value), 0.6);

    public static readonly StyledProperty<int> SegmentCountProperty =
        AvaloniaProperty.Register<MavSegmentedProgressBar, int>(nameof(SegmentCount), 5);

    public static readonly StyledProperty<double> SegmentGapProperty =
        AvaloniaProperty.Register<MavSegmentedProgressBar, double>(nameof(SegmentGap), 6.0);

    public static readonly StyledProperty<double> TrackHeightProperty =
        AvaloniaProperty.Register<MavSegmentedProgressBar, double>(nameof(TrackHeight), 6.0);

    static MavSegmentedProgressBar()
    {
        ValueProperty.Changed.AddClassHandler<MavSegmentedProgressBar>((p, _) => p.InvalidateVisual());
        SegmentCountProperty.Changed.AddClassHandler<MavSegmentedProgressBar>((p, _) => p.InvalidateVisual());
        SegmentGapProperty.Changed.AddClassHandler<MavSegmentedProgressBar>((p, _) => p.InvalidateVisual());
        TrackHeightProperty.Changed.AddClassHandler<MavSegmentedProgressBar>((p, _) => p.InvalidateVisual());
    }

    public MavSegmentedProgressBar() : this(5, 0.6, 6.0)
    {
    }

    public MavSegmentedProgressBar(int segmentCount = 5, double value = 0.6, double trackHeight = 6.0)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;
        SegmentCount = Math.Max(1, segmentCount);
        Value = Math.Clamp(value, 0, 1);
        TrackHeight = trackHeight;
        Height = trackHeight;

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, 0, 1));
    }

    public int SegmentCount
    {
        get => GetValue(SegmentCountProperty);
        set => SetValue(SegmentCountProperty, Math.Max(1, value));
    }

    public double SegmentGap
    {
        get => GetValue(SegmentGapProperty);
        set => SetValue(SegmentGapProperty, Math.Max(2, value));
    }

    public double TrackHeight
    {
        get => GetValue(TrackHeightProperty);
        set => SetValue(TrackHeightProperty, Math.Max(4, value));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 200;
        return new Size(w, TrackHeight);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var count = Math.Max(1, SegmentCount);
        var gap = SegmentGap;
        var totalGaps = (count - 1) * gap;
        var segW = Math.Max(4, (w - totalGaps) / count);
        var th = Math.Min(h, TrackHeight);
        var y = (h - th) / 2.0;
        var r = th / 2.0;

        var c = MavTheme.Current.Color;
        var activeBrush = MavTheme.BrushOf(c.Primary);
        var trackBrush = MavTheme.BrushOf(c.SecondaryContainer);
        var progressSegs = Math.Clamp(Value, 0, 1) * count;

        for (int i = 0; i < count; i++)
        {
            var x = i * (segW + gap);
            var segRect = new Rect(x, y, segW, th);
            var fillFrac = Math.Clamp(progressSegs - i, 0, 1);

            if (fillFrac <= 0.001)
            {
                context.DrawRectangle(trackBrush, null, new RoundedRect(segRect, r));
            }
            else if (fillFrac >= 0.999)
            {
                context.DrawRectangle(activeBrush, null, new RoundedRect(segRect, r));
            }
            else
            {
                context.DrawRectangle(trackBrush, null, new RoundedRect(segRect, r));
                var activeRect = new Rect(x, y, segW * fillFrac, th);
                context.DrawRectangle(activeBrush, null, new RoundedRect(activeRect, r));
            }
        }
    }
}

/// <summary>
/// Convenience Material Design 3 Expressive Wavy Linear Progress Indicator.
/// </summary>
public class MavWavyProgressIndicator : MavLinearProgress
{
    public MavWavyProgressIndicator() : this(0.65, false, 3.0, 24.0)
    {
    }

    public MavWavyProgressIndicator(
        double value = 0.65,
        bool indeterminate = false,
        double amplitude = 3.0,
        double wavelength = 24.0)
        : base(value, indeterminate, wavy: true)
    {
        WaveAmplitude = amplitude;
        WaveLength = wavelength;
    }
}

/// <summary>
/// Material Design 3 Expressive loading indicator: a rotating, softly pulsing burst of
/// rounded lobes drawn with the primary color (and optional container circle).
/// </summary>
public class MavLoadingIndicator : Control
{
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<MavLoadingIndicator, double>(nameof(Size), 48.0);

    public static readonly StyledProperty<bool> IsContainedProperty =
        AvaloniaProperty.Register<MavLoadingIndicator, bool>(nameof(IsContained));

    public static readonly StyledProperty<Color?> IndicatorColorProperty =
        AvaloniaProperty.Register<MavLoadingIndicator, Color?>(nameof(IndicatorColor));

    private double _phase;
    private bool _running;

    static MavLoadingIndicator()
    {
        SizeProperty.Changed.AddClassHandler<MavLoadingIndicator>((l, e) =>
        {
            var sz = e.GetNewValue<double>();
            l.Width = sz;
            l.Height = sz;
            l.InvalidateVisual();
        });
        IsContainedProperty.Changed.AddClassHandler<MavLoadingIndicator>((l, _) => l.InvalidateVisual());
        IndicatorColorProperty.Changed.AddClassHandler<MavLoadingIndicator>((l, _) => l.InvalidateVisual());
        IsVisibleProperty.Changed.AddClassHandler<MavLoadingIndicator>((l, _) => l.UpdateLoop());
    }

    public MavLoadingIndicator() : this(48, false)
    {
    }

    public MavLoadingIndicator(double size = 48, bool contained = false)
    {
        Size = size;
        Width = size;
        Height = size;
        IsContained = contained;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        AttachedToVisualTree += (_, _) => UpdateLoop();
        DetachedFromVisualTree += (_, _) => StopLoop();

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool IsContained
    {
        get => GetValue(IsContainedProperty);
        set => SetValue(IsContainedProperty, value);
    }

    public Color? IndicatorColor
    {
        get => GetValue(IndicatorColorProperty);
        set => SetValue(IndicatorColorProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    private void UpdateLoop()
    {
        if (!IsVisible || TopLevel.GetTopLevel(this) is null)
        {
            StopLoop();
            return;
        }
        StartLoop();
    }

    private void StartLoop()
    {
        if (_running) return;
        _running = true;
        RunStep();
    }

    private void StopLoop()
    {
        _running = false;
        MavMotion.Cancel(this, "md_loading_ind");
    }

    private void RunStep()
    {
        if (!_running) return;
        MavMotion.Animate(this, "md_loading_ind", 0, 1, 1600, MavMotion.Linear, v =>
        {
            _phase = v;
            InvalidateVisual();
        }, () =>
        {
            if (_running) RunStep();
        });
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var c = MavTheme.Current.Color;
        var center = new Point(w / 2.0, h / 2.0);
        var outerR = Math.Min(w, h) / 2.0;

        if (IsContained)
        {
            context.DrawEllipse(MavTheme.BrushOf(c.PrimaryContainer), null, center, outerR, outerR);
        }

        var fgColor = IndicatorColor ?? (IsContained ? c.OnPrimaryContainer : c.Primary);
        var radius = IsContained ? outerR * 0.68 : Math.Max(2, outerR - 2);
        var rotation = _phase * Math.PI * 2.0;
        var pulse = 0.86 + 0.14 * Math.Sin(_phase * Math.PI * 4.0);

        const int lobes = 7;
        var pts = new Point[lobes * 2];
        for (int i = 0; i < lobes * 2; i++)
        {
            var angle = rotation + Math.PI * i / lobes;
            var r = (i % 2 == 0) ? radius * pulse : radius * 0.62;
            pts[i] = new Point(center.X + r * Math.Cos(angle), center.Y + r * Math.Sin(angle));
        }

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            var n = pts.Length;
            var firstMid = new Point((pts[n - 1].X + pts[0].X) * 0.5, (pts[n - 1].Y + pts[0].Y) * 0.5);
            ctx.BeginFigure(firstMid, isFilled: true);
            for (int i = 0; i < n; i++)
            {
                var curr = pts[i];
                var next = pts[(i + 1) % n];
                var mid = new Point((curr.X + next.X) * 0.5, (curr.Y + next.Y) * 0.5);
                ctx.QuadraticBezierTo(curr, mid);
            }
            ctx.EndFigure(isClosed: true);
        }

        context.DrawGeometry(MavTheme.BrushOf(fgColor), null, geom);
    }
}
