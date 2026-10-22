using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Standard Navigation Drawer (300dp width, 56dp pill items with 28dp radius, section headers, and dividers).
/// </summary>
public class MavNavigationDrawer : MavContentView, IEnumerable<MavDrawerItem>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavNavigationDrawer, int>(nameof(SelectedIndex), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavNavigationDrawer, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavNavigationDrawer, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavNavigationDrawer, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<double> DrawerWidthProperty =
        AvaloniaProperty.Register<MavNavigationDrawer, double>(nameof(DrawerWidth), 300.0);

    private readonly AvaloniaList<MavDrawerItem> _items = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavDrawerItem, EventHandler> _wiredItems = new(
        static (item, handler) => item.ItemChanged += handler,
        static (item, handler) => item.ItemChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly List<(int ItemIndex, MavInkSurface Pill, MavIcon Icon, TextBlock Label, TextBlock? Badge)> _pills = [];
    private readonly List<TextBlock> _headers = [];
    private readonly MavSurface _drawerSurface;
    private readonly StackPanel _stack;
    private readonly TextBlock _headline;
    private bool _suppressCollectionRebuild;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;

    static MavNavigationDrawer()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavNavigationDrawer>((d, e) => d.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavNavigationDrawer>((d, e) => d.OnSelectedItemChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MavNavigationDrawer>((d, e) => d.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        TitleProperty.Changed.AddClassHandler<MavNavigationDrawer>((d, e) =>
        {
            var val = e.GetNewValue<string>() ?? string.Empty;
            d._headline.Text = val;
            d._headline.IsVisible = !string.IsNullOrEmpty(val);
        });
        DrawerWidthProperty.Changed.AddClassHandler<MavNavigationDrawer>((d, e) =>
        {
            var w = e.GetNewValue<double>();
            d.Width = w;
            d._drawerSurface.Width = w;
        });
    }

    /// <summary>Creates an empty navigation drawer 300 dp wide.</summary>
    public MavNavigationDrawer() : this(null, 300)
    {
    }

    /// <summary>Creates a navigation drawer with an optional title and width.</summary>
    public MavNavigationDrawer(string? title = null, double width = 300)
    {
        Width = width;
        VerticalAlignment = VerticalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

        _headline = new TextBlock
        {
            Text = title ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(title),
            Margin = new Thickness(16, 16, 16, 8),
        };

        _stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Margin = new Thickness(12, 8),
        };
        _stack.Children.Add(_headline);

        _items.CollectionChanged += (_, e) =>
        {
            _wiredItems.HandleCollectionChanged(e);
            foreach (var item in _items)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, item);
                _wiredItems.Add(item, (_, _) => RebuildFromItems());
            }
            if (!_suppressCollectionRebuild)
                RebuildFromItems();
        };

        var scroll = new MavScrollViewer
        {
            Content = _stack,
        };

        _drawerSurface = new MavSurface
        {
            Width = width,
            Content = new MavSafeArea(scroll, MavSafeAreaEdges.Vertical),
        };
        _drawerSurface.SetCornerRadii(0, 16, 0, 16);

        Content = _drawerSurface;
        if (title is not null) Title = title;
        DrawerWidth = width;
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
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
            if (item is MavDrawerItem di)
                _items.Add(di);
            else
                _items.Add(new MavDrawerItem(item.ToString() ?? string.Empty, MavIcons.Folder));
        }
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value ?? string.Empty);
    }

    public string? Headline
    {
        get => Title;
        set => Title = value ?? string.Empty;
    }

    public double DrawerWidth
    {
        get => GetValue(DrawerWidthProperty);
        set => SetValue(DrawerWidthProperty, value);
    }

    [Content]
    public AvaloniaList<MavDrawerItem> Items => _items;

    /// <summary>Adds an item and returns the drawer for chaining.</summary>
    public MavNavigationDrawer AddItem(string label, string icon, string? badge = null, Action? onClick = null)
    {
        var item = new MavDrawerItem(label, icon, badge, onClick);
        _suppressCollectionRebuild = true;
        _items.Add(item);
        _suppressCollectionRebuild = false;
        AppendItemVisual(_items.Count - 1, item);
        ApplyTheme(MavTheme.Current);
        return this;
    }

    /// <summary>
    /// Adds an item to the drawer.
    /// </summary>
    public void Add(MavDrawerItem item) => _items.Add(item);

    /// <summary>
    /// Returns an enumerator over the drawer items.
    /// </summary>
    public IEnumerator<MavDrawerItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void RebuildFromItems()
    {
        _stack.Children.Clear();
        _stack.Children.Add(_headline);
        _pills.Clear();
        _headers.Clear();

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.ShowDividerBefore)
                AddDivider();
            if (!string.IsNullOrEmpty(item.SectionHeader))
                AddSectionHeader(item.SectionHeader);
            AppendItemVisual(i, item);
        }
        ApplyTheme(MavTheme.Current);
    }

    private void AppendItemVisual(int idx, MavDrawerItem item)
    {
        var ic = new MavIcon(item.Icon, 24) { UseThemeDefaultColor = false };
        var lbl = new TextBlock
        {
            Text = item.Label,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Add(ic, 0, 0);
        row.Add(lbl, 1, 0);

        TextBlock? badgeLbl = null;
        if (!string.IsNullOrEmpty(item.BadgeText))
        {
            badgeLbl = new TextBlock
            {
                Text = item.BadgeText,
                VerticalAlignment = VerticalAlignment.Center,
            };
            row.Add(badgeLbl, 2, 0);
        }

        var pill = new MavInkSurface
        {
            Height = 56,
            CornerRadius = 28,
            Padding = new Thickness(16, 0, 24, 0),
            Content = row,
        };
        pill.Clicked += (_, _) =>
        {
            SelectedIndex = idx;
            item.OnClick?.Invoke();
            item.RaiseClicked();
            if (item.Command is { } cmd && cmd.CanExecute(item.CommandParameter))
                cmd.Execute(item.CommandParameter);
        };

        _pills.Add((idx, pill, ic, lbl, badgeLbl));
        _stack.Children.Add(pill);
    }

    /// <summary>Adds a section heading and returns the drawer for chaining.</summary>
    public MavNavigationDrawer AddSectionHeader(string text)
    {
        var hdr = new TextBlock
        {
            Text = text,
            Margin = new Thickness(16, 14, 16, 6),
        };
        _headers.Add(hdr);
        _stack.Children.Add(hdr);
        ApplyTheme(MavTheme.Current);
        return this;
    }

    /// <summary>Adds a divider and returns the drawer for chaining.</summary>
    public MavNavigationDrawer AddDivider()
    {
        _stack.Children.Add(new MavDivider(16, 16) { Margin = new Thickness(16, 8) });
        return this;
    }

    private void OnSelectedIndexChanged(int idx)
    {
        if (_syncingSelection) return;
        _syncingSelection = true;
        object? item = null;
        if (idx >= 0 && idx < _rawItems.Count)
            item = _rawItems[idx];
        else if (idx >= 0 && idx < _items.Count)
            item = _items[idx];
        SetValue(SelectedItemProperty, item);
        _syncingSelection = false;
        ApplyTheme(MavTheme.Current);
        SelectedIndexChanged?.Invoke(this, idx);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MavDrawerItem di)
            idx = _items.IndexOf(di);
        else if (idx < 0 && item is string s)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Label, s, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
        }
        if (idx >= 0)
            SelectedIndex = idx;
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _drawerSurface.FillColor = c.SurfaceContainerLow;
        _headline.Apply(MavTypeRole.TitleSmall, t.Typography);
        _headline.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        foreach (var hdr in _headers)
        {
            hdr.Apply(MavTypeRole.LabelLarge, t.Typography);
            hdr.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        }

        foreach (var (idx, pill, ic, lbl, badge) in _pills)
        {
            var sel = idx == SelectedIndex;
            lbl.Apply(MavTypeRole.LabelLarge, t.Typography);
            lbl.FontWeight = sel ? FontWeight.SemiBold : FontWeight.Medium;

            var bg = sel ? c.SecondaryContainer : Colors.Transparent;
            var fg = sel ? c.OnSecondaryContainer : c.OnSurfaceVariant;

            pill.FillColor = bg;
            pill.RippleColor = fg;
            ic.Color = fg;
            lbl.Foreground = MavTheme.BrushOf(fg);
            if (badge is not null)
            {
                badge.Apply(MavTypeRole.LabelLarge, t.Typography);
                badge.Foreground = MavTheme.BrushOf(fg);
            }
        }
    }
}
