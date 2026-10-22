using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Primary and Secondary Tab Bar with animated indicator and optional scrollable mode.
/// </summary>
public class MavTabs : MavContentView, IEnumerable<MavTabItem>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavTabs, int>(nameof(SelectedIndex), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavTabs, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<MavTabVariant> VariantProperty =
        AvaloniaProperty.Register<MavTabs, MavTabVariant>(nameof(Variant), MavTabVariant.Primary);

    public static readonly StyledProperty<bool> IsScrollableProperty =
        AvaloniaProperty.Register<MavTabs, bool>(nameof(IsScrollable));

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavTabs, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MavTabs, IDataTemplate?>(nameof(ItemTemplate));

    private readonly AvaloniaList<MavTabItem> _tabs = new();
    private readonly List<object> _rawItems = [];
    private readonly MavItemEventWiring<MavTabItem, EventHandler> _wiredTabs = new(
        static (tab, handler) => tab.ItemChanged += handler,
        static (tab, handler) => tab.ItemChanged -= handler);
    private readonly MavCollectionObserver _observer;
    private readonly List<TabCell> _cells = [];
    private readonly MavSurface _barSurface;
    private readonly Panel _barHost;
    private readonly Border _contentHost;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;

    static MavTabs()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavTabs>((t, e) => t.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavTabs>((t, e) => t.OnSelectedItemChanged(e.GetNewValue<object?>()));
        VariantProperty.Changed.AddClassHandler<MavTabs>((t, _) => t.Rebuild());
        IsScrollableProperty.Changed.AddClassHandler<MavTabs>((t, _) => t.Rebuild());
        ItemsSourceProperty.Changed.AddClassHandler<MavTabs>((t, e) => t.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MavTabs>((t, _) => t.RebuildFromItemsSource(t.ItemsSource));
    }

    /// <summary>Creates a primary tab bar that does not scroll.</summary>
    public MavTabs() : this(MavTabVariant.Primary, false)
    {
    }

    /// <summary>Creates a tab bar with the given <paramref name="variant"/>, optionally scrollable.</summary>
    public MavTabs(MavTabVariant variant = MavTabVariant.Primary, bool scrollable = false)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _tabs.CollectionChanged += (_, e) =>
        {
            _wiredTabs.HandleCollectionChanged(e);
            foreach (var tab in _tabs)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, tab);
                _wiredTabs.Add(tab, (_, _) => Rebuild());
            }
            Rebuild();
        };

        _barHost = new Grid();
        _barSurface = new MavSurface
        {
            Radius = 0,
            Content = _barHost,
        };

        _contentHost = new Border
        {
            IsVisible = false,
        };

        var root = new StackPanel { Orientation = Orientation.Vertical };
        root.Children.Add(_barSurface);
        root.Children.Add(new MavDivider());
        root.Children.Add(_contentHost);

        Content = root;
        Variant = variant;
        IsScrollable = scrollable;

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set
        {
            var clamped = _tabs.Count == 0 ? 0 : Math.Clamp(value, 0, _tabs.Count - 1);
            SetValue(SelectedIndexProperty, clamped);
        }
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public MavTabVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsScrollable
    {
        get => GetValue(IsScrollableProperty);
        set => SetValue(IsScrollableProperty, value);
    }

    [Content]
    public AvaloniaList<MavTabItem> Tabs => _tabs;

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
        _tabs.Clear();
        _rawItems.Clear();
        foreach (var item in src)
        {
            if (item is null) continue;
            _rawItems.Add(item);
            if (item is MavTabItem ti)
            {
                _tabs.Add(ti);
            }
            else if (ItemTemplate?.Build(item) is { } built)
            {
                built.DataContext = item;
                if (built is MavTabItem builtTab)
                    _tabs.Add(builtTab);
                else
                    _tabs.Add(new MavTabItem(item.ToString() ?? string.Empty, content: built));
            }
            else
            {
                _tabs.Add(new MavTabItem(item.ToString() ?? string.Empty));
            }
        }
    }

    /// <summary>Adds a tab with text and returns the tab bar for chaining.</summary>
    public MavTabs AddTab(string text, string? icon = null, string? badge = null, Control? content = null)
    {
        _tabs.Add(new MavTabItem(text, icon, badge, content));
        return this;
    }

    /// <summary>Adds an existing tab item and returns the tab bar for chaining.</summary>
    public MavTabs AddTab(MavTabItem item)
    {
        _tabs.Add(item);
        return this;
    }

    /// <summary>Adds a tab without returning the tab bar.</summary>
    public void Add(string text, string? icon = null, string? badge = null, Control? content = null) =>
        AddTab(text, icon, badge, content);

    public void Add(MavTabItem item) => AddTab(item);

    public IEnumerator<MavTabItem> GetEnumerator() => _tabs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Rebuild()
    {
        _barHost.Children.Clear();
        _cells.Clear();

        if (_tabs.Count == 0) return;

        if (IsScrollable)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 0,
                Margin = new Thickness(16, 0),
            };
            for (int i = 0; i < _tabs.Count; i++)
            {
                var idx = i;
                var cell = new TabCell(_tabs[i], Variant, () => SelectedIndex = idx);
                _cells.Add(cell);
                row.Children.Add(cell);
            }
            var scroll = new MavScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = row,
            };
            _barHost.Children.Add(scroll);
        }
        else
        {
            var grid = new Grid();
            for (int i = 0; i < _tabs.Count; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                var idx = i;
                var cell = new TabCell(_tabs[i], Variant, () => SelectedIndex = idx);
                _cells.Add(cell);
                grid.Add(cell, i, 0);
            }
            _barHost.Children.Add(grid);
        }

        UpdateSelection(animate: false);
        ApplyTheme(MavTheme.Current);
    }

    private void OnSelectedIndexChanged(int index)
    {
        if (!_syncingSelection)
        {
            _syncingSelection = true;
            object? item = null;
            if (index >= 0 && index < _rawItems.Count)
                item = _rawItems[index];
            else if (index >= 0 && index < _tabs.Count)
                item = _tabs[index];
            SetValue(SelectedItemProperty, item);
            _syncingSelection = false;
        }
        UpdateSelection(animate: true);
        SelectedIndexChanged?.Invoke(this, index);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        _syncingSelection = true;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MavTabItem ti)
            idx = _tabs.IndexOf(ti);
        if (idx >= 0)
            SelectedIndex = idx;
        _syncingSelection = false;
    }

    private void UpdateSelection(bool animate)
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            _cells[i].SetSelected(i == SelectedIndex, animate);
        }

        if (IsScrollable && SelectedIndex >= 0 && SelectedIndex < _cells.Count)
        {
            _cells[SelectedIndex].BringIntoView();
        }

        _contentHost.Child = null;
        if (SelectedIndex >= 0 && SelectedIndex < _tabs.Count && _tabs[SelectedIndex].Content is { } view)
        {
            MavVisualTreeHelpers.DetachFromParent(view);
            _contentHost.Child = view;
            _contentHost.IsVisible = true;
            if (animate) MavMotion.FadeIn(view, MavMotion.Short3);
        }
        else
        {
            _contentHost.IsVisible = false;
        }
    }

    private void ApplyTheme(MavTheme t)
    {
        _barSurface.FillColor = t.Color.Surface;
        for (int i = 0; i < _cells.Count; i++)
            _cells[i].ApplyTheme(t, i == SelectedIndex);
    }

    private sealed class TabCell : MavContentView
    {
        private readonly MavTabItem _item;
        private readonly MavTabVariant _variant;
        private readonly MavInkSurface _surface;
        private readonly MavIcon _icon;
        private readonly TextBlock _label;
        private readonly Border _indicator;

        public TabCell(MavTabItem item, MavTabVariant variant, Action onClick)
        {
            _item = item;
            _variant = variant;

            _icon = new MavIcon(item.Icon, 22)
            {
                UseThemeDefaultColor = false,
                IsVisible = !string.IsNullOrEmpty(item.Icon),
            };

            _label = new TextBlock
            {
                Text = item.Text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
            };

            Control contentStack;
            if (variant == MavTabVariant.Primary && !string.IsNullOrEmpty(item.Icon))
            {
                var col = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (!string.IsNullOrEmpty(item.BadgeText))
                    col.Children.Add(new MavBadgedBox(_icon, item.BadgeText));
                else
                    col.Children.Add(_icon);
                col.Children.Add(_label);
                contentStack = col;
            }
            else
            {
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (!string.IsNullOrEmpty(item.Icon))
                    row.Children.Add(_icon);
                row.Children.Add(_label);
                if (!string.IsNullOrEmpty(item.BadgeText))
                    row.Children.Add(new MavBadge(item.BadgeText));
                contentStack = row;
            }

            _indicator = new Border
            {
                Height = variant == MavTabVariant.Primary ? 3 : 2,
                CornerRadius = variant == MavTabVariant.Primary ? new CornerRadius(3, 3, 0, 0) : new CornerRadius(0),
                HorizontalAlignment = variant == MavTabVariant.Primary ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Width = variant == MavTabVariant.Primary
                    ? Math.Max(24, MavTextMetrics.MeasureWidth(item.Text, 14, bold: true))
                    : double.NaN,
                Opacity = 0,
            };

            var cellGrid = new Grid
            {
                MinHeight = variant == MavTabVariant.Primary && !string.IsNullOrEmpty(item.Icon) ? 64 : 48,
            };
            cellGrid.Children.Add(contentStack);
            cellGrid.Children.Add(_indicator);

            _surface = new MavInkSurface
            {
                CornerRadius = 0,
                Padding = new Thickness(16, 0),
                Content = cellGrid,
            };
            _surface.Clicked += (_, _) => onClick();

            Content = _surface;
        }

        public void SetSelected(bool selected, bool animate)
        {
            ApplyTheme(MavTheme.Current, selected);
            var target = selected ? 1.0 : 0.0;
            if (animate)
            {
                MavMotion.Animate(this, "tab_ind", _indicator.Opacity, target, MavMotion.Short4, MavMotion.CubicOut, v =>
                {
                    _indicator.Opacity = v;
                    if (_variant == MavTabVariant.Primary)
                        _indicator.SetScale(0.4 + 0.6 * v, 1);
                });
            }
            else
            {
                _indicator.Opacity = target;
                _indicator.SetScale(1, 1);
            }
        }

        public void ApplyTheme(MavTheme t, bool selected)
        {
            var c = t.Color;
            _label.Apply(MavTypeRole.TitleSmall, t.Typography);

            var fg = selected
                ? (_variant == MavTabVariant.Primary ? c.Primary : c.OnSurface)
                : c.OnSurfaceVariant;

            _label.Foreground = MavTheme.BrushOf(fg);
            _icon.Color = fg;
            _surface.RippleColor = c.Primary;
            _indicator.Background = MavTheme.BrushOf(c.Primary);
        }
    }
}
