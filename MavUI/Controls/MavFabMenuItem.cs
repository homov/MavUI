using System.Collections;
using System.Windows.Input;
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

/// <summary>One action inside a <see cref="MavFabMenu"/>.</summary>
public sealed class MavFabMenuItem : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavFabMenuItem, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavFabMenuItem, string>(nameof(Icon), MavIcons.Add);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavFabMenuItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavFabMenuItem, object?>(nameof(CommandParameter));

    internal event EventHandler? ItemChanged;

    static MavFabMenuItem()
    {
        LabelProperty.Changed.AddClassHandler<MavFabMenuItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavFabMenuItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
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

    /// <summary>Creates an empty menu item.</summary>
    public MavFabMenuItem()
    {
    }

    /// <summary>Creates a menu item with a label, icon and click handler.</summary>
    public MavFabMenuItem(string label, string icon, Action? onClick = null)
    {
        Label = label;
        Icon = icon;
        OnClick = onClick;
    }

    /// <summary>
    /// Measures the item during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
