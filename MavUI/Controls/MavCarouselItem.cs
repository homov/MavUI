using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>One card in a <see cref="MavCarousel"/>: title, optional subtitle, icon and accent color.</summary>
public sealed class MavCarouselItem : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavCarouselItem, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavCarouselItem, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavCarouselItem, string?>(nameof(Icon));

    public static readonly StyledProperty<Color?> AccentColorProperty =
        AvaloniaProperty.Register<MavCarouselItem, Color?>(nameof(AccentColor));

    internal event EventHandler? ItemChanged;

    static MavCarouselItem()
    {
        TitleProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        SubtitleProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        AccentColorProperty.Changed.AddClassHandler<MavCarouselItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Color? AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    /// <summary>Creates an empty carousel item.</summary>
    public MavCarouselItem()
    {
    }

    /// <summary>Creates a carousel item with a title, subtitle, icon and accent color.</summary>
    public MavCarouselItem(
        string title,
        string? subtitle = null,
        string? icon = null,
        Color? accentColor = null)
    {
        Title = title;
        Subtitle = subtitle;
        Icon = icon;
        AccentColor = accentColor;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}
