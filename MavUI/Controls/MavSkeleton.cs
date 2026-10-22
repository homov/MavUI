using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

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
