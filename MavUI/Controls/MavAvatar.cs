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

public enum MavAvatarStatus
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

    public MavAvatar() : this("A")
    {
    }

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

/// <summary>
/// Overlapping stack of <see cref="MavAvatar"/> items with automatic <c>+N</c> overflow badge.
/// </summary>
public class MavAvatarGroup : MavContentView, IEnumerable<MavAvatar>
{
    public static readonly StyledProperty<int> MaxDisplayedProperty =
        AvaloniaProperty.Register<MavAvatarGroup, int>(nameof(MaxDisplayed), 4);

    public static readonly StyledProperty<double> AvatarSizeProperty =
        AvaloniaProperty.Register<MavAvatarGroup, double>(nameof(AvatarSize), 38.0);

    public static readonly StyledProperty<double> OverlapRatioProperty =
        AvaloniaProperty.Register<MavAvatarGroup, double>(nameof(OverlapRatio), 0.28);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavAvatarGroup, IEnumerable?>(nameof(ItemsSource));

    private readonly AvaloniaList<MavAvatar> _avatars = new();
    private readonly MavCollectionObserver _observer;
    private readonly Canvas _canvas;

    static MavAvatarGroup()
    {
        MaxDisplayedProperty.Changed.AddClassHandler<MavAvatarGroup>((g, _) => g.Rebuild());
        AvatarSizeProperty.Changed.AddClassHandler<MavAvatarGroup>((g, _) => g.Rebuild());
        OverlapRatioProperty.Changed.AddClassHandler<MavAvatarGroup>((g, _) => g.Rebuild());
        ItemsSourceProperty.Changed.AddClassHandler<MavAvatarGroup>((g, e) => g.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    public MavAvatarGroup() : this(4, 38.0)
    {
    }

    public MavAvatarGroup(int maxDisplayed = 4, double avatarSize = 38.0)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _avatars.CollectionChanged += (_, _) =>
        {
            foreach (var av in _avatars)
                MavVisualTreeHelpers.AttachLogicalChild(this, av);
            Rebuild();
        };
        _canvas = new Canvas();
        Content = _canvas;
        MaxDisplayed = maxDisplayed;
        AvatarSize = avatarSize;
        MavTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public int MaxDisplayed
    {
        get => GetValue(MaxDisplayedProperty);
        set => SetValue(MaxDisplayedProperty, Math.Max(1, value));
    }

    public double AvatarSize
    {
        get => GetValue(AvatarSizeProperty);
        set => SetValue(AvatarSizeProperty, Math.Max(24, value));
    }

    public double OverlapRatio
    {
        get => GetValue(OverlapRatioProperty);
        set => SetValue(OverlapRatioProperty, Math.Clamp(value, 0.0, 0.65));
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _avatars.Clear();
        foreach (var item in src)
        {
            if (item is MavAvatar av)
                _avatars.Add(av);
            else if (item is not null)
                AddAvatar(item.ToString() ?? string.Empty);
        }
    }

    [Content]
    public AvaloniaList<MavAvatar> Avatars => _avatars;

    public MavAvatarGroup AddAvatar(string initials, string? icon = null, Color? fillColor = null, Color? textColor = null)
    {
        var avatar = new MavAvatar(initials, icon, AvatarSize);
        if (fillColor is not null) avatar.CustomFillColor = fillColor;
        if (textColor is not null) avatar.CustomTextColor = textColor;
        _avatars.Add(avatar);
        return this;
    }

    public MavAvatarGroup AddAvatar(MavAvatar avatar)
    {
        _avatars.Add(avatar);
        return this;
    }

    public void Add(MavAvatar avatar) => AddAvatar(avatar);

    public void Add(string initials) => AddAvatar(initials);

    public IEnumerator<MavAvatar> GetEnumerator() => _avatars.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _canvas.Children.Clear();
        if (_avatars.Count == 0)
        {
            _canvas.Width = 0;
            _canvas.Height = AvatarSize;
            return;
        }

        var c = MavTheme.Current.Color;
        Color[] paletteBg =
        [
            c.PrimaryContainer,
            c.SecondaryContainer,
            c.TertiaryContainer,
            c.InversePrimary,
        ];
        Color[] paletteFg =
        [
            c.OnPrimaryContainer,
            c.OnSecondaryContainer,
            c.OnTertiaryContainer,
            c.OnSurface,
        ];

        var size = AvatarSize;
        var ringPad = 2.0;
        var outerSize = size + ringPad * 2;
        var step = size * (1.0 - OverlapRatio);
        var max = Math.Max(1, MaxDisplayed);
        var visibleCount = Math.Min(_avatars.Count, max);
        var overflow = _avatars.Count - visibleCount;

        for (int i = 0; i < visibleCount; i++)
        {
            var av = _avatars[i];
            MavVisualTreeHelpers.DetachFromParent(av);
            av.Size = size;
            if (av.CustomFillColor is null)
            {
                av.CustomFillColor = paletteBg[i % paletteBg.Length];
                av.CustomTextColor = paletteFg[i % paletteFg.Length];
            }

            var ring = new Border
            {
                Width = outerSize,
                Height = outerSize,
                CornerRadius = new CornerRadius(outerSize / 2.0),
                Background = MavTheme.BrushOf(c.Surface),
                Padding = new Thickness(ringPad),
                Child = av,
            };
            Canvas.SetLeft(ring, i * step);
            Canvas.SetTop(ring, 0);
            _canvas.Children.Add(ring);
        }

        int totalSlots = visibleCount;
        if (overflow > 0)
        {
            var badgeAvatar = new MavAvatar($"+{overflow}", size: size)
            {
                CustomFillColor = c.SurfaceContainerHighest,
                CustomTextColor = c.OnSurfaceVariant,
            };
            var ring = new Border
            {
                Width = outerSize,
                Height = outerSize,
                CornerRadius = new CornerRadius(outerSize / 2.0),
                Background = MavTheme.BrushOf(c.Surface),
                Padding = new Thickness(ringPad),
                Child = badgeAvatar,
            };
            Canvas.SetLeft(ring, visibleCount * step);
            Canvas.SetTop(ring, 0);
            _canvas.Children.Add(ring);
            totalSlots++;
        }

        _canvas.Width = totalSlots > 0 ? (totalSlots - 1) * step + outerSize : 0;
        _canvas.Height = outerSize;
    }
}
