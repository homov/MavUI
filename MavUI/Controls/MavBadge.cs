using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 small dot or numbered badge.
/// </summary>
public class MavBadge : MavContentView
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MavBadge, string?>(nameof(Text));

    public static readonly StyledProperty<int?> CountProperty =
        AvaloniaProperty.Register<MavBadge, int?>(nameof(Count));

    public static readonly StyledProperty<int> MaxCountProperty =
        AvaloniaProperty.Register<MavBadge, int>(nameof(MaxCount), 999);

    public static readonly StyledProperty<bool> IsDotProperty =
        AvaloniaProperty.Register<MavBadge, bool>(nameof(IsDot));

    private readonly MavSurface _surface;
    private readonly TextBlock _label;

    static MavBadge()
    {
        TextProperty.Changed.AddClassHandler<MavBadge>((b, _) => b.Sync());
        CountProperty.Changed.AddClassHandler<MavBadge>((b, _) => b.Sync());
        MaxCountProperty.Changed.AddClassHandler<MavBadge>((b, _) => b.Sync());
        IsDotProperty.Changed.AddClassHandler<MavBadge>((b, _) => b.Sync());
    }

    /// <summary>Creates a dot badge with no text.</summary>
    public MavBadge() : this(null)
    {
    }

    /// <summary>Creates a badge that shows the given text, or a dot when the text is empty.</summary>
    public MavBadge(string? text = null)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _label = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };

        _surface = new MavSurface
        {
            Content = _label,
        };

        Content = _surface;
        if (text is not null) Text = text;

        Sync();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int? Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public int MaxCount
    {
        get => GetValue(MaxCountProperty);
        set => SetValue(MaxCountProperty, value);
    }

    public bool IsDot
    {
        get => GetValue(IsDotProperty);
        set => SetValue(IsDotProperty, value);
    }

    private void Sync()
    {
        string? display = Text;
        if (Count is { } n)
            display = n > MaxCount ? $"{MaxCount}+" : n.ToString();

        var isDot = IsDot || string.IsNullOrEmpty(display);
        _label.Text = display ?? string.Empty;
        _label.IsVisible = !isDot;

        if (isDot)
        {
            _surface.Width = 6;
            _surface.Height = 6;
            _surface.MinWidth = 6;
            _surface.Padding = new Thickness(0);
            _surface.Radius = 3;
        }
        else
        {
            var textW = MavTextMetrics.MeasureWidth(display, 11, bold: true);
            var w = display!.Length == 1 ? 16.0 : Math.Max(16.0, Math.Ceiling(textW) + 8.0);
            _surface.Width = w;
            _surface.MinWidth = 16;
            _surface.Height = 16;
            _surface.Padding = new Thickness(display.Length > 1 ? 4 : 0, 0);
            _surface.Radius = 8;
        }
    }

    private void ApplyTheme(MavTheme t)
    {
        _label.Apply(MavTypeRole.LabelSmall, t.Typography);
        _surface.FillColor = t.Color.Error;
        _label.Foreground = MavTheme.BrushOf(t.Color.OnError);
    }
}
