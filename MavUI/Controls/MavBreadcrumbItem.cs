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

/// <summary>One item of a <see cref="MavBreadcrumbBar"/>.</summary>
public sealed class MavBreadcrumbItem : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavBreadcrumbItem, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavBreadcrumbItem, string?>(nameof(Icon));

    internal event EventHandler? ItemChanged;

    static MavBreadcrumbItem()
    {
        TextProperty.Changed.AddClassHandler<MavBreadcrumbItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavBreadcrumbItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Creates an empty breadcrumb item.</summary>
    public MavBreadcrumbItem()
    {
    }

    /// <summary>Creates a breadcrumb item with a label, optional icon and an opaque tag for the caller.</summary>
    public MavBreadcrumbItem(string text, string? icon = null, object? tag = null)
    {
        Text = text;
        Icon = icon;
        Tag = tag;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}
