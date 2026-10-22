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

/// <summary>Entry of a <see cref="MavNavigationDrawer"/>: a destination, section header or divider.</summary>
public sealed class MavDrawerItem : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavDrawerItem, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavDrawerItem, string>(nameof(Icon), MavIcons.Folder);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavDrawerItem, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavDrawerItem, string?>(nameof(BadgeText));

    public static readonly StyledProperty<string?> SectionHeaderProperty =
        AvaloniaProperty.Register<MavDrawerItem, string?>(nameof(SectionHeader));

    public static readonly StyledProperty<bool> ShowDividerBeforeProperty =
        AvaloniaProperty.Register<MavDrawerItem, bool>(nameof(ShowDividerBefore));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavDrawerItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavDrawerItem, object?>(nameof(CommandParameter));

    internal event EventHandler? ItemChanged;

    static MavDrawerItem()
    {
        LabelProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        SectionHeaderProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        ShowDividerBeforeProperty.Changed.AddClassHandler<MavDrawerItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
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

    /// <summary>Creates an empty drawer entry.</summary>
    public MavDrawerItem()
    {
    }

    /// <summary>Creates a drawer entry with label, icon, optional badge and click handler.</summary>
    public MavDrawerItem(
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

    public void Add(MavDrawerItem item) => _items.Add(item);

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

/// <summary>
/// Material Design 3 Modal Navigation Drawer presented over a scrim with slide-in animation and swipe-left dismissal.
/// </summary>
public class MavModalNavigationDrawer : Control
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavModalNavigationDrawer, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavModalNavigationDrawer, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    private readonly MavNavigationDrawer _drawer;
    private IDisposable? _handle;
    private double _dragStartX;
    private bool _dragging;
    private bool _syncingIsOpen;

    static MavModalNavigationDrawer()
    {
        TitleProperty.Changed.AddClassHandler<MavModalNavigationDrawer>((d, e) =>
            d._drawer.Title = e.GetNewValue<string>() ?? string.Empty);
        IsOpenProperty.Changed.AddClassHandler<MavModalNavigationDrawer>((d, e) =>
        {
            if (d._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                d.Show(d.Parent as Visual ?? d);
            else
                d.Close();
        });
    }

    /// <summary>Creates an empty modal navigation drawer 300 dp wide.</summary>
    public MavModalNavigationDrawer() : this(null, 300)
    {
    }

    /// <summary>Creates a modal navigation drawer with an optional title and width.</summary>
    public MavModalNavigationDrawer(string? title = null, double width = 300)
    {
        _drawer = new MavNavigationDrawer(title, width)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        MavVisualTreeHelpers.AttachLogicalChild(this, _drawer);
        if (title is not null) Title = title;
        _drawer.SelectedIndexChanged += (_, _) => Close();
        _drawer.PointerPressed += OnDragPressed;
        _drawer.PointerMoved += OnDragMoved;
        _drawer.PointerReleased += OnDragReleased;
        _drawer.PointerCaptureLost += (_, _) => _dragging = false;
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    public MavNavigationDrawer Drawer => _drawer;

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
    public AvaloniaList<MavDrawerItem> Items => _drawer.Items;

    /// <summary>Adds an item and returns the modal drawer for chaining.</summary>
    public MavModalNavigationDrawer AddItem(string label, string icon, string? badge = null, Action? onClick = null)
    {
        _drawer.AddItem(label, icon, badge, onClick);
        return this;
    }

    /// <summary>Adds a section heading and returns the modal drawer for chaining.</summary>
    public MavModalNavigationDrawer AddSectionHeader(string text)
    {
        _drawer.AddSectionHeader(text);
        return this;
    }

    /// <summary>Adds a divider and returns the modal drawer for chaining.</summary>
    public MavModalNavigationDrawer AddDivider()
    {
        _drawer.AddDivider();
        return this;
    }

    /// <summary>Shows the modal drawer over <paramref name="anchor"/> with a scrim behind it.</summary>
    public void Show(Visual? anchor = null)
    {
        Close();
        var c = MavTheme.Current.Color;
        var pushAnchor = anchor ?? (this.Parent as Visual) ?? this;

        // The drawer stretches vertically; on a narrow window (or after rotation to
        // portrait) its fixed width has to be clamped to the overlay viewport.
        void Reflow()
        {
            var size = MavOverlay.GetOverlaySize(pushAnchor);
            var insets = MavOverlay.SafeAreaOf(pushAnchor);
            var viewportWidth = Math.Max(0, size.Width - insets.Left - insets.Right);
            _drawer.Width = Math.Max(0, Math.Min(_drawer.DrawerWidth, viewportWidth));
        }

        Reflow();
        _handle = MavOverlay.Show(
            pushAnchor,
            _drawer,
            c.Scrim.WithAlpha(0.32f),
            Close,
            onViewportChanged: Reflow);
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;
        _drawer.SetTranslationX(-300);
        MavMotion.Animate(_drawer, "mdraw_in", -300, 0, MavMotion.Medium2, MavMotion.CubicOut, v =>
        {
            _drawer.SetTranslationX(v);
        });
    }

    /// <summary>Hides the modal drawer and removes its scrim.</summary>
    public void Close()
    {
        if (_handle is null) return;
        var h = _handle;
        _handle = null;
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;
        MavMotion.Animate(_drawer, "mdraw_out", _drawer.GetTranslationX(), -300, MavMotion.Short4, MavMotion.CubicIn, v =>
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

        // Drag only while the button is really held. A release outside the drawer (for example on the
        // scrim) is never seen by it, so the drag must not survive into plain mouse movement.
        if (!e.GetCurrentPoint(_drawer).Properties.IsLeftButtonPressed)
        {
            _dragging = false;
            return;
        }
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
            MavMotion.Animate(_drawer, "mdraw_snap", _drawer.GetTranslationX(), 0, MavMotion.Short4, MavMotion.CubicOut, v =>
            {
                _drawer.SetTranslationX(v);
            });
        }
    }
}
