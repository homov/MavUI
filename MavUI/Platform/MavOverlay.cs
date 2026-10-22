using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Controls;

namespace Mav.Platform;

/// <summary>
/// Full-window overlay host backed by Avalonia's <see cref="OverlayLayer"/>
/// for dialogs, bottom/side sheets, date/time pickers, menus, tooltips, and snackbars.
/// Automatically intercepts hardware Back button / Escape key and shifts overlays above the soft keyboard.
/// </summary>
public static class MavOverlay
{
    private static readonly ConditionalWeakTable<Panel, OverlayRootGrid> OverlayMap = new();
    private static readonly List<OverlayHostEntry> Hosts = [];
    private static readonly List<OverlayHandle> DismissStack = [];
    private static readonly List<OverlayHandle> OverlayHandles = [];
    private static readonly List<WeakReference<OverlayRootGrid>> OverlayRoots = [];
    private static readonly ConditionalWeakTable<TopLevel, object> DismissLifecycleHooks = new();
    private static readonly object DismissLifecycleMarker = new();
    private static WeakReference<TopLevel>? _lastTopLevel;
    private static bool _layoutNotificationQueued;
    private static double _simulatedKeyboardHeight;

    static MavOverlay()
    {
        TopLevel.BackRequestedEvent.AddClassHandler<TopLevel>((_, e) =>
        {
            if (!e.Handled && TryDismissTop())
                e.Handled = true;
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct);

        Page.PageNavigationSystemBackButtonPressedEvent.AddClassHandler<Page>((_, e) =>
        {
            if (!e.Handled && TryDismissTop())
                e.Handled = true;
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct);

        InputElement.KeyDownEvent.AddClassHandler<TopLevel>((_, e) =>
        {
            // On Android the hardware Back key is surfaced as Escape by the input
            // backend, but it also arrives as TopLevel.BackRequested. Handling both
            // would dismiss an overlay and then let the activity navigate back.
            if (!e.Handled &&
                e.Key == Key.Escape &&
                !OperatingSystem.IsAndroid() &&
                TryDismissTop())
            {
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    /// <summary>
    /// Ensures static back-button and Escape handlers are registered at application startup.
    /// </summary>
    public static void Initialize()
    {
        // Triggers static constructor if not already initialized.
    }

    /// <summary>
    /// Dismisses the topmost active modal/popup overlay (dialog, sheet, picker, menu, etc.), if any.
    /// Returns <see langword="true"/> if an overlay was open and dismissed.
    /// </summary>
    public static bool TryDismissTop()
    {
        for (int i = DismissStack.Count - 1; i >= 0; i--)
        {
            var handle = DismissStack[i];
            DismissStack.RemoveAt(i);
            if (handle.TryInvokeDismiss())
                return true;
        }
        return false;
    }

    public static bool DismissTopMost(TopLevel? _ = null) => TryDismissTop();

    /// <summary>
    /// Registers an external popup/flyout dismiss callback on the Back/Escape stack until disposed.
    /// </summary>
    public static IDisposable PushDismissable(Action onDismiss, TopLevel? topLevel = null)
    {
        ArgumentNullException.ThrowIfNull(onDismiss);

        topLevel ??= _lastTopLevel is not null && _lastTopLevel.TryGetTarget(out var cached)
            ? cached
            : null;

        var handle = new OverlayHandle(null, null, onDismiss, topLevel: topLevel);
        DismissStack.Add(handle);
        OverlayHandles.Add(handle);

        if (topLevel is not null)
            EnsureTopLevelDismissCleanup(topLevel);

        return handle;
    }

    private static void EnsureTopLevelDismissCleanup(TopLevel topLevel)
    {
        if (DismissLifecycleHooks.TryGetValue(topLevel, out _))
            return;

        DismissLifecycleHooks.Add(topLevel, DismissLifecycleMarker);
        topLevel.Closed += OnDismissTopLevelClosed;
    }

    private static void OnDismissTopLevelClosed(object? sender, EventArgs e)
    {
        if (sender is TopLevel topLevel)
            DisposeOverlaysForTopLevel(topLevel);
    }

    public static bool HasActiveOverlay => DismissStack.Count > 0;

    /// <summary>
    /// Closes overlays that were opened from the given page. Used when a page
    /// leaves the navigation stack so dialogs/sheets do not outlive their owner.
    /// </summary>
    public static void DismissForPage(MavContentPage page)
    {
        // Callbacks are allowed to show/close other overlays, so iterate a snapshot
        // instead of the live static list.
        foreach (var handle in OverlayHandles.ToArray())
        {
            if (!handle.IsOwnedBy(page))
                continue;

            handle.TryInvokeDismiss();
            handle.Dispose();
        }
    }

    public static void TrackTopLevel(Visual? visual)
    {
        if (visual is null) return;
        if (TopLevel.GetTopLevel(visual) is { } tl)
            _lastTopLevel = new WeakReference<TopLevel>(tl);
    }

    /// <summary>
    /// Registers a Material overlay host for environments where the top level has no
    /// <see cref="VisualLayerManager"/> / <see cref="OverlayLayer"/> (for example the
    /// Android embeddable root when no Avalonia theme is installed).
    /// </summary>
    public static void RegisterHost(Control owner, Panel host)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(host);
        PruneHosts();
        UnregisterHost(owner);
        Hosts.Add(new OverlayHostEntry(new WeakReference<Control>(owner), new WeakReference<Panel>(host)));
    }

    public static void UnregisterHost(Control owner)
    {
        PruneHosts();
        for (int i = Hosts.Count - 1; i >= 0; i--)
        {
            if (Hosts[i].Owner.TryGetTarget(out var target) && ReferenceEquals(target, owner))
                Hosts.RemoveAt(i);
        }
    }

    /// <summary>
    /// Drops host registrations whose owner or host was collected. Called on host
    /// changes and before resolving a container so the static list cannot grow
    /// forever as windows/controls are created and closed.
    /// </summary>
    private static void PruneHosts()
    {
        for (int i = Hosts.Count - 1; i >= 0; i--)
        {
            if (!Hosts[i].Owner.TryGetTarget(out _) || !Hosts[i].Host.TryGetTarget(out _))
                Hosts.RemoveAt(i);
        }
    }

    public static TopLevel? ResolveTopLevel(Visual? anchor)
    {
        if (anchor is not null && TopLevel.GetTopLevel(anchor) is { } fromAnchor)
        {
            _lastTopLevel = new WeakReference<TopLevel>(fromAnchor);
            return fromAnchor;
        }

        if (_lastTopLevel is not null && _lastTopLevel.TryGetTarget(out var cached))
            return cached;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var win = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
            if (win is not null)
            {
                _lastTopLevel = new WeakReference<TopLevel>(win);
                return win;
            }
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            try
            {
                if (singleView.MainView is { } mainView &&
                    TopLevel.GetTopLevel(mainView) is { } singleTl)
                {
                    _lastTopLevel = new WeakReference<TopLevel>(singleTl);
                    return singleTl;
                }
            }
            catch (InvalidOperationException)
            {
                // BrowserSingleViewLifetime.MainView throws if queried during Application.Initialize()
                // before AfterApplicationSetupCallback creates the AvaloniaView.
            }
        }

        return null;
    }

    public static IDisposable Show(
        Visual? anchor,
        Control content,
        Color? scrimColor = null,
        Action? onScrimTap = null,
        bool passThroughScrim = false,
        Action? onViewportChanged = null,
        Action? onClosed = null)
    {
        MavKeyboard.Hide(anchor);

        var overlayRoot = EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return NoopDisposable.Instance;

        var layer = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var owner = anchor as MavContentPage ?? anchor?.FindAncestorOfType<MavContentPage>();
        var handle = new OverlayHandle(
            overlayRoot,
            layer,
            onScrimTap,
            owner,
            onViewportChanged,
            onClosed: onClosed);
        OverlayHandles.Add(handle);

        if (!passThroughScrim)
        {
            var scrim = new Border
            {
                Background = scrimColor is { } sc && sc.A > 0 ? Theming.MavTheme.BrushOf(sc) : Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            scrim.PointerPressed += (_, e) =>
            {
                e.Handled = true;
                handle.TryInvokeDismiss();
            };
            layer.Children.Add(scrim);
        }

        Primitives.MavVisualTreeHelpers.DetachFromParent(content);
        layer.Children.Add(content);
        overlayRoot.Children.Add(layer);

        if (onScrimTap is not null)
            DismissStack.Add(handle);

        return handle;
    }

    /// <summary>
    /// Shows a popup card anchored to <paramref name="anchor"/>, automatically clamping to screen bounds
    /// and flipping above the anchor when there is insufficient space below.
    /// </summary>
    public static IDisposable ShowAnchoredPopup(
        Visual anchor,
        Control popupCard,
        double preferredWidth = 0,
        double maxHeight = 280,
        double verticalOffset = 4,
        Action? onDismiss = null,
        Action? onClosed = null)
    {
        var overlayRoot = EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return NoopDisposable.Instance;

        void Place()
        {
            var bounds = BoundsInOverlay(anchor, overlayRoot);
            var overlaySize = GetOverlaySize(anchor);
            var insets = SafeAreaOf(anchor);
            var width = preferredWidth > 0 ? preferredWidth : Math.Max(160, bounds.Width);

            var left = Math.Clamp(
                bounds.X,
                insets.Left + 8,
                Math.Max(insets.Left + 8, overlaySize.Width - insets.Right - width - 8));
            var spaceBelow = overlaySize.Height - insets.Bottom - bounds.Bottom - verticalOffset - 12;
            var spaceAbove = bounds.Y - insets.Top - verticalOffset - 12;

            double top;
            if (spaceBelow >= Math.Min(180, maxHeight) || spaceBelow >= spaceAbove)
            {
                top = Math.Max(insets.Top + 8, bounds.Bottom + verticalOffset);
            }
            else
            {
                var estH = Math.Min(maxHeight, Math.Max(120, spaceAbove));
                top = Math.Max(insets.Top + 8, bounds.Y - estH - verticalOffset);
            }

            popupCard.Width = width;
            popupCard.MaxWidth = Math.Max(96, overlaySize.Width - insets.Left - insets.Right - 16);
            popupCard.MaxHeight = Math.Max(
                120,
                Math.Min(maxHeight, overlaySize.Height - insets.Top - insets.Bottom - 16));
            popupCard.HorizontalAlignment = HorizontalAlignment.Left;
            popupCard.VerticalAlignment = VerticalAlignment.Top;
            popupCard.Margin = new Thickness(left, top, 0, 0);
        }

        Place();

        IDisposable? handle = null;
        handle = Show(
            anchor,
            popupCard,
            scrimColor: Colors.Transparent,
            onScrimTap: () =>
            {
                handle?.Dispose();
                onDismiss?.Invoke();
            },
            onViewportChanged: Place,
            onClosed: onClosed);
        Theming.MavMotion.ScaleIn(popupCard, 0.94, Theming.MavMotion.Short3);
        return handle;
    }

    /// <summary>
    /// Current safe-area insets of the top level that hosts <paramref name="anchor"/>
    /// (display cutout, status bar, gesture bar). Empty when no insets manager exists.
    /// </summary>
    public static Thickness SafeAreaOf(Visual? anchor) =>
        ResolveTopLevel(anchor)?.InsetsManager?.SafeAreaPadding ?? default;

    /// <summary>
    /// Overlay viewport deflated by the safe-area insets, in overlay coordinates.
    /// </summary>
    public static Rect SafeAreaBounds(Visual? anchor)
    {
        var size = GetOverlaySize(anchor);
        var insets = SafeAreaOf(anchor);
        var width = Math.Max(0, size.Width - insets.Left - insets.Right);
        var height = Math.Max(0, size.Height - insets.Top - insets.Bottom);
        return new Rect(insets.Left, insets.Top, width, height);
    }

    /// <summary>
    /// Mirrors <see cref="MavKeyboardAvoidingHost.SimulatedKeyboardHeight"/> so overlays are
    /// lifted above the preview keyboard too, exactly like they are above the real IME.
    /// </summary>
    internal static void SetSimulatedKeyboardHeight(double height)
    {
        height = Math.Max(0, height);
        if (Math.Abs(_simulatedKeyboardHeight - height) < 0.5) return;

        _simulatedKeyboardHeight = height;
        for (int i = OverlayRoots.Count - 1; i >= 0; i--)
        {
            if (OverlayRoots[i].TryGetTarget(out var root))
                root.RefreshKeyboardInset();
            else
                OverlayRoots.RemoveAt(i);
        }

        NotifyViewportChanged();
    }

    internal static void NotifyViewportChanged()
    {
        if (_layoutNotificationQueued) return;
        _layoutNotificationQueued = true;

        var dispatcher = Dispatcher.UIThread;
        dispatcher.Post(() =>
        {
            _layoutNotificationQueued = false;
            foreach (var handle in OverlayHandles.ToArray())
                handle.NotifyViewportChanged();
        }, DispatcherPriority.Loaded);
    }

    public static Rect BoundsInOverlay(Visual element, Visual overlayRoot)
    {
        try
        {
            var pt = element.TranslatePoint(default, overlayRoot);
            if (pt.HasValue)
                return new Rect(pt.Value.X, pt.Value.Y, element.Bounds.Width, element.Bounds.Height);
        }
        catch
        {
            // Fallback to visual parent chain below.
        }

        double x = 0;
        double y = 0;
        Visual? cur = element;
        while (cur is not null && !ReferenceEquals(cur, overlayRoot))
        {
            x += cur.Bounds.X;
            y += cur.Bounds.Y;
            cur = cur.GetVisualParent();
        }
        return new Rect(x, y, element.Bounds.Width, element.Bounds.Height);
    }

    public static Size GetOverlaySize(Visual? anchor)
    {
        var root = EnsureOverlayRoot(anchor);
        if (root is not null && root.Bounds.Width > 0 && root.Bounds.Height > 0)
            return new Size(root.Bounds.Width, root.Bounds.Height);

        var tl = ResolveTopLevel(anchor);
        if (tl is not null && tl.ClientSize.Width > 0 && tl.ClientSize.Height > 0)
            return tl.ClientSize;

        return new Size(400, 720);
    }

    internal static Grid? EnsureOverlayRoot(Visual? anchor)
    {
        var topLevel = ResolveTopLevel(anchor);
        if (topLevel is null) return null;

        var container = ResolveOverlayContainer(anchor, topLevel);
        if (container is null) return null;

        if (OverlayMap.TryGetValue(container, out var existing))
        {
            if (!container.Children.Contains(existing))
                container.Children.Add(existing);
            return existing;
        }

        var rootGrid = new OverlayRootGrid(topLevel, container);
        container.Children.Add(rootGrid);
        OverlayMap.Add(container, rootGrid);
        return rootGrid;
    }

    private static void DisposeOverlaysForTopLevel(TopLevel topLevel)
    {
        PruneHosts();

        // Handles are held in static lists, so they must be released before the
        // window (and its overlay host) can be collected. Snapshot both lists:
        // onClosed callbacks may add/remove handles and overlay roots.
        foreach (var handle in OverlayHandles.ToArray())
        {
            if (handle.IsHostedBy(topLevel))
                handle.Dispose();
        }

        foreach (var weakRoot in OverlayRoots.ToArray())
        {
            if (!weakRoot.TryGetTarget(out var root))
            {
                OverlayRoots.Remove(weakRoot);
                continue;
            }

            if (!ReferenceEquals(root.HostTopLevel, topLevel))
                continue;

            root.Close();
            OverlayRoots.Remove(weakRoot);
        }
    }

    private static Panel? ResolveOverlayContainer(Visual? anchor, TopLevel topLevel)
    {
        PruneHosts();

        if (anchor is not null && OverlayLayer.GetOverlayLayer(anchor) is { } anchoredLayer)
            return anchoredLayer;

        if (OverlayLayer.GetOverlayLayer(topLevel) is { } topLevelLayer)
            return topLevelLayer;

        var scope = anchor ?? topLevel;
        foreach (var visual in scope.GetSelfAndVisualAncestors())
        {
            if (FindHost(visual) is { } host)
                return host;
        }

        if (FindHost(topLevel) is { } topLevelHost)
            return topLevelHost;

        for (int i = Hosts.Count - 1; i >= 0; i--)
        {
            if (Hosts[i].Host.TryGetTarget(out var fallback))
                return fallback;
        }

        return null;
    }

    private static Panel? FindHost(Visual owner)
    {
        for (int i = Hosts.Count - 1; i >= 0; i--)
        {
            if (Hosts[i].Owner.TryGetTarget(out var candidate) &&
                ReferenceEquals(candidate, owner) &&
                Hosts[i].Host.TryGetTarget(out var host))
            {
                return host;
            }
        }
        return null;
    }

    private sealed class OverlayRootGrid : Grid
    {
        private readonly TopLevel _topLevel;
        private readonly Panel _container;
        private IInputPane? _pane;
        private IInsetsManager? _insets;
        private bool _closed;

        public TopLevel HostTopLevel => _topLevel;

        public OverlayRootGrid(TopLevel topLevel, Panel container)
        {
            _topLevel = topLevel;
            _container = container;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            ClipToBounds = true;

            // An empty overlay root must not intercept pointer input: on Android the
            // fallback host sits above the page and would swallow every tap.
            IsHitTestVisible = false;
            Children.CollectionChanged += (_, _) => IsHitTestVisible = Children.Count > 0;

            _topLevel.BackRequested += OnTopLevelBackRequested;
            _topLevel.Closed += OnTopLevelClosed;
            _topLevel.PropertyChanged += OnTopLevelPropertyChanged;
            _topLevel.ScalingChanged += OnTopLevelScalingChanged;
            SizeChanged += OnSizeChanged;

            _container.SizeChanged += OnContainerSizeChanged;
            if (_topLevel.InsetsManager is { } insets)
            {
                _insets = insets;
                insets.SafeAreaChanged += OnSafeAreaChanged;
            }

            if (_topLevel.InputPane is { } inputPane)
            {
                _pane = inputPane;
                inputPane.StateChanged += OnInputPaneStateChanged;
            }

            OverlayRoots.Add(new WeakReference<OverlayRootGrid>(this));
            RefreshKeyboardInset();
        }

        private void OnTopLevelBackRequested(object? sender, RoutedEventArgs e)
        {
            if (!e.Handled && TryDismissTop())
                e.Handled = true;
        }

        private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != TopLevel.ClientSizeProperty)
                return;

            InvalidateMeasure();
            NotifyViewportChanged();
        }

        private void OnTopLevelScalingChanged(object? sender, EventArgs e) =>
            NotifyViewportChanged();

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) =>
            NotifyViewportChanged();

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e) =>
            NotifyViewportChanged();

        private void OnContainerSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            InvalidateMeasure();
            NotifyViewportChanged();
        }

        private void OnTopLevelClosed(object? sender, EventArgs e) =>
            DisposeOverlaysForTopLevel(_topLevel);

        private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
        {
            RefreshKeyboardInset();
            NotifyViewportChanged();
        }

        internal void Close()
        {
            if (_closed) return;
            _closed = true;

            _topLevel.BackRequested -= OnTopLevelBackRequested;
            _topLevel.Closed -= OnTopLevelClosed;
            _topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
            _topLevel.ScalingChanged -= OnTopLevelScalingChanged;
            SizeChanged -= OnSizeChanged;
            _container.SizeChanged -= OnContainerSizeChanged;
            if (_insets is not null)
                _insets.SafeAreaChanged -= OnSafeAreaChanged;
            if (_pane is not null)
                _pane.StateChanged -= OnInputPaneStateChanged;

            _container.Children.Remove(this);
            OverlayMap.Remove(_container);
            Children.Clear();
        }

        /// <summary>
        /// Lifts the overlay content above the soft keyboard. Uses the real input pane and,
        /// for desktop previews, the simulated height of <see cref="MavKeyboardAvoidingHost"/>.
        /// </summary>
        internal void RefreshKeyboardInset()
        {
            var bottom = _simulatedKeyboardHeight;
            if (_pane is { State: InputPaneState.Open } pane)
                bottom = Math.Max(bottom, Math.Max(0, pane.OccludedRect.Height));
            Margin = new Thickness(0, 0, 0, bottom);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Canvas-based OverlayLayer measures children with infinity; its Bounds can
            // still be stale (often zero) while the root is being created. Always use
            // the top-level client size for a top-level overlay layer instead.
            var hostWidth = _container is OverlayLayer
                ? _topLevel.ClientSize.Width
                : _container.Bounds.Width;
            var hostHeight = _container is OverlayLayer
                ? _topLevel.ClientSize.Height
                : _container.Bounds.Height;

            var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0
                ? availableSize.Width
                : (hostWidth > 0 ? hostWidth : _topLevel.ClientSize.Width);
            var h = double.IsFinite(availableSize.Height) && availableSize.Height > 0
                ? availableSize.Height
                : (hostHeight > 0 ? hostHeight : _topLevel.ClientSize.Height);

            if (w <= 0) w = 400;
            if (h <= 0) h = 720;

            var targetSize = new Size(w, h);
            base.MeasureOverride(targetSize);
            return targetSize;
        }
    }

    private sealed class OverlayHandle : IDisposable
    {
        private readonly WeakReference<MavContentPage>? _owner;
        private readonly WeakReference<TopLevel>? _topLevel;
        private Grid? _host;
        private Grid? _layer;
        private Action? _onDismiss;
        private Action? _onViewportChanged;
        private Action? _onClosed;
        private bool _disposed;
        private bool _dismissed;

        public OverlayHandle(
            Grid? host,
            Grid? layer,
            Action? onDismiss,
            MavContentPage? owner = null,
            Action? onViewportChanged = null,
            TopLevel? topLevel = null,
            Action? onClosed = null)
        {
            _host = host;
            _layer = layer;
            _onDismiss = onDismiss;
            _onViewportChanged = onViewportChanged;
            _onClosed = onClosed;
            _owner = owner is null ? null : new WeakReference<MavContentPage>(owner);
            _topLevel = topLevel is null ? null : new WeakReference<TopLevel>(topLevel);
        }

        public bool IsOwnedBy(MavContentPage page) =>
            _owner is not null && _owner.TryGetTarget(out var target) && ReferenceEquals(target, page);

        public bool IsHostedBy(TopLevel topLevel) =>
            (_host is OverlayRootGrid root && ReferenceEquals(root.HostTopLevel, topLevel)) ||
            (_topLevel is not null &&
             _topLevel.TryGetTarget(out var cached) &&
             ReferenceEquals(cached, topLevel));

        public void NotifyViewportChanged()
        {
            if (_disposed) return;
            _onViewportChanged?.Invoke();
        }

        public bool TryInvokeDismiss()
        {
            if (_disposed || _dismissed || _onDismiss is null)
                return false;

            var onDismiss = _onDismiss;
            _onDismiss = null;
            _dismissed = true;
            DismissStack.Remove(this);
            onDismiss();
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            DismissStack.Remove(this);
            OverlayHandles.Remove(this);

            var host = _host;
            var layer = _layer;
            _host = null;
            _layer = null;
            _onDismiss = null;
            _onViewportChanged = null;

            if (host is not null && layer is not null)
                host.Children.Remove(layer);
            // Release the overlay content even when the layer was already detached
            // by the host (top-level close, page teardown).
            layer?.Children.Clear();

            var onClosed = _onClosed;
            _onClosed = null;
            onClosed?.Invoke();
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();
        public void Dispose() { }
    }

    private sealed class OverlayHostEntry(
        WeakReference<Control> owner,
        WeakReference<Panel> host)
    {
        public WeakReference<Control> Owner { get; } = owner;
        public WeakReference<Panel> Host { get; } = host;
    }
}
