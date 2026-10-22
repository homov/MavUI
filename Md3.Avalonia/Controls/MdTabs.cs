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
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdTabVariant
{
    Primary,
    Secondary,
}

public sealed class MdTabItem : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdTabItem, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdTabItem, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MdTabItem, string?>(nameof(BadgeText));

    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MdTabItem, Control?>(nameof(Content));

    internal event EventHandler? ItemChanged;

    static MdTabItem()
    {
        TextProperty.Changed.AddClassHandler<MdTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MdTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MdTabItem>((i, _) => i.ItemChanged?.Invoke(i, EventArgs.Empty));
        ContentProperty.Changed.AddClassHandler<MdTabItem>((i, e) =>
        {
            MdVisualTreeHelpers.AttachLogicalChild(i, e.GetNewValue<Control?>());
            i.ItemChanged?.Invoke(i, EventArgs.Empty);
        });
    }

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

    public string? BadgeText
    {
        get => GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public MdTabItem()
    {
    }

    public MdTabItem(
        string text,
        string? icon = null,
        string? badgeText = null,
        Control? content = null)
    {
        Text = text;
        Icon = icon;
        BadgeText = badgeText;
        Content = content;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 Primary and Secondary Tab Bar with animated indicator and optional scrollable mode.
/// </summary>
public class MdTabs : MdContentView, IEnumerable<MdTabItem>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MdTabs, int>(nameof(SelectedIndex), 0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MdTabs, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<MdTabVariant> VariantProperty =
        AvaloniaProperty.Register<MdTabs, MdTabVariant>(nameof(Variant), MdTabVariant.Primary);

    public static readonly StyledProperty<bool> IsScrollableProperty =
        AvaloniaProperty.Register<MdTabs, bool>(nameof(IsScrollable));

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdTabs, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<MdTabs, IDataTemplate?>(nameof(ItemTemplate));

    private readonly AvaloniaList<MdTabItem> _tabs = new();
    private readonly List<object> _rawItems = [];
    private readonly MdItemEventWiring<MdTabItem, EventHandler> _wiredTabs = new(
        static (tab, handler) => tab.ItemChanged += handler,
        static (tab, handler) => tab.ItemChanged -= handler);
    private readonly MdCollectionObserver _observer;
    private readonly List<TabCell> _cells = [];
    private readonly MdSurface _barSurface;
    private readonly Panel _barHost;
    private readonly Border _contentHost;
    private bool _syncingSelection;

    public event EventHandler<int>? SelectedIndexChanged;

    static MdTabs()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MdTabs>((t, e) => t.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MdTabs>((t, e) => t.OnSelectedItemChanged(e.GetNewValue<object?>()));
        VariantProperty.Changed.AddClassHandler<MdTabs>((t, _) => t.Rebuild());
        IsScrollableProperty.Changed.AddClassHandler<MdTabs>((t, _) => t.Rebuild());
        ItemsSourceProperty.Changed.AddClassHandler<MdTabs>((t, e) => t.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ItemTemplateProperty.Changed.AddClassHandler<MdTabs>((t, _) => t.RebuildFromItemsSource(t.ItemsSource));
    }

    public MdTabs() : this(MdTabVariant.Primary, false)
    {
    }

    public MdTabs(MdTabVariant variant = MdTabVariant.Primary, bool scrollable = false)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MdCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));
        _tabs.CollectionChanged += (_, e) =>
        {
            _wiredTabs.HandleCollectionChanged(e);
            foreach (var tab in _tabs)
            {
                MdVisualTreeHelpers.AttachLogicalChild(this, tab);
                _wiredTabs.Add(tab, (_, _) => Rebuild());
            }
            Rebuild();
        };

        _barHost = new Grid();
        _barSurface = new MdSurface
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
        root.Children.Add(new MdDivider());
        root.Children.Add(_contentHost);

        Content = root;
        Variant = variant;
        IsScrollable = scrollable;

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public MdTabVariant Variant
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
    public AvaloniaList<MdTabItem> Tabs => _tabs;

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
            if (item is MdTabItem ti)
            {
                _tabs.Add(ti);
            }
            else if (ItemTemplate?.Build(item) is { } built)
            {
                built.DataContext = item;
                if (built is MdTabItem builtTab)
                    _tabs.Add(builtTab);
                else
                    _tabs.Add(new MdTabItem(item.ToString() ?? string.Empty, content: built));
            }
            else
            {
                _tabs.Add(new MdTabItem(item.ToString() ?? string.Empty));
            }
        }
    }

    public MdTabs AddTab(string text, string? icon = null, string? badge = null, Control? content = null)
    {
        _tabs.Add(new MdTabItem(text, icon, badge, content));
        return this;
    }

    public MdTabs AddTab(MdTabItem item)
    {
        _tabs.Add(item);
        return this;
    }

    public void Add(string text, string? icon = null, string? badge = null, Control? content = null) =>
        AddTab(text, icon, badge, content);

    public void Add(MdTabItem item) => AddTab(item);

    public IEnumerator<MdTabItem> GetEnumerator() => _tabs.GetEnumerator();

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
            var scroll = new MdScrollViewer
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
        ApplyTheme(MdTheme.Current);
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
        if (idx < 0 && item is MdTabItem ti)
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
            MdVisualTreeHelpers.DetachFromParent(view);
            _contentHost.Child = view;
            _contentHost.IsVisible = true;
            if (animate) MdMotion.FadeIn(view, MdMotion.Short3);
        }
        else
        {
            _contentHost.IsVisible = false;
        }
    }

    private void ApplyTheme(MdTheme t)
    {
        _barSurface.FillColor = t.Color.Surface;
        for (int i = 0; i < _cells.Count; i++)
            _cells[i].ApplyTheme(t, i == SelectedIndex);
    }

    private sealed class TabCell : MdContentView
    {
        private readonly MdTabItem _item;
        private readonly MdTabVariant _variant;
        private readonly MdInkSurface _surface;
        private readonly MdIcon _icon;
        private readonly TextBlock _label;
        private readonly Border _indicator;

        public TabCell(MdTabItem item, MdTabVariant variant, Action onClick)
        {
            _item = item;
            _variant = variant;

            _icon = new MdIcon(item.Icon, 22)
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
            if (variant == MdTabVariant.Primary && !string.IsNullOrEmpty(item.Icon))
            {
                var col = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (!string.IsNullOrEmpty(item.BadgeText))
                    col.Children.Add(new MdBadgedBox(_icon, item.BadgeText));
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
                    row.Children.Add(new MdBadge(item.BadgeText));
                contentStack = row;
            }

            _indicator = new Border
            {
                Height = variant == MdTabVariant.Primary ? 3 : 2,
                CornerRadius = variant == MdTabVariant.Primary ? new CornerRadius(3, 3, 0, 0) : new CornerRadius(0),
                HorizontalAlignment = variant == MdTabVariant.Primary ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Width = variant == MdTabVariant.Primary
                    ? Math.Max(24, MdTextMetrics.MeasureWidth(item.Text, 14, bold: true))
                    : double.NaN,
                Opacity = 0,
            };

            var cellGrid = new Grid
            {
                MinHeight = variant == MdTabVariant.Primary && !string.IsNullOrEmpty(item.Icon) ? 64 : 48,
            };
            cellGrid.Children.Add(contentStack);
            cellGrid.Children.Add(_indicator);

            _surface = new MdInkSurface
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
            ApplyTheme(MdTheme.Current, selected);
            var target = selected ? 1.0 : 0.0;
            if (animate)
            {
                MdMotion.Animate(this, "tab_ind", _indicator.Opacity, target, MdMotion.Short4, MdMotion.CubicOut, v =>
                {
                    _indicator.Opacity = v;
                    if (_variant == MdTabVariant.Primary)
                        _indicator.SetScale(0.4 + 0.6 * v, 1);
                });
            }
            else
            {
                _indicator.Opacity = target;
                _indicator.SetScale(1, 1);
            }
        }

        public void ApplyTheme(MdTheme t, bool selected)
        {
            var c = t.Color;
            _label.Apply(MdTypeRole.TitleSmall, t.Typography);

            var fg = selected
                ? (_variant == MdTabVariant.Primary ? c.Primary : c.OnSurface)
                : c.OnSurfaceVariant;

            _label.Foreground = MdTheme.BrushOf(fg);
            _icon.Color = fg;
            _surface.RippleColor = c.Primary;
            _indicator.Background = MdTheme.BrushOf(c.Primary);
        }
    }
}
