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

/// <summary>
/// Material Design 3 hierarchical TreeView control with expandable folders, active selection pill,
/// optional cascading checkboxes, indent guide lines, badges, and C# collection initializer support.
/// </summary>
public class MavTreeView : MavContentView, IEnumerable<MavTreeNode>
{
    public static readonly StyledProperty<MavTreeNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<MavTreeView, MavTreeNode?>(nameof(SelectedNode), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowCheckBoxesProperty =
        AvaloniaProperty.Register<MavTreeView, bool>(nameof(ShowCheckBoxes));

    public static readonly StyledProperty<bool> CascadeCheckBoxesProperty =
        AvaloniaProperty.Register<MavTreeView, bool>(nameof(CascadeCheckBoxes), true);

    public static readonly StyledProperty<bool> ShowIndentGuidesProperty =
        AvaloniaProperty.Register<MavTreeView, bool>(nameof(ShowIndentGuides), true);

    public static readonly StyledProperty<string> FilterTextProperty =
        AvaloniaProperty.Register<MavTreeView, string>(nameof(FilterText), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavTreeView, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MavTreeView, IDataTemplate?>(nameof(ItemTemplate));

    private readonly AvaloniaList<MavTreeNode> _roots = new();
    private readonly MavCollectionObserver _observer;
    private readonly StackPanel _stack;
    private readonly Dictionary<MavTreeNode, TreeRow> _rows =
        new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<MavTreeNode> _observedNodes =
        new(ReferenceEqualityComparer.Instance);
    private int _refreshDepth;
    private bool _refreshPending;
    private bool _suppressRootRebuild;

    private sealed class TreeRow
    {
        public required MavTreeNode Node { get; init; }
        public required MavInkSurface Pill { get; init; }
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

    public event EventHandler<MavTreeNode?>? SelectedNodeChanged;
    public event EventHandler<MavTreeNode>? NodeCheckedChanged;

    static MavTreeView()
    {
        SelectedNodeProperty.Changed.AddClassHandler<MavTreeView>((tv, e) =>
        {
            tv.OnSelectedNodeChanged(
                e.GetOldValue<MavTreeNode?>(),
                e.GetNewValue<MavTreeNode?>());
            tv.SelectedNodeChanged?.Invoke(tv, e.GetNewValue<MavTreeNode?>());
        });
        ShowCheckBoxesProperty.Changed.AddClassHandler<MavTreeView>((tv, _) => tv.Rebuild());
        ShowIndentGuidesProperty.Changed.AddClassHandler<MavTreeView>((tv, _) => tv.Rebuild());
        FilterTextProperty.Changed.AddClassHandler<MavTreeView>((tv, _) => tv.RefreshVisibleRows());
        ItemsSourceProperty.Changed.AddClassHandler<MavTreeView>((tv, e) => tv.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MavTreeView>((tv, _) => tv.RebuildFromItemsSource(tv.ItemsSource));
    }

    /// <summary>Creates an empty tree view.</summary>
    public MavTreeView()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _roots.CollectionChanged += (_, _) =>
        {
            foreach (var r in _roots)
                MavVisualTreeHelpers.AttachLogicalChild(this, r);

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

        MavTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public MavTreeNode? SelectedNode
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
                if (item is MavTreeNode node)
                {
                    _roots.Add(node);
                }
                else if (ItemTemplate?.Build(item) is MavTreeNode builtNode)
                {
                    builtNode.DataContext = item;
                    _roots.Add(builtNode);
                }
                else
                {
                    _roots.Add(new MavTreeNode(item.ToString() ?? string.Empty, tag: item));
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
    public AvaloniaList<MavTreeNode> Roots => _roots;

    /// <summary>
    /// Adds a root node to the tree.
    /// </summary>
    public void Add(MavTreeNode node) => _roots.Add(node);

    /// <summary>Adds a root node and returns the tree view for chaining.</summary>
    public MavTreeNode Add(
        string text,
        string? icon = null,
        string? badgeText = null,
        object? tag = null,
        bool isExpanded = true)
    {
        var node = new MavTreeNode(text, icon, badgeText, tag, isExpanded);
        _roots.Add(node);
        return node;
    }

    /// <summary>Expands every node in the tree.</summary>
    public void ExpandAll()
    {
        RunDeferredRefresh(() => SetExpandedRecursive(_roots, true));
    }

    /// <summary>Collapses every node in the tree.</summary>
    public void CollapseAll()
    {
        RunDeferredRefresh(() => SetExpandedRecursive(_roots, false));
    }

    /// <summary>
    /// Recalculates the visible rows after the tree changes.
    /// </summary>
    public void Refresh() => RefreshVisibleRows();

    /// <summary>Returns the nodes whose check box is checked.</summary>
    public IReadOnlyList<MavTreeNode> GetCheckedNodes()
    {
        var list = new List<MavTreeNode>();
        CollectChecked(_roots, list);
        return list;
    }

    private static void CollectChecked(IEnumerable<MavTreeNode> nodes, List<MavTreeNode> acc)
    {
        foreach (var n in nodes)
        {
            if (n.IsChecked) acc.Add(n);
            if (n.Children.Count > 0) CollectChecked(n.Children, acc);
        }
    }

    private static void SetExpandedRecursive(IEnumerable<MavTreeNode> nodes, bool expanded)
    {
        foreach (var n in nodes)
        {
            n.IsExpanded = expanded;
            if (n.Children.Count > 0) SetExpandedRecursive(n.Children, expanded);
        }
    }

    private static void SetCheckedRecursive(MavTreeNode node, bool isChecked)
    {
        node.IsChecked = isChecked;
        foreach (var child in node.Children)
            SetCheckedRecursive(child, isChecked);
    }

    /// <summary>
    /// Returns an enumerator over the root nodes.
    /// </summary>
    public IEnumerator<MavTreeNode> GetEnumerator() => _roots.GetEnumerator();
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

    private void OnSelectedNodeChanged(MavTreeNode? oldNode, MavTreeNode? newNode)
    {
        if (oldNode is not null)
            _rows.Remove(oldNode);
        if (newNode is not null)
            _rows.Remove(newNode);

        RequestTreeRefresh();
    }

    private void OnNodeChanged(object? sender, EventArgs e)
    {
        if (sender is not MavTreeNode node)
            return;

        _rows.Remove(node);
        RequestTreeRefresh();
    }

    private void OnNodeChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not MavTreeNode node)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } added:
                foreach (var child in added)
                {
                    if (child is MavTreeNode addedNode)
                        SubscribeSubtree(addedNode);
                }
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is { } removed:
                foreach (var child in removed)
                {
                    if (child is MavTreeNode removedNode)
                        UnsubscribeSubtree(removedNode);
                }
                break;

            case NotifyCollectionChangedAction.Replace
                when e.OldItems is { } oldItems && e.NewItems is { } newItems:
                foreach (var child in oldItems)
                {
                    if (child is MavTreeNode removedNode)
                        UnsubscribeSubtree(removedNode);
                }
                foreach (var child in newItems)
                {
                    if (child is MavTreeNode addedNode)
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
        var current = new HashSet<MavTreeNode>(ReferenceEqualityComparer.Instance);
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

    private static void CollectNodes(MavTreeNode node, HashSet<MavTreeNode> nodes)
    {
        if (!nodes.Add(node))
            return;

        foreach (var child in node.Children)
            CollectNodes(child, nodes);
    }

    private void SubscribeSubtree(MavTreeNode node)
    {
        if (!_observedNodes.Add(node))
            return;

        node.NodeChanged += OnNodeChanged;
        node.ChildrenChanged += OnNodeChildrenChanged;
        foreach (var child in node.Children)
            SubscribeSubtree(child);
    }

    private void UnsubscribeSubtree(MavTreeNode node)
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
        var t = MavTheme.Current;
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

    private List<(MavTreeNode Node, int Depth)> GetVisibleNodes(string? filter)
    {
        var result = new List<(MavTreeNode, int)>();
        if (_roots.Count == 0)
            return result;

        Dictionary<MavTreeNode, bool>? matches = null;
        if (filter is not null)
        {
            matches = new Dictionary<MavTreeNode, bool>(ReferenceEqualityComparer.Instance);
            foreach (var root in _roots)
                MatchesFilter(root, filter, matches);
        }

        void Visit(MavTreeNode node, int depth)
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
        MavTreeNode node,
        string filter,
        Dictionary<MavTreeNode, bool> memo)
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

    private bool RowStillMatches(TreeRow row, MavTreeNode node, int depth) =>
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

    private TreeRow CreateTreeRow(MavTreeNode node, int depth, MavTheme t)
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
                        Background = MavTheme.BrushOf(c.OutlineVariant.WithAlpha(0.55f)),
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
            var chevron = new MavIcon(node.IsExpanded ? MavIcons.ExpandMore : MavIcons.ChevronRight, 18)
            {
                UseThemeDefaultColor = false,
                Color = isSelected ? c.OnSecondaryContainer : c.OnSurfaceVariant,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var chevronBtn = new MavInkSurface
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
            var cb = new MavCheckBox(null, node.IsChecked)
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
            ? (node.IsExpanded ? MavIcons.FolderOpen : MavIcons.Folder)
            : MavIcons.Description);

        if (!string.IsNullOrEmpty(iconGlyph))
        {
            leadingStack.Children.Add(new MavIcon(iconGlyph, 18)
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
            Foreground = MavTheme.BrushOf(isSelected ? c.OnSecondaryContainer : c.OnSurface),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        textLbl.Apply(isSelected ? MavTypeRole.TitleSmall : MavTypeRole.BodyMedium, t.Typography);
        rowGrid.Add(textLbl, 3, 0);

        // Optional trailing badge
        if (!string.IsNullOrEmpty(node.BadgeText))
        {
            var badgeLbl = new TextBlock
            {
                Text = node.BadgeText,
                Foreground = MavTheme.BrushOf(isSelected ? c.OnPrimary : c.OnSurfaceVariant),
                VerticalAlignment = VerticalAlignment.Center,
            };
            badgeLbl.Apply(MavTypeRole.LabelSmall, t.Typography);

            var badgePill = new MavSurface
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

        var pill = new MavInkSurface
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
