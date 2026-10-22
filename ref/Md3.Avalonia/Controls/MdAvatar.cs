using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdAvatarStatus
{
    None,
    Online,
    Busy,
    Away,
    Offline,
}

/// <summary>
/// Circular monogram, icon, or image avatar with tonal background and optional presence status dot.
/// </summary>
public class MdAvatar : MdContentView
{
    public static readonly StyledProperty<string?> InitialsProperty =
        AvaloniaProperty.Register<MdAvatar, string?>(nameof(Initials), "A");

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdAvatar, string?>(nameof(Icon));

    public static readonly StyledProperty<IImage?> ImageSourceProperty =
        AvaloniaProperty.Register<MdAvatar, IImage?>(nameof(ImageSource));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<MdAvatar, double>(nameof(Size), 40.0);

    public static readonly StyledProperty<MdAvatarStatus> StatusProperty =
        AvaloniaProperty.Register<MdAvatar, MdAvatarStatus>(nameof(Status), MdAvatarStatus.None);

    public static readonly StyledProperty<Color?> CustomFillColorProperty =
        AvaloniaProperty.Register<MdAvatar, Color?>(nameof(CustomFillColor));

    public static readonly StyledProperty<Color?> CustomTextColorProperty =
        AvaloniaProperty.Register<MdAvatar, Color?>(nameof(CustomTextColor));

    private readonly MdSurface _surface;
    private readonly TextBlock _label;
    private readonly MdIcon _icon;
    private readonly Image _image;
    private readonly Border _statusRing;
    private readonly Border _statusDot;

    static MdAvatar()
    {
        InitialsProperty.Changed.AddClassHandler<MdAvatar>((a, _) => a.SyncContent());
        IconProperty.Changed.AddClassHandler<MdAvatar>((a, _) => a.SyncContent());
        ImageSourceProperty.Changed.AddClassHandler<MdAvatar>((a, _) => a.SyncContent());
        SizeProperty.Changed.AddClassHandler<MdAvatar>((a, _) => a.SyncSize());
        StatusProperty.Changed.AddClassHandler<MdAvatar>((a, _) =>
        {
            a.SyncSize();
            a.ApplyTheme(MdTheme.Current);
        });
        CustomFillColorProperty.Changed.AddClassHandler<MdAvatar>((a, _) => a.ApplyTheme(MdTheme.Current));
        CustomTextColorProperty.Changed.AddClassHandler<MdAvatar>((a, _) => a.ApplyTheme(MdTheme.Current));
    }

    public MdAvatar() : this("A")
    {
    }

    public MdAvatar(
        string? initials = "A",
        string? icon = null,
        double size = 40,
        MdAvatarStatus status = MdAvatarStatus.None,
        IImage? imageSource = null)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _label = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontWeight = FontWeight.Medium,
        };
        _icon = new MdIcon { IsVisible = false, UseThemeDefaultColor = false };
        _image = new Image
        {
            Stretch = Stretch.UniformToFill,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var innerGrid = new Grid();
        innerGrid.Children.Add(_label);
        innerGrid.Children.Add(_icon);
        innerGrid.Children.Add(_image);

        _surface = new MdSurface
        {
            Width = size,
            Height = size,
            Radius = size / 2.0,
            ClipToBounds = true,
            Content = innerGrid,
        };

        _statusDot = new Border();
        _statusRing = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(2),
            IsVisible = false,
            Child = _statusDot,
        };

        var outerGrid = new Grid();
        outerGrid.Children.Add(_surface);
        outerGrid.Children.Add(_statusRing);

        Content = outerGrid;
        Initials = initials;
        Icon = icon;
        ImageSource = imageSource;
        Size = size;
        Status = status;

        SyncContent();
        SyncSize();
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    [Content]
    public string? Initials
    {
        get => GetValue(InitialsProperty);
        set => SetValue(InitialsProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IImage? ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public MdAvatarStatus Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public Color? CustomFillColor
    {
        get => GetValue(CustomFillColorProperty);
        set => SetValue(CustomFillColorProperty, value);
    }

    public Color? CustomTextColor
    {
        get => GetValue(CustomTextColorProperty);
        set => SetValue(CustomTextColorProperty, value);
    }

    private void SyncContent()
    {
        var hasImage = ImageSource is not null;
        var hasIcon = !hasImage && !string.IsNullOrEmpty(Icon);

        _image.Source = ImageSource;
        _image.IsVisible = hasImage;

        _icon.Glyph = Icon;
        _icon.IsVisible = hasIcon;

        _label.Text = Initials;
        _label.IsVisible = !hasImage && !hasIcon;
    }

    private void SyncSize()
    {
        var s = Math.Max(20, Size);
        _surface.Width = s;
        _surface.Height = s;
        _surface.Radius = s / 2.0;
        _label.FontSize = Math.Round(s * 0.40);
        _icon.Size = Math.Round(s * 0.55);

        var dotSize = Math.Clamp(Math.Round(s * 0.26), 8, 14);
        var ringSize = dotSize + 4;
        _statusDot.Width = dotSize;
        _statusDot.Height = dotSize;
        _statusDot.CornerRadius = new CornerRadius(dotSize / 2.0);
        _statusRing.Width = ringSize;
        _statusRing.Height = ringSize;
        _statusRing.CornerRadius = new CornerRadius(ringSize / 2.0);
        _statusRing.IsVisible = Status != MdAvatarStatus.None;
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _label.FontFamily = t.Typography.PlainFont;
        var bg = CustomFillColor ?? c.PrimaryContainer;
        var fg = CustomTextColor ?? c.OnPrimaryContainer;
        _surface.FillColor = bg;
        _label.Foreground = MdTheme.BrushOf(fg);
        _icon.Color = fg;

        _statusRing.Background = MdTheme.BrushOf(c.Surface);
        var statusColor = Status switch
        {
            MdAvatarStatus.Online => Color.Parse("#2E7D32"),
            MdAvatarStatus.Busy => c.Error,
            MdAvatarStatus.Away => Color.Parse("#EF6C00"),
            MdAvatarStatus.Offline => c.Outline,
            _ => Colors.Transparent,
        };
        _statusDot.Background = MdTheme.BrushOf(statusColor);
    }
}
