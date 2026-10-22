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
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public sealed class MdDrawerItem : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MdDrawerItem, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MdDrawerItem, string>(nameof(Icon), MdIcons.Folder);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MdDrawerItem, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MdDrawerItem, string?>(nameof(BadgeText));

    public static readonly StyledProperty<string?> SectionHeaderProperty =
        AvaloniaProperty.Register<MdDrawerItem, string?>(nameof(SectionHeader));

    public static readonly StyledProperty<bool> ShowDividerBeforeProperty =
        AvaloniaProperty.Register<MdDrawerItem, bool>(nameof(ShowDividerBefore));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdDrawerItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdDrawerItem, object?>(nameof(CommandParameter));

    internal event EventHandler? ItemChanged;

    static MdDrawerItem()
    {
        LabelProperty.Changed.AddClassHandler<MdDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MdDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MdDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        SectionHeaderProperty.Changed.AddClassHandler<MdDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        ShowDividerBeforeProperty.Changed.AddClassHandler<MdDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? SelectedIcon
    {
        get => GetValue(SelectedIconProperty);
        set => SetValue(SelectedIconProperty, value);
    }

    public string? BadgeText
    {
        get => GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public string? SectionHeader
    {
        get => GetValue(SectionHeaderProperty);
        set => SetValue(SectionHeaderProperty, value);
    }

    public bool ShowDividerBefore
    {
        get => GetValue(ShowDividerBeforeProperty);
        set => SetValue(ShowDividerBeforeProperty, value);
    }

    public Action? OnClick { get; set; }
    public event EventHandler? Clicked;

    internal void RaiseClicked() => Clicked?.Invoke(this, EventArgs.Empty);

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public MdDrawerItem()
    {
    }

    public MdDrawerItem(
        string label,
        string icon,
        string? badgeText = null,
        Action? onClick = null)
    {
        Label = label;
        Icon = icon;
        BadgeText = badgeText;
        OnClick = onClick;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 Standard Navigation Drawer (300dp width, 56dp pill items with 28dp radius, section headers, and dividers).
/// </summary>
public class MdNavigationDrawer : MdContentView, IEnumerable<MdDrawerItem>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MdNavigationDrawer, int>(nameof(SelectedIndex), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MdNavigationDrawer, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdNavigationDrawer, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdNavigationDrawer, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<double> DrawerWidthProperty =
        AvaloniaProperty.Register<MdNavigationDrawer, double>(nameof(DrawerWidth), 300.0);

    private readonly AvaloniaList<MdDrawerItem> _items = new();
    private readonly List<object> _rawItems = [];
    private readonly MdItemEventWiring<MdDrawerItem, EventHandler> _wiredItems = new(
        static (item, handler) => item.ItemChanged += handler,
        static (item, handler) => item.ItemChanged -= handler);
    private readonly MdCollectionObserver _observer;
    private readonly List<(int ItemIndex, MdInkSurface Pill, MdIcon Icon, TextBlock Label, TextBlock? Badge)> _pills = [];
    private readonly List<TextBlock> _headers = [];
    private readonly MdSurface _drawerSurface;
    private readonly StackPanel _stack;
    private readonly TextBlock _headline;
    private bool _suppressCollectionRebuild;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;

    static MdNavigationDrawer()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MdNavigationDrawer>((d, e) => d.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MdNavigationDrawer>((d, e) => d.OnSelectedItemChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MdNavigationDrawer>((d, e) => d.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        TitleProperty.Changed.AddClassHandler<MdNavigationDrawer>((d, e) =>
        {
            var val = e.GetNewValue<string>() ?? string.Empty;
            d._headline.Text = val;
            d._headline.IsVisible = !string.IsNullOrEmpty(val);
        });
        DrawerWidthProperty.Changed.AddClassHandler<MdNavigationDrawer>((d, e) =>
        {
            var w = e.GetNewValue<double>();
            d.Width = w;
            d._drawerSurface.Width = w;
        });
    }

    public MdNavigationDrawer() : this(null, 300)
    {
    }

    public MdNavigationDrawer(string? title = null, double width = 300)
    {
        Width = width;
        VerticalAlignment = VerticalAlignment.Stretch;
        _observer = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

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
                MdVisualTreeHelpers.AttachLogicalChild(this, item);
                _wiredItems.Add(item, (_, _) => RebuildFromItems());
            }
            if (!_suppressCollectionRebuild)
                RebuildFromItems();
        };

        var scroll = new MdScrollViewer
        {
            Content = _stack,
        };

        _drawerSurface = new MdSurface
        {
            Width = width,
            Content = new MdSafeArea(scroll, MdSafeAreaEdges.Vertical),
        };
        _drawerSurface.SetCornerRadii(0, 16, 0, 16);

        Content = _drawerSurface;
        if (title is not null) Title = title;
        DrawerWidth = width;
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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
            if (item is MdDrawerItem di)
                _items.Add(di);
            else
                _items.Add(new MdDrawerItem(item.ToString() ?? string.Empty, MdIcons.Folder));
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
    public AvaloniaList<MdDrawerItem> Items => _items;

    public MdNavigationDrawer AddItem(string label, string icon, string? badge = null, Action? onClick = null)
    {
        var item = new MdDrawerItem(label, icon, badge, onClick);
        _suppressCollectionRebuild = true;
        _items.Add(item);
        _suppressCollectionRebuild = false;
        AppendItemVisual(_items.Count - 1, item);
        ApplyTheme(MdTheme.Current);
        return this;
    }

    public void Add(MdDrawerItem item) => _items.Add(item);

    public IEnumerator<MdDrawerItem> GetEnumerator() => _items.GetEnumerator();

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
        ApplyTheme(MdTheme.Current);
    }

    private void AppendItemVisual(int idx, MdDrawerItem item)
    {
        var ic = new MdIcon(item.Icon, 24) { UseThemeDefaultColor = false };
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

        var pill = new MdInkSurface
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

    public MdNavigationDrawer AddSectionHeader(string text)
    {
        var hdr = new TextBlock
        {
            Text = text,
            Margin = new Thickness(16, 14, 16, 6),
        };
        _headers.Add(hdr);
        _stack.Children.Add(hdr);
        ApplyTheme(MdTheme.Current);
        return this;
    }

    public MdNavigationDrawer AddDivider()
    {
        _stack.Children.Add(new MdDivider(16, 16) { Margin = new Thickness(16, 8) });
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
        ApplyTheme(MdTheme.Current);
        SelectedIndexChanged?.Invoke(this, idx);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MdDrawerItem di)
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

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _drawerSurface.FillColor = c.SurfaceContainerLow;
        _headline.Apply(MdTypeRole.TitleSmall, t.Typography);
        _headline.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);

        foreach (var hdr in _headers)
        {
            hdr.Apply(MdTypeRole.LabelLarge, t.Typography);
            hdr.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
        }

        foreach (var (idx, pill, ic, lbl, badge) in _pills)
        {
            var sel = idx == SelectedIndex;
            lbl.Apply(MdTypeRole.LabelLarge, t.Typography);
            lbl.FontWeight = sel ? FontWeight.SemiBold : FontWeight.Medium;

            var bg = sel ? c.SecondaryContainer : Colors.Transparent;
            var fg = sel ? c.OnSecondaryContainer : c.OnSurfaceVariant;

            pill.FillColor = bg;
            pill.RippleColor = fg;
            ic.Color = fg;
            lbl.Foreground = MdTheme.BrushOf(fg);
            if (badge is not null)
            {
                badge.Apply(MdTypeRole.LabelLarge, t.Typography);
                badge.Foreground = MdTheme.BrushOf(fg);
            }
        }
    }
}

/// <summary>
/// Material Design 3 Modal Navigation Drawer presented over a scrim with slide-in animation and swipe-left dismissal.
/// </summary>
public class MdModalNavigationDrawer : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdModalNavigationDrawer, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MdModalNavigationDrawer, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private readonly MdNavigationDrawer _drawer;
    private IDisposable? _handle;
    private double _dragStartX;
    private bool _dragging;
    private bool _syncingIsOpen;

    static MdModalNavigationDrawer()
    {
        TitleProperty.Changed.AddClassHandler<MdModalNavigationDrawer>((d, e) =>
            d._drawer.Title = e.GetNewValue<string>() ?? string.Empty);
        IsOpenProperty.Changed.AddClassHandler<MdModalNavigationDrawer>((d, e) =>
        {
            if (d._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                d.Show(d.Parent as Visual ?? d);
            else
                d.Close();
        });
    }

    public MdModalNavigationDrawer() : this(null, 300)
    {
    }

    public MdModalNavigationDrawer(string? title = null, double width = 300)
    {
        _drawer = new MdNavigationDrawer(title, width)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        MdVisualTreeHelpers.AttachLogicalChild(this, _drawer);
        if (title is not null) Title = title;
        _drawer.SelectedIndexChanged += (_, _) => Close();
        _drawer.PointerPressed += OnDragPressed;
        _drawer.PointerMoved += OnDragMoved;
        _drawer.PointerReleased += OnDragReleased;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    public MdNavigationDrawer Drawer => _drawer;

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Headline
    {
        get => Title;
        set => Title = value;
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    [Content]
    public AvaloniaList<MdDrawerItem> Items => _drawer.Items;

    public MdModalNavigationDrawer AddItem(string label, string icon, string? badge = null, Action? onClick = null)
    {
        _drawer.AddItem(label, icon, badge, onClick);
        return this;
    }

    public MdModalNavigationDrawer AddSectionHeader(string text)
    {
        _drawer.AddSectionHeader(text);
        return this;
    }

    public MdModalNavigationDrawer AddDivider()
    {
        _drawer.AddDivider();
        return this;
    }

    public void Show(Visual? anchor = null)
    {
        Close();
        var c = MdTheme.Current.Color;
        var pushAnchor = anchor ?? (this.Parent as Visual) ?? this;

        // The drawer stretches vertically; on a narrow window (or after rotation to
        // portrait) its fixed width has to be clamped to the overlay viewport.
        void Reflow()
        {
            var size = MdOverlay.GetOverlaySize(pushAnchor);
            var insets = MdOverlay.SafeAreaOf(pushAnchor);
            var viewportWidth = Math.Max(0, size.Width - insets.Left - insets.Right);
            _drawer.Width = Math.Max(0, Math.Min(_drawer.DrawerWidth, viewportWidth));
        }

        Reflow();
        _handle = MdOverlay.Show(
            pushAnchor,
            _drawer,
            c.Scrim.WithAlpha(0.32f),
            Close,
            onViewportChanged: Reflow);
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;
        _drawer.SetTranslationX(-300);
        MdMotion.Animate(_drawer, "mdraw_in", -300, 0, MdMotion.Medium2, MdMotion.CubicOut, v =>
        {
            _drawer.SetTranslationX(v);
        });
    }

    public void Close()
    {
        if (_handle is null) return;
        var h = _handle;
        _handle = null;
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;
        MdMotion.Animate(_drawer, "mdraw_out", _drawer.GetTranslationX(), -300, MdMotion.Short4, MdMotion.CubicIn, v =>
        {
            _drawer.SetTranslationX(v);
        }, h.Dispose);
    }

    private void OnDragPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        _dragStartX = e.GetPosition(_drawer.Parent as Visual).X;
    }

    private void OnDragMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        var curX = e.GetPosition(_drawer.Parent as Visual).X;
        var dx = Math.Min(0, curX - _dragStartX);
        if (dx < -6)
            _drawer.SetTranslationX(dx);
    }

    private void OnDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        if (_drawer.GetTranslationX() < -80)
        {
            Close();
        }
        else if (_drawer.GetTranslationX() < 0)
        {
            MdMotion.Animate(_drawer, "mdraw_snap", _drawer.GetTranslationX(), 0, MdMotion.Short4, MdMotion.CubicOut, v =>
            {
                _drawer.SetTranslationX(v);
            });
        }
    }
}
