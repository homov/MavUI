using Avalonia;
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
/// Material Design 3 Adaptive Split View (Master-Detail / List-Detail layout) with a draggable
/// MD3 handle splitter in expanded mode and an overlay drawer with scrim in compact mode.
/// </summary>
public class MavSplitView : MavContentView, IMavDraggableControl
{
    public static readonly StyledProperty<Control?> PaneProperty =
        AvaloniaProperty.Register<MavSplitView, Control?>(nameof(Pane));

    public static readonly StyledProperty<Control?> DetailContentProperty =
        AvaloniaProperty.Register<MavSplitView, Control?>(nameof(DetailContent));

    public static readonly StyledProperty<double> PaneWidthProperty =
        AvaloniaProperty.Register<MavSplitView, double>(nameof(PaneWidth), 240.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinPaneWidthProperty =
        AvaloniaProperty.Register<MavSplitView, double>(nameof(MinPaneWidth), 150.0);

    public static readonly StyledProperty<double> MaxPaneWidthProperty =
        AvaloniaProperty.Register<MavSplitView, double>(nameof(MaxPaneWidth), 420.0);

    public static readonly StyledProperty<bool> IsPaneOpenProperty =
        AvaloniaProperty.Register<MavSplitView, bool>(nameof(IsPaneOpen), true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsResizableProperty =
        AvaloniaProperty.Register<MavSplitView, bool>(nameof(IsResizable), true);

    public static readonly StyledProperty<double> CompactBreakpointProperty =
        AvaloniaProperty.Register<MavSplitView, double>(nameof(CompactBreakpoint), 520.0);

    public static readonly StyledProperty<double> InitialRatioProperty =
        AvaloniaProperty.Register<MavSplitView, double>(nameof(InitialRatio), 0.35);

    private readonly Border _outerBorder;
    private readonly Border _borderOverlay;
    private readonly Grid _rootGrid;
    private readonly Border _inlinePaneHost;
    private readonly Border _splitterHit;
    private readonly Border _splitterPill;
    private readonly Border _detailHost;
    private readonly Border _compactScrim;
    private readonly Border _compactDrawerHost;
    private bool _isCompact;
    private bool _draggingSplitter;
    private double _dragStartX;
    private double _dragStartPaneWidth;
    private bool _paneWidthResolved;
    private bool _applyingInitialRatio;

    public event EventHandler<bool>? CompactModeChanged;

    static MavSplitView()
    {
        PaneProperty.Changed.AddClassHandler<MavSplitView>((s, _) => s.SyncContent());
        DetailContentProperty.Changed.AddClassHandler<MavSplitView>((s, _) => s.SyncContent());
        PaneWidthProperty.Changed.AddClassHandler<MavSplitView>((s, _) =>
        {
            // An explicit PaneWidth wins over InitialRatio, including values set before layout.
            if (!s._applyingInitialRatio)
                s._paneWidthResolved = true;
            s.SyncLayout();
        });
        IsPaneOpenProperty.Changed.AddClassHandler<MavSplitView>((s, _) => s.SyncLayout());
        IsResizableProperty.Changed.AddClassHandler<MavSplitView>((s, _) => s.SyncLayout());
        CompactBreakpointProperty.Changed.AddClassHandler<MavSplitView>((s, _) => s.UpdateCompactState(s.Bounds.Width));
    }

    public MavSplitView() : this(null, null)
    {
    }

    public MavSplitView(Control? pane = null, Control? detailContent = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        _inlinePaneHost = new Border
        {
            ClipToBounds = true,
        };

        _splitterPill = new Border
        {
            Width = 4,
            Height = 36,
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        _splitterHit = new Border
        {
            Width = 12,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.SizeWestEast),
            Child = _splitterPill,
        };
        _splitterHit.PointerPressed += OnSplitterPointerPressed;
        _splitterHit.PointerMoved += OnSplitterPointerMoved;
        _splitterHit.PointerReleased += OnSplitterPointerReleased;

        _detailHost = new Border
        {
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        _compactScrim = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(96, 0, 0, 0)),
            IsVisible = false,
        };
        _compactScrim.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            IsPaneOpen = false;
        };

        _compactDrawerHost = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            CornerRadius = new CornerRadius(0, 16, 16, 0),
            ClipToBounds = true,
            IsVisible = false,
        };

        _rootGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
        };
        _rootGrid.Add(_inlinePaneHost, 0, 0);
        _rootGrid.Add(_splitterHit, 1, 0);
        _rootGrid.Add(_detailHost, 2, 0);

        var overlayLayer = new Grid
        {
            Children = { _rootGrid, _compactScrim, _compactDrawerHost },
        };

        var innerClip = new Border
        {
            CornerRadius = new CornerRadius(15),
            Margin = new Thickness(1),
            ClipToBounds = true,
            Child = overlayLayer,
        };

        _borderOverlay = new Border
        {
            CornerRadius = new CornerRadius(16),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
        };

        _outerBorder = new Border
        {
            CornerRadius = new CornerRadius(16),
            ClipToBounds = true,
            Child = new Grid
            {
                Children = { innerClip, _borderOverlay },
            },
        };

        Content = _outerBorder;

        if (pane is not null) Pane = pane;
        if (detailContent is not null) DetailContent = detailContent;

        SyncContent();
        SyncLayout();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public Control? Pane
    {
        get => GetValue(PaneProperty);
        set => SetValue(PaneProperty, value);
    }

    public Control? PrimaryPane
    {
        get => Pane;
        set => Pane = value;
    }

    [Content]
    public Control? DetailContent
    {
        get => GetValue(DetailContentProperty);
        set => SetValue(DetailContentProperty, value);
    }

    public Control? SecondaryPane
    {
        get => DetailContent;
        set => DetailContent = value;
    }

    public double InitialRatio
    {
        get => GetValue(InitialRatioProperty);
        set => SetValue(InitialRatioProperty, Math.Clamp(value, 0.15, 0.85));
    }

    public double PaneWidth
    {
        get => GetValue(PaneWidthProperty);
        set => SetValue(PaneWidthProperty, Math.Clamp(value, MinPaneWidth, MaxPaneWidth));
    }

    public double MinPaneWidth
    {
        get => GetValue(MinPaneWidthProperty);
        set => SetValue(MinPaneWidthProperty, value);
    }

    public double MaxPaneWidth
    {
        get => GetValue(MaxPaneWidthProperty);
        set => SetValue(MaxPaneWidthProperty, value);
    }

    public bool IsPaneOpen
    {
        get => GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    public bool IsResizable
    {
        get => GetValue(IsResizableProperty);
        set => SetValue(IsResizableProperty, value);
    }

    public double CompactBreakpoint
    {
        get => GetValue(CompactBreakpointProperty);
        set => SetValue(CompactBreakpointProperty, value);
    }

    public bool IsCompact => _isCompact;

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyInitialRatio(e.NewSize.Width);
        UpdateCompactState(e.NewSize.Width);
    }

    /// <summary>
    /// Applies <see cref="InitialRatio"/> once, on the first layout with a known width.
    /// It only decides the starting split; an explicit <see cref="PaneWidth"/> disables it.
    /// </summary>
    private void ApplyInitialRatio(double width)
    {
        if (_paneWidthResolved || width <= 0 || double.IsNaN(width))
            return;

        var ratio = double.IsNaN(InitialRatio) ? 0.35 : Math.Clamp(InitialRatio, 0.15, 0.85);
        var target = Math.Clamp(width * ratio, MinPaneWidth, MaxPaneWidth);
        _paneWidthResolved = true;
        _applyingInitialRatio = true;
        try
        {
            PaneWidth = target;
        }
        finally
        {
            _applyingInitialRatio = false;
        }
    }

    private void UpdateCompactState(double width)
    {
        if (width <= 0) return;
        var compact = width < CompactBreakpoint;
        if (compact == _isCompact) return;
        _isCompact = compact;
        SyncContent();
        SyncLayout();
        CompactModeChanged?.Invoke(this, _isCompact);
    }

    private void SyncContent()
    {
        _inlinePaneHost.Child = null;
        _compactDrawerHost.Child = null;
        _detailHost.Child = null;

        var pane = Pane;
        if (pane is not null)
        {
            MavVisualTreeHelpers.DetachFromParent(pane);
            if (_isCompact)
                _compactDrawerHost.Child = pane;
            else
                _inlinePaneHost.Child = pane;
        }

        var detail = DetailContent;
        if (detail is not null)
        {
            MavVisualTreeHelpers.DetachFromParent(detail);
            _detailHost.Child = detail;
        }
    }

    private void SyncLayout()
    {
        var w = Math.Clamp(PaneWidth, MinPaneWidth, MaxPaneWidth);
        if (_isCompact)
        {
            _inlinePaneHost.IsVisible = false;
            _splitterHit.IsVisible = false;
            _compactDrawerHost.Width = w;
            _compactDrawerHost.IsVisible = IsPaneOpen;
            _compactScrim.IsVisible = IsPaneOpen;
        }
        else
        {
            _compactDrawerHost.IsVisible = false;
            _compactScrim.IsVisible = false;
            _inlinePaneHost.Width = w;
            _inlinePaneHost.IsVisible = IsPaneOpen;
            _splitterHit.IsVisible = IsPaneOpen && IsResizable;
        }
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _outerBorder.Background = MavTheme.BrushOf(c.Surface);
        _borderOverlay.BorderBrush = MavTheme.BrushOf(c.OutlineVariant);
        _inlinePaneHost.Background = MavTheme.BrushOf(c.SurfaceContainerLow);
        _compactDrawerHost.Background = MavTheme.BrushOf(c.SurfaceContainerHigh);
        _detailHost.Background = MavTheme.BrushOf(c.Surface);
        _splitterPill.Background = MavTheme.BrushOf(_draggingSplitter ? c.Primary : c.OutlineVariant);
    }

    private void OnSplitterPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsResizable || _isCompact) return;
        if (MavGestureZones.ShouldDeferToSystem(_splitterHit, e)) return;
        _draggingSplitter = true;
        _dragStartX = e.GetPosition(this).X;
        _dragStartPaneWidth = PaneWidth;
        e.Pointer.Capture(_splitterHit);
        e.PreventGestureRecognition();
        e.Handled = true;
        ApplyTheme(MavTheme.Current);
    }

    private void OnSplitterPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_draggingSplitter) return;
        e.PreventGestureRecognition();
        var dx = e.GetPosition(this).X - _dragStartX;
        var maxAllowed = Bounds.Width > 200 ? Math.Min(MaxPaneWidth, Bounds.Width - 140) : MaxPaneWidth;
        PaneWidth = Math.Clamp(_dragStartPaneWidth + dx, MinPaneWidth, Math.Max(MinPaneWidth, maxAllowed));
        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_draggingSplitter) return;
        _draggingSplitter = false;
        e.Pointer.Capture(null);
        e.Handled = true;
        ApplyTheme(MavTheme.Current);
    }
}
