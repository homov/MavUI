using System.Collections;
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
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public sealed class MdDataColumn : StyledElement
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<MdDataColumn, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string?> PropertyNameProperty =
        AvaloniaProperty.Register<MdDataColumn, string?>(nameof(PropertyName));

    public static readonly StyledProperty<bool> IsNumericProperty =
        AvaloniaProperty.Register<MdDataColumn, bool>(nameof(IsNumeric));

    public static readonly StyledProperty<bool> IsSortableProperty =
        AvaloniaProperty.Register<MdDataColumn, bool>(nameof(IsSortable), true);

    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<MdDataColumn, IDataTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<GridLength> WidthProperty =
        AvaloniaProperty.Register<MdDataColumn, GridLength>(nameof(Width), new GridLength(1, GridUnitType.Star));

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

    public MdDataColumn()
    {
    }

    public MdDataColumn(
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
                        "root those properties or use MdDataColumn.ValueSelector.")]
    private object? ResolveByPropertyName(object row)
    {
        if (row is null) return null;
        var propName = !string.IsNullOrEmpty(PropertyName) ? PropertyName : Header;
        if (string.IsNullOrEmpty(propName)) return row;
        var prop = MdPropertyCache.Get(row.GetType(), propName);
        return prop?.GetValue(row);
    }
}

/// <summary>
/// Row presentation for <see cref="MdDataTable"/>.
/// </summary>
public enum MdDataTableLayout
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
public class MdDataTable : MdContentView, IEnumerable<MdDataColumn>
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdDataTable, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> IsSelectableProperty =
        AvaloniaProperty.Register<MdDataTable, bool>(nameof(IsSelectable));

    public static readonly StyledProperty<bool> ShowPaginationProperty =
        AvaloniaProperty.Register<MdDataTable, bool>(nameof(ShowPagination), true);

    public static readonly StyledProperty<int> PageSizeProperty =
        AvaloniaProperty.Register<MdDataTable, int>(nameof(PageSize), 5);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<MdDataTable, int>(nameof(CurrentPage), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> AlternatingRowBackgroundProperty =
        AvaloniaProperty.Register<MdDataTable, bool>(nameof(AlternatingRowBackground));

    public static readonly StyledProperty<string> FilterTextProperty =
        AvaloniaProperty.Register<MdDataTable, string>(nameof(FilterText), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> EmptyTextProperty =
        AvaloniaProperty.Register<MdDataTable, string>(nameof(EmptyText), "No matching records");

    public static readonly StyledProperty<double> MaxBodyHeightProperty =
        AvaloniaProperty.Register<MdDataTable, double>(nameof(MaxBodyHeight), double.PositiveInfinity);

    public static readonly StyledProperty<MdDataTableLayout> LayoutProperty =
        AvaloniaProperty.Register<MdDataTable, MdDataTableLayout>(nameof(Layout), MdDataTableLayout.Auto);

    public static readonly StyledProperty<double> CompactBreakpointProperty =
        AvaloniaProperty.Register<MdDataTable, double>(nameof(CompactBreakpoint), 600.0);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MdDataTable, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList?> SelectedItemsSourceProperty =
        AvaloniaProperty.Register<MdDataTable, IList?>(nameof(SelectedItemsSource));

    public static readonly StyledProperty<ICommand?> RowClickCommandProperty =
        AvaloniaProperty.Register<MdDataTable, ICommand?>(nameof(RowClickCommand));

    private readonly AvaloniaList<MdDataColumn> _columns = new();
    private readonly List<object> _allItems = [];
    private readonly HashSet<object> _selectedItems = [];
    private readonly MdCollectionObserver _observer;
    private readonly Border _cardBorder;
    private readonly Border _borderOverlay;
    private readonly StackPanel _tableStack;
    private readonly Grid _footerGrid;
    private readonly TextBlock _selectionCountLabel;
    private readonly TextBlock _pageRangeLabel;
    private readonly MdIconButton _prevPageBtn;
    private readonly MdIconButton _nextPageBtn;
    private int _sortColumnIndex = -1;
    private bool _sortAscending = true;
    private bool _syncingSelection;

    // Incremental row rendering: rows are appended in chunks while the body scrolls,
    // so a 1000+ row source does not build all children on the first layout.
    private const int RowChunkSize = 100;
    private List<object>? _pageItems;
    private StackPanel? _rowsContainer;
    private ScrollViewer? _rowsScroll;
    private int _renderedRowCount;
    private bool _appendingRows;
    private bool _compactMode;
    private bool _lastCompact;

    public event EventHandler? SelectionChanged;
    public event EventHandler<object>? RowClicked;

    static MdDataTable()
    {
        ItemsSourceProperty.Changed.AddClassHandler<MdDataTable>((t, e) => t.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        SelectedItemProperty.Changed.AddClassHandler<MdDataTable>((t, e) => t.OnSelectedItemChanged(e.GetNewValue<object?>()));
        IsSelectableProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
        ShowPaginationProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
        PageSizeProperty.Changed.AddClassHandler<MdDataTable>((t, _) =>
        {
            t.CurrentPage = 0;
            t.RebuildTable();
        });
        CurrentPageProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
        AlternatingRowBackgroundProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
        FilterTextProperty.Changed.AddClassHandler<MdDataTable>((t, _) =>
        {
            t.CurrentPage = 0;
            t.RebuildTable();
        });
        EmptyTextProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
        MaxBodyHeightProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
        LayoutProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
        CompactBreakpointProperty.Changed.AddClassHandler<MdDataTable>((t, _) => t.RebuildTable());
    }

    public MdDataTable()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _columns.CollectionChanged += (_, _) =>
        {
            foreach (var col in _columns)
                MdVisualTreeHelpers.AttachLogicalChild(this, col);
            RebuildTable();
        };
        _observer = new MdCollectionObserver(() => ReloadItems(ItemsSource));

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

        _prevPageBtn = new MdIconButton(MdIcons.ChevronLeft);
        MdTooltip.SetText(_prevPageBtn, MdLocalization.PreviousPage);
        _prevPageBtn.Clicked += (_, _) =>
        {
            if (CurrentPage > 0) CurrentPage--;
        };

        _nextPageBtn = new MdIconButton(MdIcons.ChevronRight);
        MdTooltip.SetText(_nextPageBtn, MdLocalization.NextPage);
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
        MdTheme.Observe(this, (self, _) => self.RebuildTable());

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
    /// Row presentation: <see cref="MdDataTableLayout.Auto"/> switches to cards on
    /// hosts narrower than <see cref="CompactBreakpoint"/>.
    /// </summary>
    public MdDataTableLayout Layout
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
        MdDataTableLayout.Cards => true,
        MdDataTableLayout.Table => false,
        _ => Bounds.Width > 0 && Bounds.Width < CompactBreakpoint,
    };

    /// <summary>Resolved presentation used by the last rebuild.</summary>
    public MdDataTableLayout EffectiveLayout =>
        IsCompact ? MdDataTableLayout.Cards : MdDataTableLayout.Table;

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
        RebuildTable();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
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
    public AvaloniaList<MdDataColumn> Columns => _columns;
    public IReadOnlyCollection<object> SelectedItems => _selectedItems;
    public int TotalPages
    {
        get
        {
            var count = GetFilteredItemsCount();
            return count == 0 ? 1 : (int)Math.Ceiling(count / (double)Math.Max(1, PageSize));
        }
    }

    public MdDataTable AddColumn(
        string header,
        Func<object, object?> valueSelector,
        bool isNumeric = false,
        bool isSortable = true,
        Func<object, Control>? cellTemplate = null,
        GridLength? width = null)
    {
        _columns.Add(new MdDataColumn(header, valueSelector, isNumeric, isSortable, cellTemplate, width));
        return this;
    }

    public MdDataTable AddColumn(MdDataColumn column)
    {
        _columns.Add(column);
        return this;
    }

    public void Add(MdDataColumn column) => _columns.Add(column);

    public IEnumerator<MdDataColumn> GetEnumerator() => _columns.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void ClearSelection()
    {
        if (_selectedItems.Count == 0) return;
        _selectedItems.Clear();
        RebuildTable();
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
        _selectedItems.RemoveWhere(x => !_allItems.Contains(x));
        if (CurrentPage >= TotalPages)
            CurrentPage = Math.Max(0, TotalPages - 1);
        RebuildTable();
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
        var t = MdTheme.Current;
        var c = t.Color;

        _cardBorder.Background = MdTheme.BrushOf(c.Surface);
        _borderOverlay.BorderBrush = MdTheme.BrushOf(c.OutlineVariant);

        if (_rowsScroll is not null)
        {
            _rowsScroll.ScrollChanged -= OnRowsScrollChanged;
            _rowsScroll = null;
        }
        _rowsContainer = null;
        _pageItems = null;
        _renderedRowCount = 0;
        _appendingRows = false;

        _tableStack.Children.Clear();
        if (_columns.Count == 0) return;

        var ordered = GetOrderedItems();
        int pageSize = ShowPagination ? Math.Max(1, PageSize) : Math.Max(1, ordered.Count);
        int page = ShowPagination ? Math.Clamp(CurrentPage, 0, Math.Max(0, TotalPages - 1)) : 0;
        var pageItems = ShowPagination
            ? ordered.Skip(page * pageSize).Take(pageSize).ToList()
            : ordered;

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
            Background = MdTheme.BrushOf(c.SurfaceContainerLow),
        };

        int colOffset = 0;
        if (IsSelectable)
        {
            var allSelected = _allItems.Count > 0 && _selectedItems.Count == _allItems.Count;
            var someSelected = _selectedItems.Count > 0 && !allSelected;
            var masterCheck = new MdCheckBox(isChecked: allSelected, isIndeterminate: someSelected)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsThreeState = someSelected,
            };
            masterCheck.StateChanged += (_, _) =>
            {
                if (allSelected)
                    _selectedItems.Clear();
                else
                {
                    foreach (var it in _allItems)
                        _selectedItems.Add(it);
                }
                RebuildTable();
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
                Foreground = MdTheme.BrushOf(isSorted ? c.Primary : c.OnSurfaceVariant),
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
                var sortIcon = new MdIcon(_sortAscending ? MdIcons.ArrowUpward : MdIcons.ArrowDownward)
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
        _tableStack.Children.Add(new MdDivider());

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
                Foreground = MdTheme.BrushOf(c.OnSurfaceVariant),
            };
            t.Typography.BodyMedium.ApplyTo(emptyLbl);
            rowsContainer.Children.Add(emptyLbl);
            if (ShowPagination)
                rowsContainer.Children.Add(new MdDivider());
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
        MdTheme t,
        MdColorScheme c)
    {
        _footerGrid.IsVisible = ShowPagination || (IsSelectable && _selectedItems.Count > 0);
        _footerGrid.Background = MdTheme.BrushOf(c.SurfaceContainerLow);

        t.Typography.LabelMedium.ApplyTo(_selectionCountLabel);
        _selectionCountLabel.Foreground = MdTheme.BrushOf(c.Primary);
        _selectionCountLabel.Text = IsSelectable && _selectedItems.Count > 0
            ? MdLocalization.Format(
                MdLocalization.Keys.SelectedCountFormat,
                "{0} selected",
                _selectedItems.Count)
            : string.Empty;

        t.Typography.BodySmall.ApplyTo(_pageRangeLabel);
        _pageRangeLabel.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
        if (ordered.Count == 0)
        {
            _pageRangeLabel.Text = MdLocalization.EmptyPageRange;
        }
        else
        {
            int startIdx = page * pageSize + 1;
            int endIdx = Math.Min(ordered.Count, startIdx + pageItems.Count - 1);
            _pageRangeLabel.Text = MdLocalization.Format(
                MdLocalization.Keys.PageRangeFormat,
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

    private void BuildCompactBody(List<object> pageItems, MdTheme t, MdColorScheme c)
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
                Foreground = MdTheme.BrushOf(c.OnSurfaceVariant),
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

    private Control? BuildCompactSortBar(MdTheme t, MdColorScheme c)
    {
        var sortable = new List<(int Index, MdDataColumn Column)>();
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
            var chip = new MdChip(column.Header, MdChipKind.Filter, selected: selected)
            {
                Margin = new Thickness(0, 0, 8, 8),
            };
            if (selected)
                chip.Icon = _sortAscending ? MdIcons.ArrowUpward : MdIcons.ArrowDownward;

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

    private void BuildCard(StackPanel container, List<object> pageItems, int r, MdTheme t, MdColorScheme c)
    {
        var rowItem = pageItems[r];
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

        if (IsSelectable)
        {
            var check = new MdCheckBox(isChecked: isSelected);
            MdAutomation.SetName(check, MdLocalization.SelectRow);
            check.CheckedChanged += (_, checkedValue) =>
            {
                if (checkedValue) _selectedItems.Add(rowItem);
                else _selectedItems.Remove(rowItem);
                SyncExternalSelectedItems();
                RebuildTable();
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
                Foreground = MdTheme.BrushOf(c.OnSurfaceVariant),
                MinWidth = 88,
                TextWrapping = TextWrapping.Wrap,
            };
            t.Typography.LabelSmall.ApplyTo(label);

            var value = BuildCellContent(rowItem, column, t, c, headline: false);
            line.Add(label, 0, 0);
            line.Add(value, 1, 0);
            body.Children.Add(line);
        }

        var card = new MdCard(MdCardVariant.Outlined, body, interactive: true)
        {
            Margin = new Thickness(0),
            CardPadding = new Thickness(16),
        };
        card.BackgroundColor = isSelected
            ? c.SecondaryContainer.WithAlpha(0.45f)
            : c.Surface;
        card.Clicked += (_, _) => ActivateRow(rowItem);

        container.Children.Add(card);
    }

    private Control BuildCellContent(
        object rowItem,
        MdDataColumn column,
        MdTheme t,
        MdColorScheme c,
        bool headline)
    {
        if (MdVisualTreeHelpers.BuildTemplateControl(rowItem, column.CellTemplate, column.CellFactory)
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
            Foreground = MdTheme.BrushOf(c.OnSurface),
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
        RebuildTable();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AttachBody(StackPanel container, List<object> pageItems)
    {
        if (double.IsFinite(MaxBodyHeight) && MaxBodyHeight > 0)
        {
            _rowsScroll = new MdScrollViewer
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
            var t = MdTheme.Current;
            var c = t.Color;
            var colOffset = IsSelectable ? 1 : 0;
            var end = Math.Min(_pageItems.Count, _renderedRowCount + RowChunkSize);

            for (var r = _renderedRowCount; r < end; r++)
            {
                if (_compactMode)
                    BuildCard(_rowsContainer, _pageItems, r, t, c);
                else
                    BuildRow(_rowsContainer, _pageItems, r, t, c, colOffset);
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

    private void BuildRow(
        StackPanel container,
        List<object> pageItems,
        int r,
        MdTheme t,
        MdColorScheme c,
        int colOffset)
    {
        var rowItem = pageItems[r];
        var isRowSelected = _selectedItems.Contains(rowItem);

        var rowBg = isRowSelected
            ? c.SecondaryContainer.WithAlpha(0.45f)
            : (AlternatingRowBackground && r % 2 == 1 ? c.SurfaceContainerLowest : c.Surface);

        var rowGrid = new Grid
        {
            ColumnDefinitions = BuildColumnDefinitions(),
            MinHeight = 52,
        };

        if (IsSelectable)
        {
            var rowCheck = new MdCheckBox(isChecked: isRowSelected)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            rowCheck.CheckedChanged += (_, checkedVal) =>
            {
                if (checkedVal) _selectedItems.Add(rowItem);
                else _selectedItems.Remove(rowItem);
                SyncExternalSelectedItems();
                RebuildTable();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };
            rowGrid.Add(rowCheck, 0, 0);
        }

        for (var i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            Control cellContent;
            if (MdVisualTreeHelpers.BuildTemplateControl(rowItem, col.CellTemplate, col.CellFactory) is { } builtCell)
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
                    Foreground = MdTheme.BrushOf(c.OnSurface),
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

        var rowSurface = new MdInkSurface
        {
            CornerRadius = 0,
            FillColor = rowBg,
            RippleColor = c.OnSurface,
            Padding = new Thickness(0),
            Content = rowGrid,
        };
        rowSurface.Clicked += (_, _) => ActivateRow(rowItem);

        container.Children.Add(rowSurface);
        if (r < pageItems.Count - 1 || ShowPagination)
            container.Children.Add(new MdDivider());
    }
}
