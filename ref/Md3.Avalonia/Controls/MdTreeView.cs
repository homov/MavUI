using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public sealed class MdTreeNode : Control, IEnumerable<MdTreeNode>
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdTreeNode, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdTreeNode, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MdTreeNode, string?>(nameof(BadgeText));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MdTreeNode, bool>(nameof(IsExpanded), true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<MdTreeNode, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    internal event EventHandler? NodeChanged;

    static MdTreeNode()
    {
        TextProperty.Changed.AddClassHandler<MdTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MdTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MdTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        IsExpandedProperty.Changed.AddClassHandler<MdTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
        IsCheckedProperty.Changed.AddClassHandler<MdTreeNode>((n, _) => n.NodeChanged?.Invoke(n, EventArgs.Empty));
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
    public AvaloniaList<MdTreeNode> Children { get; } = new();

    public MdTreeNode()
    {
        Children.CollectionChanged += (_, _) =>
        {
            foreach (var c in Children)
                MdVisualTreeHelpers.AttachLogicalChild(this, c);
            NodeChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public MdTreeNode(
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

    public MdTreeNode Add(MdTreeNode child)
    {
        Children.Add(child);
        return this;
    }

    public MdTreeNode Add(
        string text,
        string? icon = null,
        string? badgeText = null,
        object? tag = null,
        bool isExpanded = true)
    {
        var child = new MdTreeNode(text, icon, badgeText, tag, isExpanded);
        Children.Add(child);
        return child;
    }

    public IEnumerator<MdTreeNode> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Material Design 3 hierarchical TreeView control with expandable folders, active selection pill,
/// optional cascading checkboxes, indent guide lines, badges, and C# collection initializer support.
/// </summary>
public class MdTreeView : MdContentView, IEnumerable<MdTreeNode>
{
    public static readonly StyledProperty<MdTreeNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<MdTreeView, MdTreeNode?>(nameof(SelectedNode), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowCheckBoxesProperty =
        AvaloniaProperty.Register<MdTreeView, bool>(nameof(ShowCheckBoxes));

    public static readonly StyledProperty<bool> CascadeCheckBoxesProperty =
        AvaloniaProperty.Register<MdTreeView, bool>(nameof(CascadeCheckBoxes), true);

    public static readonly StyledProperty<bool> ShowIndentGuidesProperty =
        AvaloniaProperty.Register<MdTreeView, bool>(nameof(ShowIndentGuides), true);

    public static readonly StyledProperty<string> FilterTextProperty =
        AvaloniaProperty.Register<MdTreeView, string>(nameof(FilterText), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdTreeView, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MdTreeView, IDataTemplate?>(nameof(ItemTemplate));

    private readonly AvaloniaList<MdTreeNode> _roots = new();
    private readonly MdCollectionObserver _observer;
    private readonly StackPanel _stack;

    public event EventHandler<MdTreeNode?>? SelectedNodeChanged;
    public event EventHandler<MdTreeNode>? NodeCheckedChanged;

    static MdTreeView()
    {
        SelectedNodeProperty.Changed.AddClassHandler<MdTreeView>((tv, e) =>
        {
            tv.Rebuild();
            tv.SelectedNodeChanged?.Invoke(tv, e.GetNewValue<MdTreeNode?>());
        });
        ShowCheckBoxesProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.Rebuild());
        ShowIndentGuidesProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.Rebuild());
        FilterTextProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.Rebuild());
        ItemsSourceProperty.Changed.AddClassHandler<MdTreeView>((tv, e) => tv.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.RebuildFromItemsSource(tv.ItemsSource));
    }

    public MdTreeView()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MdCollectionObserver(() => RebuildFromItemsSource(ItemsSource));
        _roots.CollectionChanged += (_, _) =>
        {
            foreach (var r in _roots)
                MdVisualTreeHelpers.AttachLogicalChild(this, r);
            Rebuild();
        };
        _stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
        };
        Content = _stack;

        MdTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public MdTreeNode? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public bool ShowCheckBoxes
    {
        get => GetValue(ShowCheckBoxesProperty);
        set => SetValue(ShowCheckBoxesProperty, value);
    }

    public bool CascadeCheckBoxes
    {
        get => GetValue(CascadeCheckBoxesProperty);
        set => SetValue(CascadeCheckBoxesProperty, value);
    }

    public bool ShowIndentGuides
    {
        get => GetValue(ShowIndentGuidesProperty);
        set => SetValue(ShowIndentGuidesProperty, value);
    }

    public string FilterText
    {
        get => GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _roots.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            if (item is MdTreeNode node)
            {
                _roots.Add(node);
            }
            else if (ItemTemplate?.Build(item) is MdTreeNode builtNode)
            {
                builtNode.DataContext = item;
                _roots.Add(builtNode);
            }
            else
            {
                _roots.Add(new MdTreeNode(item.ToString() ?? string.Empty, tag: item));
            }
        }
    }

    [Content]
    public AvaloniaList<MdTreeNode> Roots => _roots;

    public void Add(MdTreeNode node) => _roots.Add(node);

    public MdTreeNode Add(
        string text,
        string? icon = null,
        string? badgeText = null,
        object? tag = null,
        bool isExpanded = true)
    {
        var node = new MdTreeNode(text, icon, badgeText, tag, isExpanded);
        _roots.Add(node);
        Rebuild();
        return node;
    }

    public void ExpandAll()
    {
        SetExpandedRecursive(_roots, true);
        Rebuild();
    }

    public void CollapseAll()
    {
        SetExpandedRecursive(_roots, false);
        Rebuild();
    }

    public void Refresh() => Rebuild();

    public IReadOnlyList<MdTreeNode> GetCheckedNodes()
    {
        var list = new List<MdTreeNode>();
        CollectChecked(_roots, list);
        return list;
    }

    private static void CollectChecked(IEnumerable<MdTreeNode> nodes, List<MdTreeNode> acc)
    {
        foreach (var n in nodes)
        {
            if (n.IsChecked) acc.Add(n);
            if (n.Children.Count > 0) CollectChecked(n.Children, acc);
        }
    }

    private static void SetExpandedRecursive(IEnumerable<MdTreeNode> nodes, bool expanded)
    {
        foreach (var n in nodes)
        {
            n.IsExpanded = expanded;
            if (n.Children.Count > 0) SetExpandedRecursive(n.Children, expanded);
        }
    }

    private static void SetCheckedRecursive(MdTreeNode node, bool isChecked)
    {
        node.IsChecked = isChecked;
        foreach (var child in node.Children)
            SetCheckedRecursive(child, isChecked);
    }

    public IEnumerator<MdTreeNode> GetEnumerator() => _roots.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _stack.Children.Clear();
        if (_roots.Count == 0) return;

        var filter = FilterText?.Trim();
        var t = MdTheme.Current;
        foreach (var root in _roots)
            BuildNodeRecursive(root, 0, t, filter);
    }

    private static bool NodeMatchesFilter(MdTreeNode node, string? filter)
    {
        if (string.IsNullOrEmpty(filter)) return true;
        if (node.Text.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var child in node.Children)
        {
            if (NodeMatchesFilter(child, filter))
                return true;
        }
        return false;
    }

    private void BuildNodeRecursive(MdTreeNode node, int depth, MdTheme t, string? filter = null)
    {
        if (!NodeMatchesFilter(node, filter)) return;

        var c = t.Color;
        var hasChildren = node.Children.Count > 0;
        var isSelected = ReferenceEquals(SelectedNode, node);

        var rowGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,*,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
            MinHeight = 38,
        };

        // Depth indentation + optional guide lines
        if (depth > 0)
        {
            var indentPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            for (var d = 0; d < depth; d++)
            {
                var cell = new Grid { Width = 20 };
                if (ShowIndentGuides)
                {
                    cell.Children.Add(new Border
                    {
                        Width = 1,
                        Background = MdTheme.BrushOf(c.OutlineVariant.WithAlpha(0.55f)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    });
                }
                indentPanel.Children.Add(cell);
            }
            rowGrid.Add(indentPanel, 0, 0);
        }

        // Expand/collapse chevron
        if (hasChildren)
        {
            var chevron = new MdIcon(node.IsExpanded ? MdIcons.ExpandMore : MdIcons.ChevronRight, 18)
            {
                UseThemeDefaultColor = false,
                Color = isSelected ? c.OnSecondaryContainer : c.OnSurfaceVariant,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var chevronBtn = new MdInkSurface
            {
                Width = 28,
                Height = 28,
                CornerRadius = 14,
                FillColor = Colors.Transparent,
                RippleColor = c.OnSurface,
                VerticalAlignment = VerticalAlignment.Center,
                Content = chevron,
            };
            chevronBtn.Clicked += (_, _) =>
            {
                node.IsExpanded = !node.IsExpanded;
                Rebuild();
            };
            rowGrid.Add(chevronBtn, 1, 0);
        }
        else
        {
            rowGrid.Add(new Border { Width = 28 }, 1, 0);
        }

        // Optional checkbox + node icon
        var leadingStack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 8, 0),
        };

        if (ShowCheckBoxes)
        {
            var cb = new MdCheckBox(null, node.IsChecked)
            {
                VerticalAlignment = VerticalAlignment.Center,
            };
            cb.CheckedChanged += (_, val) =>
            {
                if (CascadeCheckBoxes)
                    SetCheckedRecursive(node, val);
                else
                    node.IsChecked = val;
                NodeCheckedChanged?.Invoke(this, node);
                Rebuild();
            };
            leadingStack.Children.Add(cb);
        }

        var iconGlyph = node.Icon ?? (hasChildren
            ? (node.IsExpanded ? MdIcons.FolderOpen : MdIcons.Folder)
            : MdIcons.Description);

        if (!string.IsNullOrEmpty(iconGlyph))
        {
            leadingStack.Children.Add(new MdIcon(iconGlyph, 18)
            {
                UseThemeDefaultColor = false,
                Color = isSelected ? c.Primary : c.OnSurfaceVariant,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
        rowGrid.Add(leadingStack, 2, 0);

        // Node label
        var textLbl = new TextBlock
        {
            Text = node.Text,
            Foreground = MdTheme.BrushOf(isSelected ? c.OnSecondaryContainer : c.OnSurface),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        textLbl.Apply(isSelected ? MdTypeRole.TitleSmall : MdTypeRole.BodyMedium, t.Typography);
        rowGrid.Add(textLbl, 3, 0);

        // Optional trailing badge
        if (!string.IsNullOrEmpty(node.BadgeText))
        {
            var badgeLbl = new TextBlock
            {
                Text = node.BadgeText,
                Foreground = MdTheme.BrushOf(isSelected ? c.OnPrimary : c.OnSurfaceVariant),
                VerticalAlignment = VerticalAlignment.Center,
            };
            badgeLbl.Apply(MdTypeRole.LabelSmall, t.Typography);

            var badgePill = new MdSurface
            {
                Radius = 10,
                Padding = new Thickness(7, 2),
                FillColor = isSelected ? c.Primary : c.SurfaceContainerHigh,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 4, 0),
                Content = badgeLbl,
            };
            rowGrid.Add(badgePill, 4, 0);
        }

        var pill = new MdInkSurface
        {
            CornerRadius = 12,
            Padding = new Thickness(6, 2),
            FillColor = isSelected ? c.SecondaryContainer : Colors.Transparent,
            RippleColor = c.Primary,
            Content = rowGrid,
        };
        pill.Clicked += (_, _) =>
        {
            SelectedNode = node;
            if (hasChildren)
            {
                node.IsExpanded = !node.IsExpanded;
                Rebuild();
            }
        };

        _stack.Children.Add(pill);

        if (hasChildren && (node.IsExpanded || !string.IsNullOrEmpty(filter)))
        {
            foreach (var child in node.Children)
                BuildNodeRecursive(child, depth + 1, t, filter);
        }
    }
}
