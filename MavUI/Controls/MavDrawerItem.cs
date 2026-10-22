using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Entry of a <see cref="MavNavigationDrawer"/>: a destination, section header or divider.</summary>
public sealed class MavDrawerItem : MavNavigationItemBase
{
    public static readonly StyledProperty<string?> SectionHeaderProperty =
        AvaloniaProperty.Register<MavDrawerItem, string?>(nameof(SectionHeader));

    public static readonly StyledProperty<bool> ShowDividerBeforeProperty =
        AvaloniaProperty.Register<MavDrawerItem, bool>(nameof(ShowDividerBefore));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavDrawerItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavDrawerItem, object?>(nameof(CommandParameter));

    static MavDrawerItem()
    {
        SectionHeaderProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.OnItemChanged());
        ShowDividerBeforeProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.OnItemChanged());
    }

    public string? SectionHeader
    {
        get => GetValue(SectionHeaderProperty);
        set => SetValue(SectionHeaderProperty, value);
    }

    public bool ShowDividerBefore
    {
        get => GetValue(ShowDividerBeforeProperty);
        set => SetValue(ShowDividerBeforeProperty, value);
    }

    public Action? OnClick { get; set; }
    public event EventHandler? Clicked;

    /// <summary>
    /// Raises the Clicked event for this item.
    /// </summary>
    internal void RaiseClicked() => Clicked?.Invoke(this, EventArgs.Empty);

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Creates an empty drawer entry.</summary>
    public MavDrawerItem()
    {
        Icon = MavIcons.Folder;
    }

    /// <summary>Creates a drawer entry with label, icon, optional badge and click handler.</summary>
    public MavDrawerItem(
        string label,
        string icon,
        string? badgeText = null,
        Action? onClick = null)
    {
        Label = label;
        Icon = icon;
        BadgeText = badgeText;
        OnClick = onClick;
    }

    /// <summary>
    /// Measures the item during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
