using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;
using System.Collections;
using System.Globalization;
using Avalonia.Collections;
using Mav.Platform;

namespace Mav.Controls;

/// <summary>
/// Circular monogram, icon, or image avatar with tonal background and optional presence status dot.
/// </summary>
public class MavAvatar : MavContentView
{
    public static readonly StyledProperty<string?> InitialsProperty =
        AvaloniaProperty.Register<MavAvatar, string?>(nameof(Initials), "A");

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavAvatar, string?>(nameof(Icon));

    public static readonly StyledProperty<IImage?> ImageSourceProperty =
        AvaloniaProperty.Register<MavAvatar, IImage?>(nameof(ImageSource));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<MavAvatar, double>(nameof(Size), 40.0);

    public static readonly StyledProperty<MavAvatarStatus> StatusProperty =
        AvaloniaProperty.Register<MavAvatar, MavAvatarStatus>(nameof(Status), MavAvatarStatus.None);

    public static readonly StyledProperty<Color?> CustomFillColorProperty =
        AvaloniaProperty.Register<MavAvatar, Color?>(nameof(CustomFillColor));

    public static readonly StyledProperty<Color?> CustomTextColorProperty =
        AvaloniaProperty.Register<MavAvatar, Color?>(nameof(CustomTextColor));

    private readonly MavSurface _surface;
    private readonly TextBlock _label;
    private readonly MavIcon _icon;
    private readonly Image _image;
    private readonly Border _statusRing;
    private readonly Border _statusDot;

    static MavAvatar()
    {
        InitialsProperty.Changed.AddClassHandler<MavAvatar>((a, _) => a.SyncContent());
        IconProperty.Changed.AddClassHandler<MavAvatar>((a, _) => a.SyncContent());
        ImageSourceProperty.Changed.AddClassHandler<MavAvatar>((a, _) => a.SyncContent());
        SizeProperty.Changed.AddClassHandler<MavAvatar>((a, _) => a.SyncSize());
        StatusProperty.Changed.AddClassHandler<MavAvatar>((a, _) =>
        {
            a.SyncSize();
            a.ApplyTheme(MavTheme.Current);
        });
        CustomFillColorProperty.Changed.AddClassHandler<MavAvatar>((a, _) => a.ApplyTheme(MavTheme.Current));
        CustomTextColorProperty.Changed.AddClassHandler<MavAvatar>((a, _) => a.ApplyTheme(MavTheme.Current));
    }

    /// <summary>Creates an avatar showing the initial "A".</summary>
    public MavAvatar() : this("A")
    {
    }

    /// <summary>Creates an avatar with initials or an icon, an optional fill color and a size.</summary>
    public MavAvatar(
        string? initials = "A",
        string? icon = null,
        double size = 40,
        MavAvatarStatus status = MavAvatarStatus.None,
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
        _icon = new MavIcon { IsVisible = false, UseThemeDefaultColor = false };
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

        _surface = new MavSurface
        {
            Width = size,
            Height = size,
            Radius = size / 2.0,
            ClipToBounds = true,
            Child = innerGrid,
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

        Child = outerGrid;
        Initials = initials;
        Icon = icon;
        ImageSource = imageSource;
        Size = size;
        Status = status;

        SyncContent();
        SyncSize();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public MavAvatarStatus Status
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
        _statusRing.IsVisible = Status != MavAvatarStatus.None;
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _label.FontFamily = t.Typography.PlainFont;
        var bg = CustomFillColor ?? c.PrimaryContainer;
        var fg = CustomTextColor ?? c.OnPrimaryContainer;
        _surface.FillColor = bg;
        _label.Foreground = MavTheme.BrushOf(fg);
        _icon.Color = fg;

        _statusRing.Background = MavTheme.BrushOf(c.Surface);
        var statusColor = Status switch
        {
            MavAvatarStatus.Online => Color.Parse("#2E7D32"),
            MavAvatarStatus.Busy => c.Error,
            MavAvatarStatus.Away => Color.Parse("#EF6C00"),
            MavAvatarStatus.Offline => c.Outline,
            _ => Colors.Transparent,
        };
        _statusDot.Background = MavTheme.BrushOf(statusColor);
    }
}
