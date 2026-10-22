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

/// <summary>Layout chosen by <see cref="MavNavigationSuiteScaffold"/> for the current width: bottom bar, rail or drawer.</summary>
public enum MavNavigationSuiteMode
{
    Auto,
    NavigationBar,
    NavigationRail,
    NavigationDrawer,
    None,
}

/// <summary>Destination shown by a <see cref="MavNavigationSuiteScaffold"/>.</summary>
public sealed class MavSuiteDestination : Control
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string>(nameof(Icon), MavIcons.Home);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<string?> BadgeTextProperty =
        AvaloniaProperty.Register<MavSuiteDestination, string?>(nameof(BadgeText));

    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<MavSuiteDestination, Control?>(nameof(Content));

    internal event EventHandler? DestinationChanged;

    static MavSuiteDestination()
    {
        LabelProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        IconProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        SelectedIconProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        BadgeTextProperty.Changed.AddClassHandler<MavSuiteDestination>((d, _) => d.DestinationChanged?.Invoke(d, EventArgs.Empty));
        ContentProperty.Changed.AddClassHandler<MavSuiteDestination>((d, e) =>
        {
            MavVisualTreeHelpers.AttachLogicalChild(d, e.GetNewValue<Control?>());
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

    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Creates an empty destination.</summary>
    public MavSuiteDestination()
    {
    }

    /// <summary>Creates a destination with label, icon, and the content it shows.</summary>
    public MavSuiteDestination(
        string label,
        string icon,
        string? selectedIcon = null,
        string? badgeText = null,
        Control? content = null)
    {
        Label = label;
        Icon = icon;
        SelectedIcon = selectedIcon;
        BadgeText = badgeText;
        Content = content;
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 Adaptive Navigation Suite Scaffold that automatically transitions between
/// a bottom <see cref="MavNavigationBar"/> (&lt; 600dp), a side <see cref="MavNavigationRail"/> (600–839dp),
/// and a permanent <see cref="MavNavigationDrawer"/> (&gt;= 840dp).
/// </summary>
public class MavNavigationSuiteScaffold : MavContentView, IEnumerable<MavSuiteDestination>
{
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, int>(
            nameof(SelectedIndex),
            0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, object?>(
            nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<MavNavigationSuiteMode> ModeProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, MavNavigationSuiteMode>(
            nameof(Mode),
            MavNavigationSuiteMode.Auto);

    public static readonly StyledProperty<double> CompactBreakpointProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, double>(
            nameof(CompactBreakpoint),
            600.0);

    public static readonly StyledProperty<double> ExpandedBreakpointProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, double>(
            nameof(ExpandedBreakpoint),
            840.0);

    public static readonly StyledProperty<string?> DrawerHeadlineProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, string?>(
            nameof(DrawerHeadline),
            "Navigation");

    public static readonly StyledProperty<Control?> TopAppBarProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, Control?>(nameof(TopAppBar));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, Control?>(nameof(Body));

    public static readonly StyledProperty<Control?> FloatingActionButtonProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, Control?>(nameof(FloatingActionButton));

    public static readonly StyledProperty<Control?> RailHeaderProperty =
        AvaloniaProperty.Register<MavNavigationSuiteScaffold, Control?>(nameof(RailHeader));

    private readonly AvaloniaList<MavSuiteDestination> _destinations = new();
    private readonly MavItemEventWiring<MavSuiteDestination, EventHandler> _wiredDestinations = new(
        static (destination, handler) => destination.DestinationChanged += handler,
        static (destination, handler) => destination.DestinationChanged -= handler);
    private readonly List<object> _rawItems = [];
    private readonly MavCollectionObserver _observer;
    private readonly Grid _root;
    private readonly Border _topBarHost;
    private readonly Border _bodyHost;
    private readonly Border _fabHost;
    private readonly MavNavigationBar _navBar;
    private readonly MavNavigationRail _navRail;
    private readonly MavNavigationDrawer _navDrawer;
    private readonly MavDivider _railDivider;

    private MavNavigationSuiteMode _effectiveMode = MavNavigationSuiteMode.NavigationBar;
    private bool _syncingSelection;
    private bool _suppressDestinationCollectionSync;

    public event EventHandler<int>? SelectedIndexChanged;
    public event EventHandler<MavNavigationSuiteMode>? EffectiveModeChanged;

    static MavNavigationSuiteScaffold()
    {
        SelectedIndexProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, e) =>
            s.OnSelectedIndexChanged(e.GetNewValue<int>()));
        SelectedItemProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, e) =>
            s.OnSelectedItemChanged(e.GetNewValue<object?>()));
        ItemsSourceProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, e) =>
            s.OnItemsSourceChanged(e.GetNewValue<IEnumerable?>()));
        ModeProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, _) =>
            s.UpdateLayoutMode());
        CompactBreakpointProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, _) =>
            s.UpdateLayoutMode());
        ExpandedBreakpointProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, _) =>
            s.UpdateLayoutMode());
        DrawerHeadlineProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, e) =>
            s._navDrawer.Title = e.GetNewValue<string?>() ?? string.Empty);
        TopAppBarProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, e) =>
        {
            var bar = e.GetNewValue<Control?>();
            s._topBarHost.Child = null;
            MavVisualTreeHelpers.DetachFromParent(bar);
            s._topBarHost.Child = bar;
            s._topBarHost.IsVisible = bar is not null;
        });
        BodyProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, _) =>
            s.SyncBodyContent());
        FloatingActionButtonProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, e) =>
        {
            var fab = e.GetNewValue<Control?>();
            s._fabHost.Child = null;
            MavVisualTreeHelpers.DetachFromParent(fab);
            s._fabHost.Child = fab;
            s._fabHost.IsVisible = fab is not null;
        });
        RailHeaderProperty.Changed.AddClassHandler<MavNavigationSuiteScaffold>((s, e) =>
            s._navRail.Header = e.GetNewValue<Control?>());
    }

    /// <summary>Creates an empty adaptive navigation scaffold.</summary>
    public MavNavigationSuiteScaffold()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => RebuildFromItemsSource(ItemsSource));

        _destinations.CollectionChanged += (_, e) =>
        {
            _wiredDestinations.HandleCollectionChanged(e);
            foreach (var d in _destinations)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, d);
                _wiredDestinations.Add(d, (_, _) => RebuildChildNavControls());
            }
            if (!_suppressDestinationCollectionSync)
                RebuildChildNavControls();
        };

        _navBar = new MavNavigationBar();
        _navBar.SelectedIndexChanged += (_, idx) => SyncFromChild(idx);

        _navRail = new MavNavigationRail();
        _navRail.SelectedIndexChanged += (_, idx) => SyncFromChild(idx);

        _navDrawer = new MavNavigationDrawer("Navigation", 240);
        _navDrawer.SelectedIndexChanged += (_, idx) => SyncFromChild(idx);

        _railDivider = new MavDivider(vertical: true);

        _topBarHost = new Border { IsVisible = false };
        _bodyHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _fabHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16),
            IsVisible = false,
        };

        _root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
        };

        Content = _root;

        SizeChanged += (_, _) => UpdateLayoutMode();
        MavTheme.Observe(this, (self, theme) =>
        {
            self._root.Background = MavTheme.BrushOf(theme.Color.Surface);
        });
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
            if (item is MavSuiteDestination sd)
                _destinations.Add(sd);
            else
                _destinations.Add(new MavSuiteDestination(item.ToString() ?? string.Empty, MavIcons.Circle));
        }
    }

    public MavNavigationSuiteMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public MavNavigationSuiteMode EffectiveMode => _effectiveMode;

    public double CompactBreakpoint
    {
        get => GetValue(CompactBreakpointProperty);
        set => SetValue(CompactBreakpointProperty, value);
    }

    public double ExpandedBreakpoint
    {
        get => GetValue(ExpandedBreakpointProperty);
        set => SetValue(ExpandedBreakpointProperty, value);
    }

    public string? DrawerHeadline
    {
        get => GetValue(DrawerHeadlineProperty);
        set => SetValue(DrawerHeadlineProperty, value);
    }

    public Control? TopAppBar
    {
        get => GetValue(TopAppBarProperty);
        set => SetValue(TopAppBarProperty, value);
    }

    public Control? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public Control? FloatingActionButton
    {
        get => GetValue(FloatingActionButtonProperty);
        set => SetValue(FloatingActionButtonProperty, value);
    }

    public Control? RailHeader
    {
        get => GetValue(RailHeaderProperty);
        set => SetValue(RailHeaderProperty, value);
    }

    [Content]
    public AvaloniaList<MavSuiteDestination> Destinations => _destinations;

    /// <summary>Adds a destination and returns the scaffold for chaining.</summary>
    public MavNavigationSuiteScaffold AddDestination(
        string label,
        string icon,
        string? selectedIcon = null,
        string? badge = null,
        Control? content = null)
    {
        _suppressDestinationCollectionSync = true;
        _destinations.Add(new MavSuiteDestination(label, icon, selectedIcon, badge, content));
        _suppressDestinationCollectionSync = false;
        _navBar.AddDestination(label, icon, selectedIcon, badge);
        _navRail.AddDestination(label, icon, selectedIcon, badge);
        _navDrawer.AddItem(label, icon, badge);

        RebuildArrangement();
        SyncBodyContent();
        return this;
    }

    public void Add(MavSuiteDestination destination) => _destinations.Add(destination);

    public IEnumerator<MavSuiteDestination> GetEnumerator() => _destinations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void RebuildChildNavControls()
    {
        _navBar.Destinations.Clear();
        _navRail.Destinations.Clear();
        _navDrawer.Items.Clear();

        foreach (var d in _destinations)
        {
            _navBar.AddDestination(d.Label, d.Icon, d.SelectedIcon, d.BadgeText);
            _navRail.AddDestination(d.Label, d.Icon, d.SelectedIcon, d.BadgeText);
            _navDrawer.AddItem(d.Label, d.Icon, d.BadgeText);
        }

        RebuildArrangement();
        SyncBodyContent();
    }

    private void SyncFromChild(int index)
    {
        if (_syncingSelection) return;
        SelectedIndex = index;
    }

    private void OnSelectedIndexChanged(int idx)
    {
        if (_syncingSelection) return;
        _syncingSelection = true;
        _navBar.SelectedIndex = idx;
        _navRail.SelectedIndex = idx;
        _navDrawer.SelectedIndex = idx;
        object? item = null;
        if (idx >= 0 && idx < _rawItems.Count)
            item = _rawItems[idx];
        else if (idx >= 0 && idx < _destinations.Count)
            item = _destinations[idx];
        SetValue(SelectedItemProperty, item);
        _syncingSelection = false;

        SyncBodyContent();
        SelectedIndexChanged?.Invoke(this, idx);
    }

    private void OnSelectedItemChanged(object? item)
    {
        if (_syncingSelection || item is null) return;
        int idx = _rawItems.IndexOf(item);
        if (idx < 0 && item is MavSuiteDestination sd)
            idx = _destinations.IndexOf(sd);
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

    private void SyncBodyContent()
    {
        var explicitBody = Body;
        if (explicitBody is not null)
        {
            _bodyHost.Child = null;
            MavVisualTreeHelpers.DetachFromParent(explicitBody);
            _bodyHost.Child = explicitBody;
            return;
        }

        if (SelectedIndex >= 0 && SelectedIndex < _destinations.Count &&
            _destinations[SelectedIndex].Content is { } destContent)
        {
            _bodyHost.Child = null;
            MavVisualTreeHelpers.DetachFromParent(destContent);
            _bodyHost.Child = destContent;
            MavMotion.FadeIn(destContent, MavMotion.Short3);
        }
    }

    private MavNavigationSuiteMode ResolveEffectiveMode()
    {
        if (Mode != MavNavigationSuiteMode.Auto)
            return Mode;

        var w = Bounds.Width;
        if (w <= 0)
            return MavNavigationSuiteMode.NavigationBar;
        if (w < CompactBreakpoint)
            return MavNavigationSuiteMode.NavigationBar;
        if (w < ExpandedBreakpoint)
            return MavNavigationSuiteMode.NavigationRail;
        return MavNavigationSuiteMode.NavigationDrawer;
    }

    private void UpdateLayoutMode()
    {
        var resolved = ResolveEffectiveMode();
        var changed = resolved != _effectiveMode;
        _effectiveMode = resolved;
        RebuildArrangement();
        if (changed)
            EffectiveModeChanged?.Invoke(this, _effectiveMode);
    }

    private void RebuildArrangement()
    {
        _root.Children.Clear();

        switch (_effectiveMode)
        {
            case MavNavigationSuiteMode.NavigationRail:
                _root.Add(_navRail, 0, 0);
                Grid.SetRowSpan(_navRail, 3);
                _root.Add(_railDivider, 1, 0);
                Grid.SetRowSpan(_railDivider, 3);
                break;

            case MavNavigationSuiteMode.NavigationDrawer:
                _root.Add(_navDrawer, 0, 0);
                Grid.SetRowSpan(_navDrawer, 3);
                _root.Add(_railDivider, 1, 0);
                Grid.SetRowSpan(_railDivider, 3);
                break;
        }

        _root.Add(_topBarHost, 2, 0);
        _root.Add(_bodyHost, 2, 1);
        _root.Add(_fabHost, 2, 1);

        if (_effectiveMode == MavNavigationSuiteMode.NavigationBar)
        {
            _root.Add(_navBar, 2, 2);
        }
    }
}
