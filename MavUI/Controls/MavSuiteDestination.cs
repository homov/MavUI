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
public sealed class MavSuiteDestination : MavNavigationItemBase
{
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MavSuiteDestination, Control?>(nameof(Content));

    static MavSuiteDestination()
    {
        ContentProperty.Changed.AddClassHandler<MavSuiteDestination>((d, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(d, e.GetNewValue<Control?>());
            d.OnItemChanged();
        });
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
