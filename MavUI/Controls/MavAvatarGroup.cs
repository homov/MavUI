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

    /// <summary>Creates an avatar group that shows up to four avatars.</summary>
    public MavAvatarGroup() : this(4, 38.0)
    {
    }

    /// <summary>Creates an avatar group that shows up to <paramref name="maxDisplayed"/> avatars and a "+N" overflow badge.</summary>
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

    /// <summary>Adds an avatar built from initials or an icon and returns the group for chaining.</summary>
    public MavAvatarGroup AddAvatar(string initials, string? icon = null, Color? fillColor = null, Color? textColor = null)
    {
        var avatar = new MavAvatar(initials, icon, AvatarSize);
        if (fillColor is not null) avatar.CustomFillColor = fillColor;
        if (textColor is not null) avatar.CustomTextColor = textColor;
        _avatars.Add(avatar);
        return this;
    }

    /// <summary>Adds an existing avatar to the group and returns the group for chaining.</summary>
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
