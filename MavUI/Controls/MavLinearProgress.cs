using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

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

    /// <summary>Creates a linear progress indicator at 50 % without indeterminate or wavy mode.</summary>
    public MavLinearProgress() : this(0.5, false, false)
    {
    }

    /// <summary>Creates a linear progress indicator with the given value, indeterminate and wavy modes.</summary>
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

    /// <summary>Measures the track to fill the available width at the track height.</summary>
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

    /// <summary>Draws the track, the active part and the animated wave or indeterminate segment.</summary>
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
