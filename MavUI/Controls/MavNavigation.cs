using System.Collections;
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

/// <summary>Destination of a <see cref="MavNavigationBar"/> or <see cref="MavNavigationRail"/>.</summary>
public sealed class MavNavigationDestination : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string>(nameof(Icon), MavIcons.Home);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavNavigationDestination, string?>(nameof(BadgeText));

    public static readonly StyledProperty<bool> ShowBadgeDotProperty =
        AvaloniaProperty.Register<MavNavigationDestination, bool>(nameof(ShowBadgeDot));

    internal event EventHandler? DestinationChanged;

    static MavNavigationDestination()
    {
        LabelProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        SelectedIconProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavNavigationDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        ShowBadgeDotProperty.Changed.AddClassHandler<MavNavigationDestination>((d, e) =>
        {
            if (e.GetNewValue<bool>() && d.BadgeText is null)
                d.BadgeText = string.Empty;
            d.DestinationChanged?.Invoke(d, EventArgs.Empty);
        });
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

    public bool ShowBadgeDot
    {
        get => GetValue(ShowBadgeDotProperty);
        set => SetValue(ShowBadgeDotProperty, value);
    }

    /// <summary>Creates an empty navigation destination.</summary>
    public MavNavigationDestination()
    {
    }

    /// <summary>Creates a destination with label, icon, selected icon and optional badge.</summary>
    public MavNavigationDestination(
        string label,
        string icon,
        string? selectedIcon = null,
        string? badgeText = null)
    {
        Label = label;
        Icon = icon;
        SelectedIcon = selectedIcon;
        BadgeText = badgeText;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 Bottom Navigation Bar (80dp height, 3-5 destinations, animated active pill indicator).
/// </summary>
public class MavNavigationBar : MavContentView, IEnumerable<MavNavigationDestination>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavNavigationBar, int>(nameof(SelectedIndex), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavNavigationBar, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavNavigationBar, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<bool> ShowLabelsProperty =
        AvaloniaProperty.Register<MavNavigationBar, bool>(nameof(ShowLabels), true);

    private readonly AvaloniaList<MavNavigationDestination> _destinations = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavNavigationDestination, EventHandler> _wiredDestinations = new(
        static (destination, handler) => destination.DestinationChanged += handler,
        static (destination, handler) => destination.DestinationChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly List<NavBarCell> _cells = [];
    private readonly MavSurface _bar;
    private readonly Grid _row;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;

    static MavNavigationBar()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavNavigationBar>((b, e) => b.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavNavigationBar>((b, e) => b.OnSelectedItemChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MavNavigationBar>((b, e) => b.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ShowLabelsProperty.Changed.AddClassHandler<MavNavigationBar>((b, _) => b.Rebuild());
    }

    /// <summary>Creates an empty bottom navigation bar.</summary>
    public MavNavigationBar()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _destinations.CollectionChanged += (_, e) =>
        {
            _wiredDestinations.HandleCollectionChanged(e);
            foreach (var d in _destinations)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, d);
                _wiredDestinations.Add(d, (_, _) => Rebuild());
            }
            Rebuild();
        };
        _row = new Grid { MinHeight = 80 };
        _bar = new MavSurface
        {
            Radius = 0,
            Elevation = 2,
            Content = _row,
        };
        Content = _bar;

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set
        {
            var clamped = _destinations.Count == 0 ? 0 : Math.Clamp(value, 0, _destinations.Count - 1);
            SetValue(SelectedIndexProperty, clamped);
        }
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
        _destinations.Clear();
        _rawItems.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MavNavigationDestination dest)
                _destinations.Add(dest);
            else
                _destinations.Add(new MavNavigationDestination(item.ToString() ?? string.Empty, MavIcons.Circle));
        }
    }

    public bool ShowLabels
    {
        get => GetValue(ShowLabelsProperty);
        set => SetValue(ShowLabelsProperty, value);
    }

    [Content]
    public AvaloniaList<MavNavigationDestination> Destinations => _destinations;

    /// <summary>Adds a destination and returns the bar for chaining.</summary>
    public MavNavigationBar AddDestination(string label, string icon, string? selectedIcon = null, string? badge = null)
    {
        _destinations.Add(new MavNavigationDestination(label, icon, selectedIcon, badge));
        return this;
    }

    public void Add(MavNavigationDestination destination) => _destinations.Add(destination);

    /// <summary>Adds a destination without returning the bar.</summary>
    public void Add(string label, string icon, string? selectedIcon = null, string? badge = null) =>
        AddDestination(label, icon, selectedIcon, badge);

    public IEnumerator<MavNavigationDestination> GetEnumerator() => _destinations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _row.Children.Clear();
        _row.ColumnDefinitions.Clear();
        _cells.Clear();

        for (int i = 0; i < _destinations.Count; i++)
        {
            _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var idx = i;
            var cell = new NavBarCell(_destinations[i], ShowLabels, () => SelectedIndex = idx);
            _cells.Add(cell);
            _row.Add(cell, i, 0);
        }

        UpdateSelection(animate: false);
        ApplyTheme(MavTheme.Current);
    }

    private void OnSelectedIndexChanged(int idx)
    {
        if (_syncingSelection) return;
        _syncingSelection = true;
        object? item = null;
        if (idx >= 0 && idx < _rawItems.Count)
            item = _rawItems[idx];
        else if (idx >= 0 && idx < _destinations.Count)
            item = _destinations[idx];
        SetValue(SelectedItemProperty, item);
        _syncingSelection = false;
        UpdateSelection(animate: true);
        SelectedIndexChanged?.Invoke(this, idx);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MavNavigationDestination dest)
            idx = _destinations.IndexOf(dest);
        else if (idx < 0 && item is string s)
        {
            for (int i = 0; i < _destinations.Count; i++)
            {
                if (string.Equals(_destinations[i].Label, s, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
        }
        if (idx >= 0)
            SelectedIndex = idx;
    }

    private void UpdateSelection(bool animate)
    {
        for (int i = 0; i < _cells.Count; i++)
            _cells[i].SetSelected(i == SelectedIndex, animate);
    }

    private void ApplyTheme(MavTheme t)
    {
        _bar.FillColor = t.Color.SurfaceContainer;
        for (int i = 0; i < _cells.Count; i++)
            _cells[i].ApplyTheme(t, i == SelectedIndex);
    }

    private sealed class NavBarCell : MavContentView
    {
        private readonly MavNavigationDestination _dest;
        private readonly MavInkSurface _pill;
        private readonly MavIcon _icon;
        private readonly TextBlock _label;

        public NavBarCell(MavNavigationDestination dest, bool showLabel, Action onClick)
        {
            _dest = dest;
            _icon = new MavIcon(dest.Icon, 24) { UseThemeDefaultColor = false };
            Control iconNode = !string.IsNullOrEmpty(dest.BadgeText)
                ? new MavBadgedBox(_icon, dest.BadgeText)
                : _icon;

            _pill = new MavInkSurface
            {
                Width = 64,
                Height = 32,
                CornerRadius = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = iconNode,
            };
            _pill.Clicked += (_, _) => onClick();

            _label = new TextBlock
            {
                Text = dest.Label,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                IsVisible = showLabel,
                TextWrapping = TextWrapping.NoWrap,
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            stack.Children.Add(_pill);
            stack.Children.Add(_label);

            var tapBox = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(0, 12, 0, 16),
                Child = stack,
            };
            tapBox.PointerReleased += (_, _) => onClick();

            Content = tapBox;
        }

        public void SetSelected(bool selected, bool animate)
        {
            _icon.Glyph = selected && !string.IsNullOrEmpty(_dest.SelectedIcon) ? _dest.SelectedIcon : _dest.Icon;
            ApplyTheme(MavTheme.Current, selected);
            if (animate && selected)
            {
                _pill.SetScale(0.6, 1.0);
                MavMotion.Animate(_pill, "nav_pill", 0.6, 1.0, MavMotion.Short4, MavMotion.CubicOut, v =>
                {
                    _pill.SetScale(v, 1.0);
                });
            }
        }

        public void ApplyTheme(MavTheme t, bool selected)
        {
            var c = t.Color;
            _label.Apply(MavTypeRole.LabelMedium, t.Typography);
            _label.FontWeight = selected ? FontWeight.SemiBold : FontWeight.Medium;

            _pill.FillColor = selected ? c.SecondaryContainer : Colors.Transparent;
            _pill.RippleColor = c.OnSecondaryContainer;
            _icon.Color = selected ? c.OnSecondaryContainer : c.OnSurfaceVariant;
            _label.Foreground = MavTheme.BrushOf(selected ? c.OnSurface : c.OnSurfaceVariant);
        }
    }
}

/// <summary>
/// Material Design 3 Vertical Navigation Rail (80dp width, optional menu/FAB header, vertical destinations).
/// </summary>
public class MavNavigationRail : MavContentView, IEnumerable<MavNavigationDestination>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavNavigationRail, int>(nameof(SelectedIndex), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavNavigationRail, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavNavigationRail, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<Control?> HeaderProperty =
        AvaloniaProperty.Register<MavNavigationRail, Control?>(nameof(Header));

    private readonly AvaloniaList<MavNavigationDestination> _destinations = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavNavigationDestination, EventHandler> _wiredDestinations = new(
        static (destination, handler) => destination.DestinationChanged += handler,
        static (destination, handler) => destination.DestinationChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly List<RailCell> _cells = [];
    private readonly MavSurface _rail;
    private readonly StackPanel _headerHost;
    private readonly StackPanel _itemsStack;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;

    static MavNavigationRail()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavNavigationRail>((r, e) => r.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavNavigationRail>((r, e) => r.OnSelectedItemChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MavNavigationRail>((r, e) => r.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        HeaderProperty.Changed.AddClassHandler<MavNavigationRail>((r, e) =>
        {
            r._headerHost.Children.Clear();
            var value = e.GetNewValue<Control?>();
            if (value is not null)
            {
                MavVisualTreeHelpers.DetachFromParent(value);
                r._headerHost.Children.Add(value);
            }
            r._headerHost.IsVisible = value is not null;
        });
    }

    /// <summary>Creates a navigation rail without a header.</summary>
    public MavNavigationRail() : this(null)
    {
    }

    /// <summary>Creates a navigation rail with an optional <paramref name="header"/> above the destinations.</summary>
    public MavNavigationRail(Control? header = null)
    {
        Width = 80;
        VerticalAlignment = VerticalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _destinations.CollectionChanged += (_, e) =>
        {
            _wiredDestinations.HandleCollectionChanged(e);
            foreach (var d in _destinations)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, d);
                _wiredDestinations.Add(d, (_, _) => Rebuild());
            }
            Rebuild();
        };

        _headerHost = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 16),
            IsVisible = header is not null,
        };
        if (header is not null)
            Header = header;

        _itemsStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var col = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        col.Children.Add(_headerHost);
        col.Children.Add(_itemsStack);

        _rail = new MavSurface
        {
            Width = 80,
            Radius = 0,
            Padding = new Thickness(0, 8),
            Content = col,
        };

        Content = _rail;
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set
        {
            var clamped = _destinations.Count == 0 ? 0 : Math.Clamp(value, 0, _destinations.Count - 1);
            SetValue(SelectedIndexProperty, clamped);
        }
    }

    public Control? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
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
        _destinations.Clear();
        _rawItems.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MavNavigationDestination dest)
                _destinations.Add(dest);
            else
                _destinations.Add(new MavNavigationDestination(item.ToString() ?? string.Empty, MavIcons.Circle));
        }
    }

    [Content]
    public AvaloniaList<MavNavigationDestination> Destinations => _destinations;

    /// <summary>Adds a destination and returns the rail for chaining.</summary>
    public MavNavigationRail AddDestination(string label, string icon, string? selectedIcon = null, string? badge = null)
    {
        _destinations.Add(new MavNavigationDestination(label, icon, selectedIcon, badge));
        return this;
    }

    public void Add(MavNavigationDestination destination) => _destinations.Add(destination);

    /// <summary>Adds a destination without returning the rail.</summary>
    public void Add(string label, string icon, string? selectedIcon = null, string? badge = null) =>
        AddDestination(label, icon, selectedIcon, badge);

    public IEnumerator<MavNavigationDestination> GetEnumerator() => _destinations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _itemsStack.Children.Clear();
        _cells.Clear();

        for (int i = 0; i < _destinations.Count; i++)
        {
            var idx = i;
            var cell = new RailCell(_destinations[i], () => SelectedIndex = idx);
            _cells.Add(cell);
            _itemsStack.Children.Add(cell);
        }

        UpdateSelection();
        ApplyTheme(MavTheme.Current);
    }

    private void OnSelectedIndexChanged(int idx)
    {
        if (_syncingSelection) return;
        _syncingSelection = true;
        object? item = null;
        if (idx >= 0 && idx < _rawItems.Count)
            item = _rawItems[idx];
        else if (idx >= 0 && idx < _destinations.Count)
            item = _destinations[idx];
        SetValue(SelectedItemProperty, item);
        _syncingSelection = false;
        UpdateSelection();
        SelectedIndexChanged?.Invoke(this, idx);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MavNavigationDestination dest)
            idx = _destinations.IndexOf(dest);
        else if (idx < 0 && item is string s)
        {
            for (int i = 0; i < _destinations.Count; i++)
            {
                if (string.Equals(_destinations[i].Label, s, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
        }
        if (idx >= 0)
            SelectedIndex = idx;
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < _cells.Count; i++)
            _cells[i].SetSelected(i == SelectedIndex);
    }

    private void ApplyTheme(MavTheme t)
    {
        _rail.FillColor = t.Color.Surface;
        for (int i = 0; i < _cells.Count; i++)
            _cells[i].ApplyTheme(t, i == SelectedIndex);
    }

    private sealed class RailCell : MavContentView
    {
        private readonly MavNavigationDestination _dest;
        private readonly MavInkSurface _pill;
        private readonly MavIcon _icon;
        private readonly TextBlock _label;

        public RailCell(MavNavigationDestination dest, Action onClick)
        {
            _dest = dest;
            _icon = new MavIcon(dest.Icon, 24) { UseThemeDefaultColor = false };
            Control iconNode = !string.IsNullOrEmpty(dest.BadgeText)
                ? new MavBadgedBox(_icon, dest.BadgeText)
                : _icon;

            _pill = new MavInkSurface
            {
                Width = 56,
                Height = 32,
                CornerRadius = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = iconNode,
            };
            _pill.Clicked += (_, _) => onClick();

            _label = new TextBlock
            {
                Text = dest.Label,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            stack.Children.Add(_pill);
            stack.Children.Add(_label);

            Content = stack;
        }

        public void SetSelected(bool selected)
        {
            _icon.Glyph = selected && !string.IsNullOrEmpty(_dest.SelectedIcon) ? _dest.SelectedIcon : _dest.Icon;
            ApplyTheme(MavTheme.Current, selected);
        }

        public void ApplyTheme(MavTheme t, bool selected)
        {
            var c = t.Color;
            _label.Apply(MavTypeRole.LabelMedium, t.Typography);
            _pill.FillColor = selected ? c.SecondaryContainer : Colors.Transparent;
            _pill.RippleColor = c.OnSecondaryContainer;
            _icon.Color = selected ? c.OnSecondaryContainer : c.OnSurfaceVariant;
            _label.Foreground = MavTheme.BrushOf(selected ? c.OnSurface : c.OnSurfaceVariant);
        }
    }
}
