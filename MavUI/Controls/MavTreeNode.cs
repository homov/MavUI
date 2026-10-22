using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>One node of a <see cref="MavTreeView"/>, with optional child nodes.</summary>
public sealed class MavTreeNode : Control, IEnumerable<MavTreeNode>
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavTreeNode, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavTreeNode, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavTreeNode, string?>(nameof(BadgeText));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MavTreeNode, bool>(nameof(IsExpanded), true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<MavTreeNode, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    internal event EventHandler? NodeChanged;
    internal event EventHandler<NotifyCollectionChangedEventArgs>? ChildrenChanged;

    static MavTreeNode()
    {
        TextProperty.Changed.AddClassHandler<MavTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        IsExpandedProperty.Changed.AddClassHandler<MavTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        IsCheckedProperty.Changed.AddClassHandler<MavTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
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

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    [Content]
    public AvaloniaList<MavTreeNode> Children { get; } = new();

    /// <summary>Creates an empty tree node.</summary>
    public MavTreeNode()
    {
        Children.CollectionChanged += (_, e) =>
        {
            foreach (var c in Children)
                MavVisualTreeHelpers.AttachLogicalChild(this, c);
            ChildrenChanged?.Invoke(this, e);
        };
    }

    /// <summary>Creates a tree node with a label, optional icon and value.</summary>
    public MavTreeNode(
        string text,
        string? icon = null,
        string? badgeText = null,
        object? tag = null,
        bool isExpanded = true) : this()
    {
        Text = text;
        Icon = icon;
        BadgeText = badgeText;
        Tag = tag;
        IsExpanded = isExpanded;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Adds a child node and returns this node for chaining.</summary>
    public MavTreeNode Add(MavTreeNode child)
    {
        Children.Add(child);
        return this;
    }

    /// <summary>Adds a child node with the given label and returns the new node.</summary>
    public MavTreeNode Add(
        string text,
        string? icon = null,
        string? badgeText = null,
        object? tag = null,
        bool isExpanded = true)
    {
        var child = new MavTreeNode(text, icon, badgeText, tag, isExpanded);
        Children.Add(child);
        return child;
    }

    public IEnumerator<MavTreeNode> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
