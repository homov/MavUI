using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

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

    /// <summary>Creates a 48 dp loading indicator without a container.</summary>
    public MavLoadingIndicator() : this(48, false)
    {
    }

    /// <summary>Creates a loading indicator of the given <paramref name="size"/>, optionally on a container circle.</summary>
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

    /// <summary>Draws the animated lobes at the current rotation and pulse phase.</summary>
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
