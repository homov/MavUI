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
public sealed class MavNavigationDestination : MavNavigationItemBase
{
    public static readonly StyledProperty<bool> ShowBadgeDotProperty =
        AvaloniaProperty.Register<MavNavigationDestination, bool>(nameof(ShowBadgeDot));

    static MavNavigationDestination()
    {
        ShowBadgeDotProperty.Changed.AddClassHandler<MavNavigationDestination>((d, e) =>
        {
            if (e.GetNewValue<bool>() && d.BadgeText is null)
                d.BadgeText = string.Empty;
            d.OnItemChanged();
        });
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

    /// <summary>
    /// Measures the destination during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
