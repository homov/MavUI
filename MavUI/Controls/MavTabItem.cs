using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>One tab of a <see cref="MavTabs"/> control.</summary>
public sealed class MavTabItem : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavTabItem, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavTabItem, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavTabItem, string?>(nameof(BadgeText));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<MavTabItem, object?>(nameof(Content));

    internal event EventHandler? ItemChanged;

    static MavTabItem()
    {
        TextProperty.Changed.AddClassHandler<MavTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        ContentProperty.Changed.AddClassHandler<MavTabItem>((i, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<object?>() as Control);
            i.ItemChanged?.Invoke(i, EventArgs.Empty);
        });
    }

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

    public string? BadgeText
    {
        get => GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    /// <summary>The item content. Any object is accepted; the host shows it through a ContentPresenter when it is not a control.</summary>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Creates an empty tab item.</summary>
    public MavTabItem()
    {
    }

    /// <summary>Creates a tab item with text, optional icon, badge and content.</summary>
    public MavTabItem(
        string text,
        string? icon = null,
        string? badgeText = null,
        object? content = null)
    {
        Text = text;
        Icon = icon;
        BadgeText = badgeText;
        Content = content;
    }

    /// <summary>
    /// Measures the tab item during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
