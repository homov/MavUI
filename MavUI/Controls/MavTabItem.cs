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

    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MavTabItem, Control?>(nameof(Content));

    internal event EventHandler? ItemChanged;

    static MavTabItem()
    {
        TextProperty.Changed.AddClassHandler<MavTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        ContentProperty.Changed.AddClassHandler<MavTabItem>((i, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<Control?>());
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

    [Content]
    public Control? Content
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
        Control? content = null)
    {
        Text = text;
        Icon = icon;
        BadgeText = badgeText;
        Content = content;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}
