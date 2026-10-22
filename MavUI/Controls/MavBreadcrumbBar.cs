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

/// <summary>
/// Material Design 3 Breadcrumb navigation bar with interactive path segments,
/// chevron separators, overflow ellipsis expansion, and C# collection initializer support.
/// </summary>
public class MavBreadcrumbBar : MavContentView, IEnumerable<MavBreadcrumbItem>
{
    public static readonly StyledProperty<int> MaxVisibleItemsProperty =
        AvaloniaProperty.Register<MavBreadcrumbBar, int>(nameof(MaxVisibleItems), 5);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavBreadcrumbBar, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavBreadcrumbBar, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> ItemClickCommandProperty =
        AvaloniaProperty.Register<MavBreadcrumbBar, ICommand?>(nameof(ItemClickCommand));

    private readonly AvaloniaList<MavBreadcrumbItem> _items = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavBreadcrumbItem, EventHandler> _wiredItems = new(
        static (item, handler) => item.ItemChanged += handler,
        static (item, handler) => item.ItemChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly WrapPanel _panel;
    private bool _expandedOverflow;

    public event EventHandler<(int Index, MavBreadcrumbItem Item)>? ItemClicked;

    static MavBreadcrumbBar()
    {
        MaxVisibleItemsProperty.Changed.AddClassHandler<MavBreadcrumbBar>((b, _) => b.Rebuild());
        ItemsSourceProperty.Changed.AddClassHandler<MavBreadcrumbBar>((b, e) => b.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    /// <summary>Creates an empty breadcrumb bar.</summary>
    public MavBreadcrumbBar()
    {
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _items.CollectionChanged += (_, e) =>
        {
            _wiredItems.HandleCollectionChanged(e);
            foreach (var item in _items)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, item);
                _wiredItems.Add(item, (_, _) => Rebuild());
            }
            _expandedOverflow = false;
            Rebuild();
        };
        _panel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Content = _panel;
        MavTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public int MaxVisibleItems
    {
        get => GetValue(MaxVisibleItemsProperty);
        set => SetValue(MaxVisibleItemsProperty, Math.Max(2, value));
    }

    [Content]
    public AvaloniaList<MavBreadcrumbItem> Items => _items;

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public ICommand? ItemClickCommand
    {
        get => GetValue(ItemClickCommandProperty);
        set => SetValue(ItemClickCommandProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        RebuildFromItemsSource(src);
    }

    private void RebuildFromItemsSource(IEnumerable? src)
    {
        if (src is null) return;
        _items.Clear();
        _rawItems.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MavBreadcrumbItem bi)
                _items.Add(bi);
            else
                _items.Add(new MavBreadcrumbItem(item.ToString() ?? string.Empty));
        }
    }

    /// <summary>Appends an item to the path.</summary>
    public void Add(string text, string? icon = null, object? tag = null)
        => Add(new MavBreadcrumbItem(text, icon, tag));

    /// <summary>
    /// Adds a breadcrumb item to the end of the bar.
    /// </summary>
    public void Add(MavBreadcrumbItem item) => _items.Add(item);

    /// <summary>Replaces the whole path with the given labels.</summary>
    public void SetPath(params string[] segments)
    {
        _items.Clear();
        foreach (var s in segments)
        {
            if (!string.IsNullOrWhiteSpace(s))
                _items.Add(new MavBreadcrumbItem(s.Trim()));
        }
        _expandedOverflow = false;
        Rebuild();
    }

    /// <summary>Removes the items after <paramref name="index"/> and makes that item the current location.</summary>
    public void NavigateToIndex(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        while (_items.Count > index + 1)
            _items.RemoveAt(_items.Count - 1);
        _expandedOverflow = false;
        Rebuild();
    }

    /// <summary>Removes all items from the path.</summary>
    public void Clear()
    {
        _items.Clear();
        _expandedOverflow = false;
        Rebuild();
    }

    /// <summary>
    /// Returns an enumerator over the breadcrumb items.
    /// </summary>
    public IEnumerator<MavBreadcrumbItem> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _panel.Children.Clear();
        if (_items.Count == 0) return;

        var t = MavTheme.Current;
        var c = t.Color;
        var maxVis = Math.Max(2, MaxVisibleItems);
        var needsCollapse = !_expandedOverflow && _items.Count > maxVis;

        if (!needsCollapse)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (i > 0) _panel.Children.Add(BuildSeparator(c));
                _panel.Children.Add(BuildCrumb(i, _items[i], i == _items.Count - 1, t));
            }
        }
        else
        {
            // First item + ellipsis + tail items
            _panel.Children.Add(BuildCrumb(0, _items[0], false, t));
            _panel.Children.Add(BuildSeparator(c));
            _panel.Children.Add(BuildEllipsisButton(t));

            var tailCount = Math.Max(1, maxVis - 1);
            var startIdx = Math.Max(1, _items.Count - tailCount);
            for (var i = startIdx; i < _items.Count; i++)
            {
                _panel.Children.Add(BuildSeparator(c));
                _panel.Children.Add(BuildCrumb(i, _items[i], i == _items.Count - 1, t));
            }
        }
    }

    private Control BuildSeparator(MavColorScheme c) => new MavIcon(MavIcons.ChevronRight, 18)
    {
        UseThemeDefaultColor = false,
        Color = c.OnSurfaceVariant.WithAlpha(0.7f),
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(2, 0),
    };

    private Control BuildEllipsisButton(MavTheme t)
    {
        var c = t.Color;
        var lbl = new TextBlock
        {
            Text = "•••",
            Foreground = MavTheme.BrushOf(c.OnSurfaceVariant),
            VerticalAlignment = VerticalAlignment.Center,
        };
        lbl.Apply(MavTypeRole.LabelLarge, t.Typography);

        var surf = new MavInkSurface
        {
            CornerRadius = 8,
            Padding = new Thickness(8, 4),
            FillColor = c.SurfaceContainerHigh,
            RippleColor = c.OnSurface,
            VerticalAlignment = VerticalAlignment.Center,
            Content = lbl,
        };
        surf.Clicked += (_, _) =>
        {
            _expandedOverflow = true;
            Rebuild();
        };
        return surf;
    }

    private Control BuildCrumb(int index, MavBreadcrumbItem item, bool isCurrent, MavTheme t)
    {
        var c = t.Color;
        var fg = isCurrent ? c.OnSurface : c.OnSurfaceVariant;

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (!string.IsNullOrEmpty(item.Icon))
        {
            row.Children.Add(new MavIcon(item.Icon, 16)
            {
                UseThemeDefaultColor = false,
                Color = isCurrent ? c.Primary : fg,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        var lbl = new TextBlock
        {
            Text = item.Text,
            Foreground = MavTheme.BrushOf(fg),
            VerticalAlignment = VerticalAlignment.Center,
        };
        lbl.Apply(isCurrent ? MavTypeRole.TitleSmall : MavTypeRole.LabelLarge, t.Typography);
        row.Children.Add(lbl);

        var surf = new MavInkSurface
        {
            CornerRadius = 8,
            Padding = new Thickness(8, 4),
            FillColor = isCurrent ? c.SecondaryContainer.WithAlpha(0.55f) : Colors.Transparent,
            RippleColor = c.Primary,
            VerticalAlignment = VerticalAlignment.Center,
            Content = row,
        };
        var captured = index;
        surf.Clicked += (_, _) =>
        {
            var raw = captured >= 0 && captured < _rawItems.Count ? _rawItems[captured] : (item.Tag ?? item);
            SetValue(SelectedItemProperty, raw);
            ItemClicked?.Invoke(this, (captured, item));
            var param = item.Tag ?? (object)captured;
            if (ItemClickCommand is { } cmd && cmd.CanExecute(param))
                cmd.Execute(param);
        };
        return surf;
    }
}
