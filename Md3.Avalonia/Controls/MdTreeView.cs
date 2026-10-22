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
    internal event EventHandler<NotifyCollectionChangedEventArgs>? ChildrenChanged;

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
        Children.CollectionChanged += (_, e) =>
        {
            foreach (var c in Children)
                MdVisualTreeHelpers.AttachLogicalChild(this, c);
            ChildrenChanged?.Invoke(this, e);
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
    private readonly Dictionary<MdTreeNode, TreeRow> _rows =
        new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<MdTreeNode> _observedNodes =
        new(ReferenceEqualityComparer.Instance);
    private int _refreshDepth;
    private bool _refreshPending;
    private bool _suppressRootRebuild;

    private sealed class TreeRow
    {
        public required MdTreeNode Node { get; init; }
        public required MdInkSurface Pill { get; init; }
        public required int Depth { get; init; }
        public required bool Selected { get; init; }
        public required bool Expanded { get; init; }
        public required bool Checked { get; init; }
        public required bool ShowCheckBoxes { get; init; }
        public required bool ShowIndentGuides { get; init; }
        public required bool HasChildren { get; init; }
        public required string Text { get; init; }
        public required string? Icon { get; init; }
        public required string? BadgeText { get; init; }
    }

    public event EventHandler<MdTreeNode?>? SelectedNodeChanged;
    public event EventHandler<MdTreeNode>? NodeCheckedChanged;

    static MdTreeView()
    {
        SelectedNodeProperty.Changed.AddClassHandler<MdTreeView>((tv, e) =>
        {
            tv.OnSelectedNodeChanged(
                e.GetOldValue<MdTreeNode?>(),
                e.GetNewValue<MdTreeNode?>());
            tv.SelectedNodeChanged?.Invoke(tv, e.GetNewValue<MdTreeNode?>());
        });
        ShowCheckBoxesProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.Rebuild());
        ShowIndentGuidesProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.Rebuild());
        FilterTextProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.RefreshVisibleRows());
        ItemsSourceProperty.Changed.AddClassHandler<MdTreeView>((tv, e) => tv.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MdTreeView>((tv, _) => tv.RebuildFromItemsSource(tv.ItemsSource));
    }

    public MdTreeView()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _roots.CollectionChanged += (_, _) =>
        {
            foreach (var r in _roots)
                MdVisualTreeHelpers.AttachLogicalChild(this, r);

            if (_suppressRootRebuild)
                return;

            SyncNodeSubscriptions();
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

        _suppressRootRebuild = true;
        try
        {
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
        finally
        {
            _suppressRootRebuild = false;
        }

        SyncNodeSubscriptions();
        Rebuild();
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
        return node;
    }

    public void ExpandAll()
    {
        RunDeferredRefresh(() => SetExpandedRecursive(_roots, true));
    }

    public void CollapseAll()
    {
        RunDeferredRefresh(() => SetExpandedRecursive(_roots, false));
    }

    public void Refresh() => RefreshVisibleRows();

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
        _rows.Clear();
        SyncNodeSubscriptions();
        RefreshVisibleRows();
    }

    private void RunDeferredRefresh(Action action)
    {
        _refreshDepth++;
        try
        {
            action();
        }
        finally
        {
            _refreshDepth--;
            if (_refreshDepth == 0 && _refreshPending)
            {
                _refreshPending = false;
                RefreshVisibleRows();
            }
        }
    }

    private void RequestTreeRefresh()
    {
        if (_refreshDepth > 0)
        {
            _refreshPending = true;
            return;
        }

        RefreshVisibleRows();
    }

    private void OnSelectedNodeChanged(MdTreeNode? oldNode, MdTreeNode? newNode)
    {
        if (oldNode is not null)
            _rows.Remove(oldNode);
        if (newNode is not null)
            _rows.Remove(newNode);

        RequestTreeRefresh();
    }

    private void OnNodeChanged(object? sender, EventArgs e)
    {
        if (sender is not MdTreeNode node)
            return;

        _rows.Remove(node);
        RequestTreeRefresh();
    }

    private void OnNodeChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not MdTreeNode node)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } added:
                foreach (var child in added)
                {
                    if (child is MdTreeNode addedNode)
                        SubscribeSubtree(addedNode);
                }
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is { } removed:
                foreach (var child in removed)
                {
                    if (child is MdTreeNode removedNode)
                        UnsubscribeSubtree(removedNode);
                }
                break;

            case NotifyCollectionChangedAction.Replace
                when e.OldItems is { } oldItems && e.NewItems is { } newItems:
                foreach (var child in oldItems)
                {
                    if (child is MdTreeNode removedNode)
                        UnsubscribeSubtree(removedNode);
                }
                foreach (var child in newItems)
                {
                    if (child is MdTreeNode addedNode)
                        SubscribeSubtree(addedNode);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                SyncNodeSubscriptions();
                break;
        }

        _rows.Remove(node);
        RequestTreeRefresh();
    }

    private void SyncNodeSubscriptions()
    {
        var current = new HashSet<MdTreeNode>(ReferenceEqualityComparer.Instance);
        foreach (var root in _roots)
            CollectNodes(root, current);

        foreach (var node in _observedNodes.ToArray())
        {
            if (!current.Contains(node))
                UnsubscribeSubtree(node);
        }

        foreach (var node in current)
        {
            if (!_observedNodes.Contains(node))
                SubscribeSubtree(node);
        }
    }

    private static void CollectNodes(MdTreeNode node, HashSet<MdTreeNode> nodes)
    {
        if (!nodes.Add(node))
            return;

        foreach (var child in node.Children)
            CollectNodes(child, nodes);
    }

    private void SubscribeSubtree(MdTreeNode node)
    {
        if (!_observedNodes.Add(node))
            return;

        node.NodeChanged += OnNodeChanged;
        node.ChildrenChanged += OnNodeChildrenChanged;
        foreach (var child in node.Children)
            SubscribeSubtree(child);
    }

    private void UnsubscribeSubtree(MdTreeNode node)
    {
        if (!_observedNodes.Remove(node))
            return;

        node.NodeChanged -= OnNodeChanged;
        node.ChildrenChanged -= OnNodeChildrenChanged;
        _rows.Remove(node);
        foreach (var child in node.Children)
            UnsubscribeSubtree(child);
    }

    private void RefreshVisibleRows()
    {
        if (_refreshDepth > 0)
        {
            _refreshPending = true;
            return;
        }

        var filter = FilterText?.Trim();
        var visible = GetVisibleNodes(string.IsNullOrEmpty(filter) ? null : filter);
        var t = MdTheme.Current;
        var desired = new List<Control>(visible.Count);

        foreach (var (node, depth) in visible)
        {
            if (!_rows.TryGetValue(node, out var row) || !RowStillMatches(row, node, depth))
            {
                row = CreateTreeRow(node, depth, t);
                _rows[node] = row;
            }

            desired.Add(row.Pill);
        }

        SyncStack(desired);
    }

    private List<(MdTreeNode Node, int Depth)> GetVisibleNodes(string? filter)
    {
        var result = new List<(MdTreeNode, int)>();
        if (_roots.Count == 0)
            return result;

        Dictionary<MdTreeNode, bool>? matches = null;
        if (filter is not null)
        {
            matches = new Dictionary<MdTreeNode, bool>(ReferenceEqualityComparer.Instance);
            foreach (var root in _roots)
                MatchesFilter(root, filter, matches);
        }

        void Visit(MdTreeNode node, int depth)
        {
            if (filter is not null && !MatchesFilter(node, filter, matches!))
                return;

            result.Add((node, depth));
            if (filter is not null || node.IsExpanded)
            {
                foreach (var child in node.Children)
                    Visit(child, depth + 1);
            }
        }

        foreach (var root in _roots)
            Visit(root, 0);

        return result;
    }

    private static bool MatchesFilter(
        MdTreeNode node,
        string filter,
        Dictionary<MdTreeNode, bool> memo)
    {
        if (memo.TryGetValue(node, out var known))
            return known;

        var matches = node.Text.Contains(filter, StringComparison.OrdinalIgnoreCase);
        if (!matches)
        {
            foreach (var child in node.Children)
            {
                if (MatchesFilter(child, filter, memo))
                {
                    matches = true;
                    break;
                }
            }
        }

        memo[node] = matches;
        return matches;
    }

    private bool RowStillMatches(TreeRow row, MdTreeNode node, int depth) =>
        row.Depth == depth
        && row.Node == node
        && row.Selected == ReferenceEquals(SelectedNode, node)
        && row.Expanded == node.IsExpanded
        && row.Checked == node.IsChecked
        && row.ShowCheckBoxes == ShowCheckBoxes
        && row.ShowIndentGuides == ShowIndentGuides
        && row.HasChildren == (node.Children.Count > 0)
        && row.Text == node.Text
        && row.Icon == node.Icon
        && row.BadgeText == node.BadgeText;

    private void SyncStack(List<Control> desired)
    {
        var common = 0;
        var commonLimit = Math.Min(_stack.Children.Count, desired.Count);
        while (common < commonLimit
               && ReferenceEquals(_stack.Children[common], desired[common]))
        {
            common++;
        }

        if (common == _stack.Children.Count && common == desired.Count)
            return;

        for (var i = _stack.Children.Count - 1; i >= common; i--)
            _stack.Children.RemoveAt(i);

        for (var i = common; i < desired.Count; i++)
            _stack.Children.Add(desired[i]);
    }

    private TreeRow CreateTreeRow(MdTreeNode node, int depth, MdTheme t)
    {
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
                RunDeferredRefresh(() =>
                {
                    if (CascadeCheckBoxes)
                        SetCheckedRecursive(node, val);
                    else
                        node.IsChecked = val;
                });
                NodeCheckedChanged?.Invoke(this, node);
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
            RunDeferredRefresh(() =>
            {
                SelectedNode = node;
                if (hasChildren)
                    node.IsExpanded = !node.IsExpanded;
            });
        };

        return new TreeRow
        {
            Node = node,
            Pill = pill,
            Depth = depth,
            Selected = isSelected,
            Expanded = node.IsExpanded,
            Checked = node.IsChecked,
            ShowCheckBoxes = ShowCheckBoxes,
            ShowIndentGuides = ShowIndentGuides,
            HasChildren = hasChildren,
            Text = node.Text,
            Icon = node.Icon,
            BadgeText = node.BadgeText,
        };
    }
}
