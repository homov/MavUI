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

    public MavBreadcrumbItem()
    {
    }

    public MavBreadcrumbItem(string text, string? icon = null, object? tag = null)
    {
        Text = text;
        Icon = icon;
        Tag = tag;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

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

    public void Add(string text, string? icon = null, object? tag = null)
        => Add(new MavBreadcrumbItem(text, icon, tag));

    public void Add(MavBreadcrumbItem item) => _items.Add(item);

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

    public void NavigateToIndex(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        while (_items.Count > index + 1)
            _items.RemoveAt(_items.Count - 1);
        _expandedOverflow = false;
        Rebuild();
    }

    public void Clear()
    {
        _items.Clear();
        _expandedOverflow = false;
        Rebuild();
    }

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

/// <summary>
/// Material Design 3 Pagination control with smart page windowing, ellipsis indicators,
/// first/previous/next/last navigation buttons, and TwoWay CurrentPage binding.
/// </summary>
public class MavPagination : MavContentView
{
    public static readonly StyledProperty<int> TotalPagesProperty =
        AvaloniaProperty.Register<MavPagination, int>(nameof(TotalPages), 1);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<MavPagination, int>(nameof(CurrentPage), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxVisiblePagesProperty =
        AvaloniaProperty.Register<MavPagination, int>(nameof(MaxVisiblePages), 5);

    public static readonly StyledProperty<bool> ShowFirstLastButtonsProperty =
        AvaloniaProperty.Register<MavPagination, bool>(nameof(ShowFirstLastButtons), true);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavPagination, ICommand?>(nameof(Command));

    private readonly StackPanel _row;

    public event EventHandler<int>? PageChanged;

    static MavPagination()
    {
        TotalPagesProperty.Changed.AddClassHandler<MavPagination>((p, _) => p.Rebuild());
        CurrentPageProperty.Changed.AddClassHandler<MavPagination>((p, e) =>
        {
            p.Rebuild();
            var page = e.GetNewValue<int>();
            p.PageChanged?.Invoke(p, page);
            if (p.Command?.CanExecute(page) == true)
                p.Command.Execute(page);
        });
        MaxVisiblePagesProperty.Changed.AddClassHandler<MavPagination>((p, _) => p.Rebuild());
        ShowFirstLastButtonsProperty.Changed.AddClassHandler<MavPagination>((p, _) => p.Rebuild());
    }

    public MavPagination() : this(10, 1)
    {
    }

    public MavPagination(int totalPages = 10, int currentPage = 1)
    {
        _row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Content = _row;

        TotalPages = Math.Max(1, totalPages);
        CurrentPage = Math.Clamp(currentPage, 1, TotalPages);

        MavTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public int TotalPages
    {
        get => GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, Math.Max(1, value));
    }

    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, Math.Clamp(value, 1, Math.Max(1, TotalPages)));
    }

    public int MaxVisiblePages
    {
        get => GetValue(MaxVisiblePagesProperty);
        set => SetValue(MaxVisiblePagesProperty, Math.Max(3, value));
    }

    public bool ShowFirstLastButtons
    {
        get => GetValue(ShowFirstLastButtonsProperty);
        set => SetValue(ShowFirstLastButtonsProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private void Rebuild()
    {
        _row.Children.Clear();
        var t = MavTheme.Current;
        var total = Math.Max(1, TotalPages);
        var current = Math.Clamp(CurrentPage, 1, total);

        if (ShowFirstLastButtons)
            _row.Children.Add(BuildNavButton(MavIcons.FirstPage, current > 1, () => CurrentPage = 1, t));

        _row.Children.Add(BuildNavButton(MavIcons.ChevronLeft, current > 1, () => CurrentPage = current - 1, t));

        foreach (var token in ComputePageTokens(total, current, Math.Max(3, MaxVisiblePages)))
        {
            if (token < 0)
            {
                var dots = new TextBlock
                {
                    Text = "…",
                    Width = 32,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = MavTheme.BrushOf(t.Color.OnSurfaceVariant),
                };
                dots.Apply(MavTypeRole.LabelLarge, t.Typography);
                _row.Children.Add(dots);
            }
            else
            {
                _row.Children.Add(BuildPagePill(token, token == current, t));
            }
        }

        _row.Children.Add(BuildNavButton(MavIcons.ChevronRight, current < total, () => CurrentPage = current + 1, t));

        if (ShowFirstLastButtons)
            _row.Children.Add(BuildNavButton(MavIcons.LastPage, current < total, () => CurrentPage = total, t));
    }

    private static List<int> ComputePageTokens(int total, int current, int maxVisible)
    {
        var result = new List<int>();
        if (total <= maxVisible + 2)
        {
            for (var i = 1; i <= total; i++) result.Add(i);
            return result;
        }

        var half = maxVisible / 2;
        var start = Math.Max(2, current - half);
        var end = Math.Min(total - 1, start + maxVisible - 1);
        start = Math.Max(2, end - maxVisible + 1);

        result.Add(1);
        if (start > 2) result.Add(-1);
        for (var i = start; i <= end; i++) result.Add(i);
        if (end < total - 1) result.Add(-1);
        result.Add(total);
        return result;
    }

    private Control BuildNavButton(string glyph, bool enabled, Action onClick, MavTheme t)
    {
        var c = t.Color;
        var icon = new MavIcon(glyph, 18)
        {
            UseThemeDefaultColor = false,
            Color = enabled ? c.OnSurface : c.OnSurface.WithAlpha(0.35f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var surf = new MavInkSurface
        {
            Width = 36,
            Height = 36,
            CornerRadius = 18,
            FillColor = Colors.Transparent,
            RippleColor = c.OnSurface,
            IsEnabled = enabled,
            Content = icon,
        };
        surf.Clicked += (_, _) =>
        {
            if (enabled) onClick();
        };
        return surf;
    }

    private Control BuildPagePill(int page, bool isSelected, MavTheme t)
    {
        var c = t.Color;
        var lbl = new TextBlock
        {
            Text = page.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = MavTheme.BrushOf(isSelected ? c.OnPrimary : c.OnSurface),
        };
        lbl.Apply(MavTypeRole.LabelLarge, t.Typography);

        var surf = new MavInkSurface
        {
            MinWidth = 36,
            Height = 36,
            CornerRadius = isSelected ? 12 : 18,
            Padding = new Thickness(10, 0),
            FillColor = isSelected ? c.Primary : c.SurfaceContainerLow,
            StrokeThickness = isSelected ? 0 : 1,
            StrokeColor = c.OutlineVariant.WithAlpha(0.5f),
            RippleColor = isSelected ? c.OnPrimary : c.Primary,
            Content = lbl,
        };
        surf.Clicked += (_, _) => CurrentPage = page;
        return surf;
    }
}
