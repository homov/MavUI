using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

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

    /// <summary>Creates a segmented progress bar with five segments at 60 %.</summary>
    public MavSegmentedProgressBar() : this(5, 0.6, 6.0)
    {
    }

    /// <summary>Creates a segmented progress bar with the given segment count, value and track height.</summary>
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

    /// <summary>Measures the bar to fill the available width at the track height.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 200;
        return new Size(w, TrackHeight);
    }

    /// <summary>Draws the segments, filled up to the current value.</summary>
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
