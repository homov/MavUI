using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Destination of a <see cref="MavNavigationBar"/> or <see cref="MavNavigationRail"/>.</summary>
public sealed class MavNavigationDestination : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string>(nameof(Icon), MavIcons.Home);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string?>(nameof(BadgeText));

    public static readonly StyledProperty<bool> ShowBadgeDotProperty =
        AvaloniaProperty.Register<MavNavigationDestination, bool>(nameof(ShowBadgeDot));

    internal event EventHandler? DestinationChanged;

    static MavNavigationDestination()
    {
        LabelProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        SelectedIconProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        ShowBadgeDotProperty.Changed.AddClassHandler<MavNavigationDestination>((d, e) =>
        {
            if (e.GetNewValue<bool>() && d.BadgeText is null)
                d.BadgeText = string.Empty;
            d.DestinationChanged?.Invoke(d, EventArgs.Empty);
        });
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? SelectedIcon
    {
        get => GetValue(SelectedIconProperty);
        set => SetValue(SelectedIconProperty, value);
    }

    public string? BadgeText
    {
        get => GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public bool ShowBadgeDot
    {
        get => GetValue(ShowBadgeDotProperty);
        set => SetValue(ShowBadgeDotProperty, value);
    }

    /// <summary>Creates an empty navigation destination.</summary>
    public MavNavigationDestination()
    {
    }

    /// <summary>Creates a destination with label, icon, selected icon and optional badge.</summary>
    public MavNavigationDestination(
        string label,
        string icon,
        string? selectedIcon = null,
        string? badgeText = null)
    {
        Label = label;
        Icon = icon;
        SelectedIcon = selectedIcon;
        BadgeText = badgeText;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}
