using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Platform;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Column definition for a <see cref="MavDataTable"/>: header, value selector, width and alignment.</summary>
public sealed class MavDataColumn : StyledElement
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<MavDataColumn, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string?> PropertyNameProperty =
        AvaloniaProperty.Register<MavDataColumn, string?>(nameof(PropertyName));

    public static readonly StyledProperty<bool> IsNumericProperty =
        AvaloniaProperty.Register<MavDataColumn, bool>(nameof(IsNumeric));

    public static readonly StyledProperty<bool> IsSortableProperty =
        AvaloniaProperty.Register<MavDataColumn, bool>(nameof(IsSortable), true);

    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<MavDataColumn, IDataTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<GridLength> WidthProperty =
        AvaloniaProperty.Register<MavDataColumn, GridLength>(nameof(Width), new GridLength(1, GridUnitType.Star));

    private Func<object, object?>? _valueSelector;

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? PropertyName
    {
        get => GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    public string? BindingPath
    {
        get => PropertyName;
        set => PropertyName = value;
    }

    public Func<object, object?> ValueSelector
    {
        get => _valueSelector ?? ResolveByPropertyName;
        set => _valueSelector = value;
    }

    public bool IsNumeric
    {
        get => GetValue(IsNumericProperty);
        set => SetValue(IsNumericProperty, value);
    }

    public bool IsSortable
    {
        get => GetValue(IsSortableProperty);
        set => SetValue(IsSortableProperty, value);
    }

    [Content]
    public IDataTemplate? CellTemplate
    {
        get => GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    public Func<object, Control>? CellFactory { get; set; }

    public GridLength Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    /// <summary>Creates an empty column.</summary>
    public MavDataColumn()
    {
    }

    /// <summary>Creates a column with a header, a value selector and an optional width.</summary>
    public MavDataColumn(
        string header,
        Func<object, object?> valueSelector,
        bool isNumeric = false,
        bool isSortable = true,
        Func<object, Control>? cellTemplate = null,
        GridLength? width = null)
    {
        Header = header;
        _valueSelector = valueSelector;
        IsNumeric = isNumeric;
        IsSortable = isSortable;
        CellFactory = cellTemplate;
        Width = width ?? new GridLength(1, GridUnitType.Star);
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming",
        "IL2072",
        Justification = "PropertyName targets an application model type; trimmed apps must " +
                        "root those properties or use MavDataColumn.ValueSelector.")]
    private object? ResolveByPropertyName(object row)
    {
        if (row is null) return null;
        var propName = !string.IsNullOrEmpty(PropertyName) ? PropertyName : Header;
        if (string.IsNullOrEmpty(propName)) return row;
        var prop = MavPropertyCache.Get(row.GetType(), propName);
        return prop?.GetValue(row);
    }
}

/// <summary>
/// Row presentation for <see cref="MavDataTable"/>.
/// </summary>
public enum MavDataTableLayout
{
    /// <summary>Cards on narrow hosts, classic table otherwise.</summary>
    Auto,
    /// <summary>Always render the classic header + rows table.</summary>
    Table,
    /// <summary>Always render one card per row (phone layout).</summary>
    Cards,
}

/// <summary>
/// Material Design 3 Data Table with sortable columns, optional multi-row checkbox selection,
/// custom cell templates, live <c>INotifyCollectionChanged</c> updates, and pagination footer.
/// </summary>
public class MavDataTable : MavContentView, IEnumerable<MavDataColumn>
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavDataTable, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> IsSelectableProperty =
        AvaloniaProperty.Register<MavDataTable, bool>(nameof(IsSelectable));

    public static readonly StyledProperty<bool> ShowPaginationProperty =
        AvaloniaProperty.Register<MavDataTable, bool>(nameof(ShowPagination), true);

    public static readonly StyledProperty<int> PageSizeProperty =
        AvaloniaProperty.Register<MavDataTable, int>(nameof(PageSize), 5);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<MavDataTable, int>(nameof(CurrentPage), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> AlternatingRowBackgroundProperty =
        AvaloniaProperty.Register<MavDataTable, bool>(nameof(AlternatingRowBackground));

    public static readonly StyledProperty<string> FilterTextProperty =
        AvaloniaProperty.Register<MavDataTable, string>(nameof(FilterText), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<MavDataTable, string>(nameof(EmptyText), "No matching records");

    public static readonly StyledProperty<double> MaxBodyHeightProperty =
        AvaloniaProperty.Register<MavDataTable, double>(nameof(MaxBodyHeight), double.PositiveInfinity);

    public static readonly StyledProperty<MavDataTableLayout> LayoutProperty =
        AvaloniaProperty.Register<MavDataTable, MavDataTableLayout>(nameof(Layout), MavDataTableLayout.Auto);

    public static readonly StyledProperty<double> CompactBreakpointProperty =
        AvaloniaProperty.Register<MavDataTable, double>(nameof(CompactBreakpoint), 600.0);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavDataTable, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList?> SelectedItemsSourceProperty =
        AvaloniaProperty.Register<MavDataTable, IList?>(nameof(SelectedItemsSource));

    public static readonly StyledProperty<ICommand?> RowClickCommandProperty =
        AvaloniaProperty.Register<MavDataTable, ICommand?>(nameof(RowClickCommand));

    private readonly AvaloniaList<MavDataColumn> _columns = new();
    private readonly List<object> _allItems = [];
    private readonly HashSet<object> _selectedItems = [];
    private readonly List<(object Item, Action<bool> Apply)> _selectionVisuals = [];
    private readonly MavCollectionObserver _observer;
    private readonly Border _cardBorder;
    private readonly Border _borderOverlay;
    private readonly StackPanel _tableStack;
    private readonly Grid _footerGrid;
    private readonly TextBlock _selectionCountLabel;
    private readonly TextBlock _pageRangeLabel;
    private readonly MavIconButton _prevPageBtn;
    private readonly MavIconButton _nextPageBtn;
    private int _sortColumnIndex = -1;
    private bool _sortAscending = true;
    private bool _syncingSelection;
    private MavCheckBox? _masterCheck;
    private List<object>? _orderedItems;
    private int _currentPage;
    private int _currentPageSize;

    // Incremental row rendering: rows are appended in chunks while the body scrolls,
    // so a 1000+ row source does not build all children on the first layout.
    private const int RowChunkSize = 100;
    private List<object>? _pageItems;
    private StackPanel? _rowsContainer;
    private ScrollViewer? _rowsScroll;
    private readonly List<RowVisual> _rowVisuals = [];
    private readonly Dictionary<object, List<RowVisual>> _rowsByItem =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, PropertyChangedEventHandler> _itemPropertyHandlers =
        new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, int> _itemReferenceCounts =
        new(ReferenceEqualityComparer.Instance);
    private bool _itemPropertySubscriptionsActive;
    private int _renderedRowCount;
    private bool _appendingRows;
    private bool _refreshingRows;
    private bool _compactMode;
    private bool _lastCompact;

    /// <summary>
    /// A rendered table row (or compact card) together with the state needed to
    /// reuse it across incremental collection changes.
    /// </summary>
    private sealed class RowVisual
    {
        public required object Item { get; init; }
        public required Control Surface { get; init; }
        public Control? Divider { get; init; }
        public required Action<bool> ApplySelection { get; init; }
        public Action<int, int>? ApplyIndex { get; init; }
        public bool IsSelected { get; set; }
        public int Index { get; set; }
        public int Total { get; set; }
    }

    public event EventHandler? SelectionChanged;
    public event EventHandler<object>? RowClicked;

    static MavDataTable()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MavDataTable>((t, e) => t.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        SelectedItemProperty.Changed.AddClassHandler<MavDataTable>((t, e) => t.OnSelectedItemChanged(e.GetNewValue<object?>()));
        IsSelectableProperty.Changed.AddClassHandler<MavDataTable>((t, _) => t.RebuildTable());
        ShowPaginationProperty.Changed.AddClassHandler<MavDataTable>((t, _) => t.RebuildTable());
        PageSizeProperty.Changed.AddClassHandler<MavDataTable>((t, _) =>
        {
            t.CurrentPage = 0;
            t.RebuildTable();
        });
        CurrentPageProperty.Changed.AddClassHandler<MavDataTable>((t, _) =>
        {
            if (!t._refreshingRows)
                t.RebuildTable();
        });
        AlternatingRowBackgroundProperty.Changed.AddClassHandler<MavDataTable>((t, _) => t.RebuildTable());
        FilterTextProperty.Changed.AddClassHandler<MavDataTable>((t, _) =>
        {
            t.CurrentPage = 0;
            t.RebuildTable();
        });
        EmptyTextProperty.Changed.AddClassHandler<MavDataTable>((t, _) => t.RebuildTable());
        MaxBodyHeightProperty.Changed.AddClassHandler<MavDataTable>((t, _) => t.RebuildTable());
        LayoutProperty.Changed.AddClassHandler<MavDataTable>((t, _) => t.RebuildTable());
        CompactBreakpointProperty.Changed.AddClassHandler<MavDataTable>((t, _) => t.RebuildTable());
    }

    /// <summary>Creates an empty data table.</summary>
    public MavDataTable()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _columns.CollectionChanged += (_, _) =>
        {
            foreach (var col in _columns)
                MavVisualTreeHelpers.AttachLogicalChild(this, col);
            RebuildTable();
        };
        _observer = new MavCollectionObserver(
            this,
            () => ReloadItems(ItemsSource),
            OnCollectionChanged);

        _tableStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
        };

        _selectionCountLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
        };

        _pageRangeLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };

        _prevPageBtn = new MavIconButton(MavIcons.ChevronLeft);
        MavTooltip.SetText(_prevPageBtn, MavLocalization.PreviousPage);
        _prevPageBtn.Clicked += (_, _) =>
        {
            if (CurrentPage > 0) CurrentPage--;
        };

        _nextPageBtn = new MavIconButton(MavIcons.ChevronRight);
        MavTooltip.SetText(_nextPageBtn, MavLocalization.NextPage);
        _nextPageBtn.Clicked += (_, _) =>
        {
            if (CurrentPage < TotalPages - 1) CurrentPage++;
        };

        var navRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Children = { _pageRangeLabel, _prevPageBtn, _nextPageBtn },
        };

        _footerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            MinHeight = 52,
        };
        _footerGrid.Add(_selectionCountLabel, 0, 0);
        _footerGrid.Add(navRow, 1, 0);

        var outerStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children = { _tableStack, _footerGrid },
        };

        var innerClip = new Border
        {
            CornerRadius = new CornerRadius(11),
            Margin = new Thickness(1),
            ClipToBounds = true,
            Child = outerStack,
        };

        _borderOverlay = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
        };

        _cardBorder = new Border
        {
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Child = new Grid
            {
                Children = { innerClip, _borderOverlay },
            },
        };

        Content = _cardBorder;
        MavTheme.Observe(this, (self, _) => self.RebuildTable());

        AttachedToVisualTree += (_, _) =>
        {
            _itemPropertySubscriptionsActive = true;
            TrackAllItemProperties();
        };
        DetachedFromVisualTree += (_, _) =>
        {
            _itemPropertySubscriptionsActive = false;
            ClearItemPropertySubscriptions();
        };

        // Auto layout: switch between table and cards when the host crosses the breakpoint.
        SizeChanged += (_, _) =>
        {
            var compact = IsCompact;
            if (compact == _lastCompact)
                return;
            _lastCompact = compact;
            RebuildTable();
        };
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public bool IsSelectable
    {
        get => GetValue(IsSelectableProperty);
        set => SetValue(IsSelectableProperty, value);
    }

    public bool ShowPagination
    {
        get => GetValue(ShowPaginationProperty);
        set => SetValue(ShowPaginationProperty, value);
    }

    public int PageSize
    {
        get => GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, Math.Max(1, value));
    }

    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, Math.Max(0, value));
    }

    public bool AlternatingRowBackground
    {
        get => GetValue(AlternatingRowBackgroundProperty);
        set => SetValue(AlternatingRowBackgroundProperty, value);
    }

    public string FilterText
    {
        get => GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    public string EmptyText
    {
        get => GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    public double MaxBodyHeight
    {
        get => GetValue(MaxBodyHeightProperty);
        set => SetValue(MaxBodyHeightProperty, value);
    }

    /// <summary>
    /// Row presentation: <see cref="MavDataTableLayout.Auto"/> switches to cards on
    /// hosts narrower than <see cref="CompactBreakpoint"/>.
    /// </summary>
    public MavDataTableLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    /// <summary>Width in DIPs below which Auto uses the card layout. Default 600.</summary>
    public double CompactBreakpoint
    {
        get => GetValue(CompactBreakpointProperty);
        set => SetValue(CompactBreakpointProperty, value);
    }

    /// <summary>True when the current layout is the phone/card presentation.</summary>
    public bool IsCompact => Layout switch
    {
        MavDataTableLayout.Cards => true,
        MavDataTableLayout.Table => false,
        _ => Bounds.Width > 0 && Bounds.Width < CompactBreakpoint,
    };

    /// <summary>Resolved presentation used by the last rebuild.</summary>
    public MavDataTableLayout EffectiveLayout =>
        IsCompact ? MavDataTableLayout.Cards : MavDataTableLayout.Table;

    public ICommand? RowClickCommand
    {
        get => GetValue(RowClickCommandProperty);
        set => SetValue(RowClickCommandProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IList? SelectedItemsSource
    {
        get => GetValue(SelectedItemsSourceProperty);
        set => SetValue(SelectedItemsSourceProperty, value);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection) return;
        _selectedItems.Clear();
        if (item is not null)
            _selectedItems.Add(item);
        SyncExternalSelectedItems();
        RefreshSelectionVisuals();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshSelectionVisuals()
    {
        _syncingSelection = true;
        try
        {
            foreach (var (item, apply) in _selectionVisuals)
                apply(_selectedItems.Contains(item));

            if (_masterCheck is not null)
            {
                var all = _allItems.Count > 0 && _selectedItems.Count == _allItems.Count;
                var some = _selectedItems.Count > 0 && !all;
                _masterCheck.IsThreeState = some;
                _masterCheck.IsIndeterminate = some;
                _masterCheck.IsChecked = all;
            }
        }
        finally
        {
            _syncingSelection = false;
        }

        if (_orderedItems is not null && _pageItems is not null)
        {
            var t = MavTheme.Current;
            UpdateFooter(_orderedItems, _pageItems, _currentPage, _currentPageSize, t, t.Color);
        }
    }

    private void SyncExternalSelectedItems()
    {
        _syncingSelection = true;
        SetValue(SelectedItemProperty, _selectedItems.FirstOrDefault());
        if (SelectedItemsSource is { IsReadOnly: false } list)
        {
            list.Clear();
            foreach (var it in _selectedItems)
                list.Add(it);
        }
        _syncingSelection = false;
    }

    [Content]
    public AvaloniaList<MavDataColumn> Columns => _columns;
    public IReadOnlyCollection<object> SelectedItems => _selectedItems;
    public int TotalPages
    {
        get
        {
            var count = GetFilteredItemsCount();
            return count == 0 ? 1 : (int)Math.Ceiling(count / (double)Math.Max(1, PageSize));
        }
    }

    /// <summary>Adds a column with a header and a value selector and returns the table for chaining.</summary>
    public MavDataTable AddColumn(
        string header,
        Func<object, object?> valueSelector,
        bool isNumeric = false,
        bool isSortable = true,
        Func<object, Control>? cellTemplate = null,
        GridLength? width = null)
    {
        _columns.Add(new MavDataColumn(header, valueSelector, isNumeric, isSortable, cellTemplate, width));
        return this;
    }

    /// <summary>Adds an existing column and returns the table for chaining.</summary>
    public MavDataTable AddColumn(MavDataColumn column)
    {
        _columns.Add(column);
        return this;
    }

    public void Add(MavDataColumn column) => _columns.Add(column);

    public IEnumerator<MavDataColumn> GetEnumerator() => _columns.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Clears the selected rows.</summary>
    public void ClearSelection()
    {
        if (_selectedItems.Count == 0) return;
        _selectedItems.Clear();
        RefreshSelectionVisuals();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        ReloadItems(src);
    }

    private void ReloadItems(IEnumerable? src)
    {
        _allItems.Clear();
        if (src is not null)
        {
            foreach (var item in src)
            {
                if (item is not null)
                    _allItems.Add(item);
            }
        }
        PruneSelection();
        SyncItemPropertySubscriptions();
        if (CurrentPage >= TotalPages)
            CurrentPage = Math.Max(0, TotalPages - 1);
        RebuildTable();
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_refreshingRows || _allItems.Count == 0)
        {
            ReloadItems(ItemsSource);
            return;
        }

        var oldCount = _allItems.Count;
        _refreshingRows = true;
        try
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add
                    when e.NewStartingIndex >= 0 && e.NewItems is { Count: > 0 } added
                         && !ContainsNull(added):
                {
                    var index = Math.Clamp(e.NewStartingIndex, 0, _allItems.Count);
                    var newItems = new List<object>(added.Count);
                    foreach (var item in added)
                        newItems.Add(item!);
                    _allItems.InsertRange(index, newItems);
                    break;
                }

                case NotifyCollectionChangedAction.Remove
                    when e.OldStartingIndex >= 0 && e.OldItems is { Count: > 0 } removed
                         && e.OldStartingIndex + removed.Count <= _allItems.Count:
                    _allItems.RemoveRange(e.OldStartingIndex, removed.Count);
                    break;

                case NotifyCollectionChangedAction.Replace
                    when e.NewStartingIndex >= 0 && e.NewItems is { Count: > 0 } replaced
                         && e.OldItems is { Count: > 0 } oldReplaced
                         && oldReplaced.Count == replaced.Count
                         && e.NewStartingIndex + replaced.Count <= _allItems.Count
                         && !ContainsNull(replaced):
                    for (var i = 0; i < replaced.Count; i++)
                        _allItems[e.NewStartingIndex + i] = replaced[i]!;
                    break;

                case NotifyCollectionChangedAction.Move
                    when e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0
                         && e.OldItems is { Count: > 0 } moved
                         && e.OldStartingIndex + moved.Count <= _allItems.Count
                         && e.NewStartingIndex + moved.Count <= _allItems.Count:
                {
                    var movedItems = _allItems.GetRange(e.OldStartingIndex, moved.Count);
                    _allItems.RemoveRange(e.OldStartingIndex, moved.Count);
                    _allItems.InsertRange(e.NewStartingIndex, movedItems);
                    break;
                }

                default:
                    ReloadItems(ItemsSource);
                    return;
            }

            PruneSelection();
            UpdateItemSubscriptions(e);
            if (CurrentPage >= TotalPages)
                CurrentPage = Math.Max(0, TotalPages - 1);

            // Unsorted/unfiltered tables that do not paginate can update a single
            // affected row (or append/remove a tail row) without re-indexing the page.
            if (TryApplyDirectCollectionChange(e, oldCount))
                return;

            if (!TryRefreshRowsIncrementally())
                ReloadItems(ItemsSource);
        }
        finally
        {
            _refreshingRows = false;
        }
    }

    /// <summary>
    /// Selection pruning used to call <see cref="List{T}.Contains(object)"/> for every
    /// selected item, which is O(S×N) on large tables. One membership set makes it O(S+N).
    /// </summary>
    private void PruneSelection()
    {
        if (_selectedItems.Count == 0)
            return;

        var present = new HashSet<object>(_allItems);
        _selectedItems.RemoveWhere(item => !present.Contains(item));
    }

    private void UpdateItemSubscriptions(NotifyCollectionChangedEventArgs e)
    {
        if (!_itemPropertySubscriptionsActive)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } added:
                foreach (var item in added)
                {
                    if (item is not null)
                        TrackItemProperty(item);
                }
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is { } removed:
                foreach (var item in removed)
                {
                    if (item is not null)
                        UntrackItemProperty(item);
                }
                break;

            case NotifyCollectionChangedAction.Replace
                when e.OldItems is { } oldItems && e.NewItems is { } newItems:
                foreach (var item in oldItems)
                {
                    if (item is not null)
                        UntrackItemProperty(item);
                }
                foreach (var item in newItems)
                {
                    if (item is not null)
                        TrackItemProperty(item);
                }
                break;
        }
    }

    private static bool ContainsNull(IList items)
    {
        foreach (var item in items)
        {
            if (item is null)
                return true;
        }

        return false;
    }

    private void SyncItemPropertySubscriptions()
    {
        ClearItemPropertySubscriptions();
        if (!_itemPropertySubscriptionsActive)
            return;

        foreach (var item in _allItems)
            TrackItemProperty(item);
    }

    private void TrackAllItemProperties()
    {
        ClearItemPropertySubscriptions();
        foreach (var item in _allItems)
            TrackItemProperty(item);
    }

    private void ClearItemPropertySubscriptions()
    {
        foreach (var (item, handler) in _itemPropertyHandlers)
        {
            if (item is INotifyPropertyChanged inpc)
                inpc.PropertyChanged -= handler;
        }

        _itemPropertyHandlers.Clear();
        _itemReferenceCounts.Clear();
    }

    private void TrackItemProperty(object item)
    {
        if (!_itemPropertySubscriptionsActive || item is not INotifyPropertyChanged inpc)
            return;

        if (_itemReferenceCounts.TryGetValue(item, out var count))
        {
            _itemReferenceCounts[item] = count + 1;
            return;
        }

        var handler = new PropertyChangedEventHandler(OnRowItemPropertyChanged);
        _itemReferenceCounts[item] = 1;
        _itemPropertyHandlers[item] = handler;
        inpc.PropertyChanged += handler;
    }

    private void UntrackItemProperty(object item)
    {
        if (item is not INotifyPropertyChanged inpc)
            return;

        if (!_itemReferenceCounts.TryGetValue(item, out var count))
            return;

        if (count > 1)
        {
            _itemReferenceCounts[item] = count - 1;
            return;
        }

        _itemReferenceCounts.Remove(item);
        if (_itemPropertyHandlers.Remove(item, out var handler))
            inpc.PropertyChanged -= handler;
    }

    private void OnRowItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is null || _refreshingRows || _pageItems is null)
            return;

        if (string.IsNullOrWhiteSpace(FilterText) && _sortColumnIndex < 0)
        {
            if (!_rowsByItem.TryGetValue(sender, out var rows))
                return;

            foreach (var visual in rows.ToArray())
            {
                var index = visual.Index;
                if ((uint)index < (uint)_pageItems.Count
                    && ReferenceEquals(_pageItems[index], sender))
                {
                    ReplaceRenderedRow(index, sender);
                }
            }

            return;
        }

        // Filtering/sorting may move or hide the row, so drop only the affected
        // visuals and let the normal reconciler place the fresh controls.
        if (_rowsByItem.TryGetValue(sender, out var affected))
        {
            foreach (var visual in affected.ToArray())
            {
                var index = _rowVisuals.IndexOf(visual);
                if (index >= 0)
                    RemoveRenderedRow(index);
            }
        }

        if (!TryRefreshRowsIncrementally())
            ReloadItems(ItemsSource);
    }

    /// <summary>
    /// Fast path for the common live-update case: no filter, no sort, no pagination.
    /// In that mode the page is the source list, so an append, a tail removal, or an
    /// in-place replacement can be applied at one index without scanning the page.
    /// </summary>
    private bool TryApplyDirectCollectionChange(NotifyCollectionChangedEventArgs e, int oldCount)
    {
        if (_rowsContainer is null || _pageItems is null || _orderedItems is null
            || _columns.Count == 0 || _compactMode != IsCompact || ShowPagination
            || !string.IsNullOrWhiteSpace(FilterText) || _sortColumnIndex >= 0
            || oldCount != _pageItems.Count)
        {
            return false;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add
                when e.NewStartingIndex == oldCount && e.NewItems is { Count: > 0 } added:
            {
                var newItems = new List<object>(added.Count);
                foreach (var item in added)
                    newItems.Add(item!);

                _pageItems.AddRange(newItems);
                if (!ReferenceEquals(_orderedItems, _pageItems))
                    _orderedItems.AddRange(newItems);

                var wasFullyRendered = _renderedRowCount >= oldCount;
                var oldLast = wasFullyRendered ? _rowVisuals.LastOrDefault() : null;
                if (wasFullyRendered)
                {
                    if (_rowsScroll is null)
                    {
                        while (_renderedRowCount < _pageItems.Count)
                            AppendRowsChunk();
                    }
                    else
                    {
                        AppendRowsChunk();
                    }

                    // The previous last row now has a divider.
                    oldLast?.ApplyIndex?.Invoke(oldLast.Index, _pageItems.Count);
                }

                _currentPage = 0;
                _currentPageSize = Math.Max(1, _pageItems.Count);
                RefreshSelectionVisuals();
                return true;
            }

            case NotifyCollectionChangedAction.Remove
                when e.OldStartingIndex >= 0 && e.OldItems is { Count: > 0 } removed
                     && e.OldStartingIndex + removed.Count == oldCount:
            {
                var start = e.OldStartingIndex;
                var newCount = oldCount - removed.Count;
                if (newCount == 0)
                    return false;

                _pageItems.RemoveRange(start, removed.Count);
                if (!ReferenceEquals(_orderedItems, _pageItems))
                    _orderedItems.RemoveRange(start, removed.Count);

                for (var i = _rowVisuals.Count - 1; i >= start; i--)
                    RemoveRenderedRow(i);

                _renderedRowCount = Math.Min(_renderedRowCount, newCount);
                if (_rowVisuals.Count > 0)
                {
                    var last = _rowVisuals[^1];
                    last.ApplyIndex?.Invoke(last.Index, newCount);
                }

                _currentPage = 0;
                _currentPageSize = Math.Max(1, newCount);
                RefreshSelectionVisuals();
                return true;
            }

            case NotifyCollectionChangedAction.Replace
                when e.NewStartingIndex >= 0 && e.NewItems is { Count: > 0 } replacement
                     && e.OldItems is { Count: > 0 } oldReplacement
                     && e.OldStartingIndex == e.NewStartingIndex
                     && oldReplacement.Count == replacement.Count:
            {
                var start = e.NewStartingIndex;
                var count = replacement.Count;
                if (start + count > _pageItems.Count)
                    return false;

                for (var i = 0; i < count; i++)
                {
                    var index = start + i;
                    var item = replacement[i]!;
                    _pageItems[index] = item;
                    if (!ReferenceEquals(_orderedItems, _pageItems))
                        _orderedItems[index] = item;

                    if (index < _renderedRowCount)
                        ReplaceRenderedRow(index, item);
                }

                RefreshSelectionVisuals();
                return true;
            }
        }

        return false;
    }

    private void ReplaceRenderedRow(int index, object item)
    {
        if (_rowsContainer is null || _pageItems is null)
            return;

        var old = _rowVisuals[index];
        var t = MavTheme.Current;
        var c = t.Color;
        var replacement = _compactMode
            ? CreateCard(item, t, c)
            : CreateRow(item, index, _pageItems.Count, t, c, IsSelectable ? 1 : 0);
        replacement.Index = index;
        replacement.Total = _pageItems.Count;

        var childIndex = _rowsContainer.Children.IndexOf(old.Surface);
        DetachRenderedRow(old);
        if (childIndex >= 0)
        {
            childIndex = Math.Min(childIndex, _rowsContainer.Children.Count);
            _rowsContainer.Children.Insert(childIndex, replacement.Surface);
            if (replacement.Divider is not null)
            {
                _rowsContainer.Children.Insert(
                    Math.Min(childIndex + 1, _rowsContainer.Children.Count),
                    replacement.Divider);
            }
        }

        _rowVisuals[index] = replacement;
        UnindexRow(old);
        IndexRow(replacement);
        _selectionVisuals.RemoveAll(v => ReferenceEquals(v.Apply, old.ApplySelection));
    }

    private void RemoveRenderedRow(int index)
    {
        var visual = _rowVisuals[index];
        DetachRenderedRow(visual);
        UnindexRow(visual);
        _selectionVisuals.RemoveAll(v => ReferenceEquals(v.Apply, visual.ApplySelection));
        _rowVisuals.RemoveAt(index);
    }

    private void DetachRenderedRow(RowVisual visual)
    {
        if (_rowsContainer is null)
            return;

        _rowsContainer.Children.Remove(visual.Surface);
        if (visual.Divider is not null)
            _rowsContainer.Children.Remove(visual.Divider);
    }

    private void IndexRow(RowVisual visual)
    {
        if (!_rowsByItem.TryGetValue(visual.Item, out var rows))
            _rowsByItem[visual.Item] = rows = [];

        rows.Add(visual);
    }

    private void UnindexRow(RowVisual visual)
    {
        if (!_rowsByItem.TryGetValue(visual.Item, out var rows))
            return;

        rows.Remove(visual);
        if (rows.Count == 0)
            _rowsByItem.Remove(visual.Item);
    }

    private void ReindexRows()
    {
        _rowsByItem.Clear();
        foreach (var visual in _rowVisuals)
            IndexRow(visual);
    }

    private void SyncRenderedControls(List<Control> desired)
    {
        if (_rowsContainer is null)
            return;

        var common = 0;
        var commonLimit = Math.Min(_rowsContainer.Children.Count, desired.Count);
        while (common < commonLimit
               && ReferenceEquals(_rowsContainer.Children[common], desired[common]))
        {
            common++;
        }

        if (common == _rowsContainer.Children.Count && common == desired.Count)
            return;

        for (var i = _rowsContainer.Children.Count - 1; i >= common; i--)
            _rowsContainer.Children.RemoveAt(i);

        for (var i = common; i < desired.Count; i++)
            _rowsContainer.Children.Add(desired[i]);
    }

    /// <summary>
    /// Reconciles the rendered rows against the current ordered page by item
    /// reference: unchanged rows keep their controls, new rows are built, removed
    /// rows are detached. Returns false when a full rebuild is required (empty
    /// state, chunked body, layout mode change, page out of range).
    /// </summary>
    private bool TryRefreshRowsIncrementally()
    {
        if (_rowsContainer is null || _pageItems is null || _columns.Count == 0)
            return false;
        if (_compactMode != IsCompact)
            return false;

        var ordered = GetOrderedItems();
        if (ordered.Count == 0 || _pageItems.Count == 0)
            return false;

        var pageSize = ShowPagination ? Math.Max(1, PageSize) : Math.Max(1, ordered.Count);
        var page = ShowPagination
            ? Math.Clamp(CurrentPage, 0, Math.Max(0, TotalPages - 1))
            : 0;
        // The clamped page differs from the requested one (e.g. the item count shrank),
        // so the incremental path cannot reproduce the state; the caller rebuilds fully.
        if (page != CurrentPage && ShowPagination)
            return false;

        var pageItems = ShowPagination
            ? ordered.Skip(page * pageSize).Take(pageSize).ToList()
            : ordered;
        if (pageItems.Count == 0)
            return false;

        // Only the already rendered prefix is reconciled; the remaining rows stay
        // chunked and are appended on scroll as before. A previously fully rendered
        // page stays fully rendered when it grows (e.g. append at the end).
        var wasFullyRendered = _renderedRowCount >= _pageItems.Count;
        var renderCount = wasFullyRendered
            ? pageItems.Count
            : Math.Min(pageItems.Count, Math.Max(_renderedRowCount, 1));
        var renderedItems = pageItems.Take(renderCount).ToList();

        var t = MavTheme.Current;
        var c = t.Color;
        var colOffset = IsSelectable ? 1 : 0;

        // Reuse controls for the same item instances. A queue per item keeps
        // duplicate references in the source deterministic.
        var buckets = new Dictionary<object, Queue<RowVisual>>(ReferenceEqualityComparer.Instance);
        foreach (var visual in _rowVisuals)
        {
            if (!buckets.TryGetValue(visual.Item, out var queue))
                buckets[visual.Item] = queue = new Queue<RowVisual>();
            queue.Enqueue(visual);
        }

        var desired = new List<RowVisual>(renderedItems.Count);
        for (var i = 0; i < renderedItems.Count; i++)
        {
            var item = renderedItems[i];
            if (buckets.TryGetValue(item, out var queue) && queue.Count > 0)
                desired.Add(queue.Dequeue());
            else
                desired.Add(_compactMode
                    ? CreateCard(item, t, c)
                    : CreateRow(item, i, pageItems.Count, t, c, colOffset));
        }

        var keep = new HashSet<RowVisual>(desired);
        // Rows that are no longer desired are removed from the tree. Their selection
        // callbacks are collected first so the matching entries can be dropped in one pass.
        var staleCallbacks = new HashSet<Action<bool>>();
        foreach (var stale in _rowVisuals)
        {
            if (keep.Contains(stale))
                continue;

            _rowsContainer.Children.Remove(stale.Surface);
            if (stale.Divider is not null)
                _rowsContainer.Children.Remove(stale.Divider);
            staleCallbacks.Add(stale.ApplySelection);
        }

        if (staleCallbacks.Count > 0)
            _selectionVisuals.RemoveAll(v => staleCallbacks.Contains(v.Apply));

        _rowVisuals.Clear();
        _rowVisuals.AddRange(desired);

        // Index and divider updates are only needed when the row position or page
        // size really changed; a one-row replacement must not touch every sibling.
        for (var i = 0; i < desired.Count; i++)
        {
            var visual = desired[i];
            if (visual.Index == i && visual.Total == pageItems.Count)
                continue;

            visual.ApplyIndex?.Invoke(i, pageItems.Count);
            if (visual.ApplyIndex is null)
            {
                visual.Index = i;
                visual.Total = pageItems.Count;
            }
        }

        // Restore visual order without an IndexOf scan per child.
        var controls = new List<Control>(desired.Count * 2);
        foreach (var visual in desired)
        {
            controls.Add(visual.Surface);
            if (visual.Divider is not null)
                controls.Add(visual.Divider);
        }

        SyncRenderedControls(controls);
        ReindexRows();

        _orderedItems = ordered;
        _pageItems = pageItems;
        _currentPage = page;
        _currentPageSize = pageSize;
        _renderedRowCount = desired.Count;
        UpdateFooter(ordered, pageItems, page, pageSize, t, c);
        RefreshSelectionVisuals();
        return true;
    }

    private int GetFilteredItemsCount()
    {
        var filter = FilterText?.Trim();
        if (string.IsNullOrEmpty(filter) || _columns.Count == 0)
            return _allItems.Count;
        return _allItems.Count(item => MatchesFilter(item, filter));
    }

    private bool MatchesFilter(object item, string filter)
    {
        foreach (var col in _columns)
        {
            var val = col.ValueSelector(item)?.ToString();
            if (!string.IsNullOrEmpty(val) && val.Contains(filter, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private List<object> GetOrderedItems()
    {
        var filter = FilterText?.Trim();
        var filtered = string.IsNullOrEmpty(filter) || _columns.Count == 0
            ? _allItems.ToList()
            : _allItems.Where(item => MatchesFilter(item, filter)).ToList();

        if (_sortColumnIndex < 0 || _sortColumnIndex >= _columns.Count)
            return filtered;

        var col = _columns[_sortColumnIndex];
        filtered.Sort((a, b) =>
        {
            var va = col.ValueSelector(a);
            var vb = col.ValueSelector(b);
            int cmp;
            if (va is null && vb is null) cmp = 0;
            else if (va is null) cmp = -1;
            else if (vb is null) cmp = 1;
            else if (va is IComparable ca && vb is IComparable cb && va.GetType() == vb.GetType())
                cmp = ca.CompareTo(cb);
            else if (ConvertToDouble(va, out var da) && ConvertToDouble(vb, out var db))
                cmp = da.CompareTo(db);
            else
                cmp = string.Compare(va.ToString(), vb.ToString(), StringComparison.CurrentCultureIgnoreCase);

            return _sortAscending ? cmp : -cmp;
        });
        return filtered;
    }

    private static bool ConvertToDouble(object v, out double result)
    {
        if (v is double d) { result = d; return true; }
        if (v is float f) { result = f; return true; }
        if (v is int i) { result = i; return true; }
        if (v is long l) { result = l; return true; }
        if (v is decimal m) { result = (double)m; return true; }
        return double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private ColumnDefinitions BuildColumnDefinitions()
    {
        var defs = new ColumnDefinitions();
        if (IsSelectable)
            defs.Add(new ColumnDefinition(48, GridUnitType.Pixel));
        foreach (var col in _columns)
            defs.Add(new ColumnDefinition(col.Width));
        return defs;
    }

    private void RebuildTable()
    {
        var t = MavTheme.Current;
        var c = t.Color;

        _cardBorder.Background = MavTheme.BrushOf(c.Surface);
        _borderOverlay.BorderBrush = MavTheme.BrushOf(c.OutlineVariant);

        if (_rowsScroll is not null)
        {
            _rowsScroll.ScrollChanged -= OnRowsScrollChanged;
            _rowsScroll = null;
        }
        _rowsContainer = null;
        _pageItems = null;
        _renderedRowCount = 0;
        _appendingRows = false;
        _rowVisuals.Clear();
        _rowsByItem.Clear();
        _selectionVisuals.Clear();
        _masterCheck = null;
        _orderedItems = null;

        _tableStack.Children.Clear();
        if (_columns.Count == 0) return;

        var ordered = GetOrderedItems();
        int pageSize = ShowPagination ? Math.Max(1, PageSize) : Math.Max(1, ordered.Count);
        int page = ShowPagination ? Math.Clamp(CurrentPage, 0, Math.Max(0, TotalPages - 1)) : 0;
        var pageItems = ShowPagination
            ? ordered.Skip(page * pageSize).Take(pageSize).ToList()
            : ordered;

        _orderedItems = ordered;
        _currentPage = page;
        _currentPageSize = pageSize;
        _compactMode = IsCompact;
        _lastCompact = _compactMode;

        if (_compactMode)
        {
            BuildCompactBody(pageItems, t, c);
            UpdateFooter(ordered, pageItems, page, pageSize, t, c);
            return;
        }

        // Header Row
        var headerGrid = new Grid
        {
            ColumnDefinitions = BuildColumnDefinitions(),
            MinHeight = 52,
            Background = MavTheme.BrushOf(c.SurfaceContainerLow),
        };

        int colOffset = 0;
        if (IsSelectable)
        {
            var allSelected = _allItems.Count > 0 && _selectedItems.Count == _allItems.Count;
            var someSelected = _selectedItems.Count > 0 && !allSelected;
            var masterCheck = new MavCheckBox(isChecked: allSelected, isIndeterminate: someSelected)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsThreeState = someSelected,
            };
            _masterCheck = masterCheck;
            masterCheck.StateChanged += (_, _) =>
            {
                if (_syncingSelection) return;

                var all = _allItems.Count > 0 && _selectedItems.Count == _allItems.Count;
                if (all)
                    _selectedItems.Clear();
                else
                {
                    foreach (var it in _allItems)
                        _selectedItems.Add(it);
                }
                RefreshSelectionVisuals();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };
            headerGrid.Add(masterCheck, 0, 0);
            colOffset = 1;
        }

        for (int i = 0; i < _columns.Count; i++)
        {
            var colIndex = i;
            var col = _columns[i];
            var isSorted = _sortColumnIndex == i;

            var headerText = new TextBlock
            {
                Text = col.Header,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = MavTheme.BrushOf(isSorted ? c.Primary : c.OnSurfaceVariant),
                FontWeight = FontWeight.SemiBold,
            };
            t.Typography.TitleSmall.ApplyTo(headerText);
            headerText.FontWeight = FontWeight.SemiBold;

            var headerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = col.IsNumeric ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            };

            if (col.IsSortable && isSorted)
            {
                var sortIcon = new MavIcon(_sortAscending ? MavIcons.ArrowUpward : MavIcons.ArrowDownward)
                {
                    Size = 16,
                    UseThemeDefaultColor = false,
                    Color = c.Primary,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (col.IsNumeric)
                {
                    headerStack.Children.Add(sortIcon);
                    headerStack.Children.Add(headerText);
                }
                else
                {
                    headerStack.Children.Add(headerText);
                    headerStack.Children.Add(sortIcon);
                }
            }
            else
            {
                headerStack.Children.Add(headerText);
            }

            var cellBorder = new Border
            {
                Padding = new Thickness(16, 12),
                Background = Brushes.Transparent,
                Cursor = col.IsSortable ? new Cursor(StandardCursorType.Hand) : Cursor.Default,
                Child = headerStack,
            };

            if (col.IsSortable)
            {
                cellBorder.PointerPressed += (_, e) =>
                {
                    e.Handled = true;
                    if (_sortColumnIndex == colIndex)
                        _sortAscending = !_sortAscending;
                    else
                    {
                        _sortColumnIndex = colIndex;
                        _sortAscending = true;
                    }
                    RebuildTable();
                };
            }

            headerGrid.Add(cellBorder, colOffset + i, 0);
        }

        _tableStack.Children.Add(headerGrid);
        _tableStack.Children.Add(new MavDivider());

        var rowsContainer = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
        };

        if (pageItems.Count == 0)
        {
            var emptyLbl = new TextBlock
            {
                Text = EmptyText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(16, 24),
                Foreground = MavTheme.BrushOf(c.OnSurfaceVariant),
            };
            t.Typography.BodyMedium.ApplyTo(emptyLbl);
            rowsContainer.Children.Add(emptyLbl);
            if (ShowPagination)
                rowsContainer.Children.Add(new MavDivider());
        }

        // Data Rows: rendered incrementally. When the body scrolls, the next chunk is
        // appended so a 1000+ row source does not build every child on first layout.
        _pageItems = pageItems;
        _rowsContainer = rowsContainer;
        _renderedRowCount = 0;
        AppendRowsChunk();

        AttachBody(rowsContainer, pageItems);

        // Footer
        UpdateFooter(ordered, pageItems, page, pageSize, t, c);
    }

    private void UpdateFooter(
        List<object> ordered,
        List<object> pageItems,
        int page,
        int pageSize,
        MavTheme t,
        MavColorScheme c)
    {
        _footerGrid.IsVisible = ShowPagination || (IsSelectable && _selectedItems.Count > 0);
        _footerGrid.Background = MavTheme.BrushOf(c.SurfaceContainerLow);

        t.Typography.LabelMedium.ApplyTo(_selectionCountLabel);
        _selectionCountLabel.Foreground = MavTheme.BrushOf(c.Primary);
        _selectionCountLabel.Text = IsSelectable && _selectedItems.Count > 0
            ? MavLocalization.Format(
                MavLocalization.Keys.SelectedCountFormat,
                "{0} selected",
                _selectedItems.Count)
            : string.Empty;

        t.Typography.BodySmall.ApplyTo(_pageRangeLabel);
        _pageRangeLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        if (ordered.Count == 0)
        {
            _pageRangeLabel.Text = MavLocalization.EmptyPageRange;
        }
        else
        {
            int startIdx = page * pageSize + 1;
            int endIdx = Math.Min(ordered.Count, startIdx + pageItems.Count - 1);
            _pageRangeLabel.Text = MavLocalization.Format(
                MavLocalization.Keys.PageRangeFormat,
                "{0}\u2013{1} of {2}",
                startIdx,
                endIdx,
                ordered.Count);
        }

        _prevPageBtn.IsEnabled = page > 0;
        _nextPageBtn.IsEnabled = page < TotalPages - 1;
        _prevPageBtn.IsVisible = ShowPagination;
        _nextPageBtn.IsVisible = ShowPagination;
        _pageRangeLabel.IsVisible = ShowPagination;
    }

    private void BuildCompactBody(List<object> pageItems, MavTheme t, MavColorScheme c)
    {
        var sortBar = BuildCompactSortBar(t, c);
        if (sortBar is not null)
            _tableStack.Children.Add(sortBar);

        var cards = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            Margin = new Thickness(12),
        };

        if (pageItems.Count == 0)
        {
            var empty = new TextBlock
            {
                Text = EmptyText,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(16, 24),
                Foreground = MavTheme.BrushOf(c.OnSurfaceVariant),
            };
            t.Typography.BodyMedium.ApplyTo(empty);
            cards.Children.Add(empty);
        }

        _pageItems = pageItems;
        _rowsContainer = cards;
        _renderedRowCount = 0;
        AppendRowsChunk();
        AttachBody(cards, pageItems);
    }

    private Control? BuildCompactSortBar(MavTheme t, MavColorScheme c)
    {
        var sortable = new List<(int Index, MavDataColumn Column)>();
        for (var i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].IsSortable)
                sortable.Add((i, _columns[i]));
        }

        if (sortable.Count == 0)
            return null;

        var bar = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(12, 12, 12, 0),
        };

        foreach (var (index, column) in sortable)
        {
            var selected = index == _sortColumnIndex;
            var chip = new MavChip(column.Header, MavChipKind.Filter, selected: selected)
            {
                Margin = new Thickness(0, 0, 8, 8),
            };
            if (selected)
                chip.Icon = _sortAscending ? MavIcons.ArrowUpward : MavIcons.ArrowDownward;

            var captured = index;
            chip.Clicked += (_, _) =>
            {
                if (_sortColumnIndex == captured)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortColumnIndex = captured;
                    _sortAscending = true;
                }
                RebuildTable();
            };
            bar.Children.Add(chip);
        }

        return bar;
    }

    private RowVisual CreateCard(object rowItem, MavTheme t, MavColorScheme c)
    {
        var isSelected = _selectedItems.Contains(rowItem);

        var body = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
        };

        var headlineRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 8,
        };

        var headline = BuildCellContent(rowItem, _columns[0], t, c, headline: true);
        headlineRow.Add(headline, 0, 0);

        MavCheckBox? check = null;
        if (IsSelectable)
        {
            check = new MavCheckBox(isChecked: isSelected);
            MavAutomation.SetName(check, MavLocalization.SelectRow);
            check.CheckedChanged += (_, checkedValue) =>
            {
                if (_syncingSelection) return;

                if (checkedValue) _selectedItems.Add(rowItem);
                else _selectedItems.Remove(rowItem);
                SyncExternalSelectedItems();
                RefreshSelectionVisuals();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };
            headlineRow.Add(check, 1, 0);
        }

        body.Children.Add(headlineRow);

        for (var i = 1; i < _columns.Count; i++)
        {
            var column = _columns[i];
            var line = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 12,
            };

            var label = new TextBlock
            {
                Text = column.Header,
                VerticalAlignment = VerticalAlignment.Top,
                Foreground = MavTheme.BrushOf(c.OnSurfaceVariant),
                MinWidth = 88,
                TextWrapping = TextWrapping.Wrap,
            };
            t.Typography.LabelSmall.ApplyTo(label);

            var value = BuildCellContent(rowItem, column, t, c, headline: false);
            line.Add(label, 0, 0);
            line.Add(value, 1, 0);
            body.Children.Add(line);
        }

        var card = new MavCard(MavCardVariant.Outlined, body, interactive: true)
        {
            Margin = new Thickness(0),
            CardPadding = new Thickness(16),
        };
        card.BackgroundColor = isSelected
            ? c.SecondaryContainer.WithAlpha(0.45f)
            : c.Surface;
        card.Clicked += (_, _) => ActivateRow(rowItem);

        var visual = new RowVisual
        {
            Item = rowItem,
            Surface = card,
            ApplySelection = selected =>
            {
                card.BackgroundColor = selected
                    ? c.SecondaryContainer.WithAlpha(0.45f)
                    : c.Surface;
                if (check is not null)
                    check.IsChecked = selected;
            },
        };

        _selectionVisuals.Add((rowItem, visual.ApplySelection));
        return visual;
    }

    private Control BuildCellContent(
        object rowItem,
        MavDataColumn column,
        MavTheme t,
        MavColorScheme c,
        bool headline)
    {
        if (MavVisualTreeHelpers.BuildTemplateControl(rowItem, column.CellTemplate, column.CellFactory)
            is { } builtCell)
        {
            return builtCell;
        }

        var text = new TextBlock
        {
            Text = column.ValueSelector(rowItem)?.ToString() ?? string.Empty,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = column.IsNumeric && !headline
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left,
            Foreground = MavTheme.BrushOf(c.OnSurface),
            TextWrapping = TextWrapping.Wrap,
        };

        if (headline)
            t.Typography.TitleMedium.ApplyTo(text);
        else
            t.Typography.BodyMedium.ApplyTo(text);

        return text;
    }

    private void ActivateRow(object rowItem)
    {
        RowClicked?.Invoke(this, rowItem);
        if (RowClickCommand is { } rCmd && rCmd.CanExecute(rowItem))
            rCmd.Execute(rowItem);

        if (!IsSelectable)
            return;

        if (!_selectedItems.Add(rowItem))
            _selectedItems.Remove(rowItem);
        RefreshSelectionVisuals();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AttachBody(StackPanel container, List<object> pageItems)
    {
        if (double.IsFinite(MaxBodyHeight) && MaxBodyHeight > 0)
        {
            _rowsScroll = new MavScrollViewer
            {
                MaxHeight = MaxBodyHeight,
                Content = container,
            };
            _rowsScroll.ScrollChanged += OnRowsScrollChanged;
            _tableStack.Children.Add(_rowsScroll);
        }
        else
        {
            _tableStack.Children.Add(container);
            // No inner viewport: render the whole page so every row stays reachable.
            while (_renderedRowCount < pageItems.Count)
                AppendRowsChunk();
        }
    }

    private void AppendRowsChunk()
    {
        if (_appendingRows || _pageItems is null || _rowsContainer is null)
            return;

        _appendingRows = true;
        try
        {
            var t = MavTheme.Current;
            var c = t.Color;
            var colOffset = IsSelectable ? 1 : 0;
            var end = Math.Min(_pageItems.Count, _renderedRowCount + RowChunkSize);

            for (var r = _renderedRowCount; r < end; r++)
            {
                var visual = _compactMode
                    ? CreateCard(_pageItems[r], t, c)
                    : CreateRow(_pageItems[r], r, _pageItems.Count, t, c, colOffset);
                _rowsContainer.Children.Add(visual.Surface);
                if (visual.Divider is not null)
                    _rowsContainer.Children.Add(visual.Divider);
                visual.Index = r;
                visual.Total = _pageItems.Count;
                _rowVisuals.Add(visual);
                IndexRow(visual);
            }

            _renderedRowCount = end;
        }
        finally
        {
            _appendingRows = false;
        }
    }

    private void OnRowsScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_rowsScroll is null || _pageItems is null || _renderedRowCount >= _pageItems.Count)
            return;

        var remaining = _rowsScroll.Extent.Height -
                        (_rowsScroll.Offset.Y + _rowsScroll.Viewport.Height);
        if (remaining < 400)
            AppendRowsChunk();
    }

    private RowVisual CreateRow(
        object rowItem,
        int r,
        int count,
        MavTheme t,
        MavColorScheme c,
        int colOffset)
    {
        var isRowSelected = _selectedItems.Contains(rowItem);
        var selectedBg = c.SecondaryContainer.WithAlpha(0.45f);
        var rowBg = isRowSelected ? selectedBg : BaseRowColor(r, c);

        var rowGrid = new Grid
        {
            ColumnDefinitions = BuildColumnDefinitions(),
            MinHeight = 52,
        };

        MavCheckBox? rowCheck = null;
        if (IsSelectable)
        {
            rowCheck = new MavCheckBox(isChecked: isRowSelected)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            rowCheck.CheckedChanged += (_, checkedVal) =>
            {
                if (_syncingSelection) return;

                if (checkedVal) _selectedItems.Add(rowItem);
                else _selectedItems.Remove(rowItem);
                SyncExternalSelectedItems();
                RefreshSelectionVisuals();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };
            rowGrid.Add(rowCheck, 0, 0);
        }

        for (var i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            Control cellContent;
            if (MavVisualTreeHelpers.BuildTemplateControl(rowItem, col.CellTemplate, col.CellFactory) is { } builtCell)
            {
                cellContent = builtCell;
            }
            else
            {
                var val = col.ValueSelector(rowItem);
                var tb = new TextBlock
                {
                    Text = val?.ToString() ?? string.Empty,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = col.IsNumeric ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    Foreground = MavTheme.BrushOf(c.OnSurface),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                t.Typography.BodyMedium.ApplyTo(tb);
                cellContent = tb;
            }

            var cellWrapper = new Border
            {
                Padding = new Thickness(16, 10),
                VerticalAlignment = VerticalAlignment.Center,
                Child = cellContent,
            };
            rowGrid.Add(cellWrapper, colOffset + i, 0);
        }

        var rowSurface = new MavInkSurface
        {
            CornerRadius = 0,
            FillColor = rowBg,
            RippleColor = c.OnSurface,
            Padding = new Thickness(0),
            Content = rowGrid,
        };
        rowSurface.Clicked += (_, _) => ActivateRow(rowItem);

        var divider = new MavDivider { IsVisible = r < count - 1 || ShowPagination };

        RowVisual visual = null!;
        visual = new RowVisual
        {
            Item = rowItem,
            Surface = rowSurface,
            Divider = divider,
            ApplySelection = selected =>
            {
                // The visual keeps the selection flag so index/alternating updates
                // can recompute the background without losing the selected state.
                visual.IsSelected = selected;
                rowSurface.FillColor = selected ? selectedBg : BaseRowColor(visual.Index, c);
                if (rowCheck is not null)
                    rowCheck.IsChecked = selected;
            },
            ApplyIndex = (index, total) =>
            {
                visual.Index = index;
                visual.Total = total;
                rowSurface.FillColor = visual.IsSelected
                    ? selectedBg
                    : BaseRowColor(index, c);
                divider.IsVisible = index < total - 1 || ShowPagination;
            },
        };

        visual.ApplySelection(isRowSelected);
        visual.ApplyIndex(r, count);
        _selectionVisuals.Add((rowItem, visual.ApplySelection));
        return visual;
    }

    private Color BaseRowColor(int index, MavColorScheme c) =>
        AlternatingRowBackground && index % 2 == 1 ? c.SurfaceContainerLowest : c.Surface;
}
