using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Destination shown by a <see cref="MavNavigationSuiteScaffold"/>.</summary>
public sealed class MavSuiteDestination : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string>(nameof(Icon), MavIcons.Home);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string?>(nameof(BadgeText));

    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MavSuiteDestination, Control?>(nameof(Content));

    internal event EventHandler? DestinationChanged;

    static MavSuiteDestination()
    {
        LabelProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        SelectedIconProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        ContentProperty.Changed.AddClassHandler<MavSuiteDestination>((d, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(d, e.GetNewValue<Control?>());
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

    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Creates an empty destination.</summary>
    public MavSuiteDestination()
    {
    }

    /// <summary>Creates a destination with label, icon, and the content it shows.</summary>
    public MavSuiteDestination(
        string label,
        string icon,
        string? selectedIcon = null,
        string? badgeText = null,
        Control? content = null)
    {
        Label = label;
        Icon = icon;
        SelectedIcon = selectedIcon;
        BadgeText = badgeText;
        Content = content;
    }

    /// <summary>
    /// Measures the destination during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
