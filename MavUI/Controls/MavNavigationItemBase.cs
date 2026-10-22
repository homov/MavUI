using Avalonia;
using Avalonia.Controls;

namespace Mav.Controls;

/// <summary>
/// Common data of a navigation entry: label, icon, selected icon and badge.
/// Base of <see cref="MavNavigationDestination"/>, <see cref="MavSuiteDestination"/>
/// and <see cref="MavDrawerItem"/>.
/// </summary>
public abstract class MavNavigationItemBase : Control
{
    /// <summary>Identifies the <see cref="Label"/> property.</summary>
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavNavigationItemBase, string>(nameof(Label), string.Empty);

    /// <summary>Identifies the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavNavigationItemBase, string>(nameof(Icon), MavIcons.Home);

    /// <summary>Identifies the <see cref="SelectedIcon"/> property.</summary>
    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavNavigationItemBase, string?>(nameof(SelectedIcon));

    /// <summary>Identifies the <see cref="BadgeText"/> property.</summary>
    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavNavigationItemBase, string?>(nameof(BadgeText));

    /// <summary>Raised when the item changes so that its container can refresh.</summary>
    internal event EventHandler? ItemChanged;

    static MavNavigationItemBase()
    {
        LabelProperty.Changed.AddClassHandler<MavNavigationItemBase>((i, _) => i.OnItemChanged());
        IconProperty.Changed.AddClassHandler<MavNavigationItemBase>((i, _) => i.OnItemChanged());
        SelectedIconProperty.Changed.AddClassHandler<MavNavigationItemBase>((i, _) => i.OnItemChanged());
        BadgeTextProperty.Changed.AddClassHandler<MavNavigationItemBase>((i, _) => i.OnItemChanged());
    }

    /// <summary>Gets or sets the text shown with the item.</summary>
    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Gets or sets the icon glyph shown when the item is not selected.</summary>
    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the icon glyph shown when the item is selected. Falls back to <see cref="Icon"/> when null.</summary>
    public string? SelectedIcon
    {
        get => GetValue(SelectedIconProperty);
        set => SetValue(SelectedIconProperty, value);
    }

    /// <summary>Gets or sets the badge text, or null when there is no badge.</summary>
    public string? BadgeText
    {
        get => GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    /// <summary>Raises <see cref="ItemChanged"/>. Derived types call it when their own state changes.</summary>
    protected void OnItemChanged() => ItemChanged?.Invoke(this, EventArgs.Empty);
}
