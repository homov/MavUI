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
public sealed class MavDrawerItem : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavDrawerItem, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavDrawerItem, string>(nameof(Icon), MavIcons.Folder);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavDrawerItem, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavDrawerItem, string?>(nameof(BadgeText));

    public static readonly StyledProperty<string?> SectionHeaderProperty =
        AvaloniaProperty.Register<MavDrawerItem, string?>(nameof(SectionHeader));

    public static readonly StyledProperty<bool> ShowDividerBeforeProperty =
        AvaloniaProperty.Register<MavDrawerItem, bool>(nameof(ShowDividerBefore));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavDrawerItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavDrawerItem, object?>(nameof(CommandParameter));

    internal event EventHandler? ItemChanged;

    static MavDrawerItem()
    {
        LabelProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        SectionHeaderProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        ShowDividerBeforeProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
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

    protected override Size MeasureOverride(Size availableSize) => default;
}
