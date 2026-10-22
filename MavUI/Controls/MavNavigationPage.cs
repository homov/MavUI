using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Animation used when a <see cref="MavNavigationPage"/> changes its page.</summary>
public enum MavNavigationTransition
{
    None,
    Slide,
    Fade,
}

/// <summary>
/// Material Design stack navigation host. It owns its page stack, app bar,
/// bottom bar and floating action button instead of relying on Avalonia page
/// navigation, so every part of the chrome can use Mav controls.
/// </summary>
public class MavNavigationPage : MavContentView
{
    public static readonly StyledProperty<MavNavigationTransition> TransitionProperty =
        AvaloniaProperty.Register<MavNavigationPage, MavNavigationTransition>(
            nameof(Transition),
            MavNavigationTransition.Slide);

    /// <summary>
    /// When true (default) the app bar, bottom bar and FAB respect the
    /// <see cref="IInsetsManager.SafeAreaPadding"/> of the current top level.
    /// </summary>
    public static readonly StyledProperty<bool> ApplySafeAreaProperty =
        AvaloniaProperty.Register<MavNavigationPage, bool>(nameof(ApplySafeArea), true);

    /// <summary>
    /// Forces the safe-area insets instead of reading them from the platform.
    /// Useful for desktop previews and tests; <c>null</c> uses the real insets.
    /// </summary>
    public static readonly StyledProperty<Thickness?> SafeAreaOverrideProperty =
        AvaloniaProperty.Register<MavNavigationPage, Thickness?>(nameof(SafeAreaOverride));

    private readonly List<MavContentPage> _stack = [];
    private readonly Grid _root;
    private readonly MavTopAppBar _appBar;
    private MavNavigationSuiteScaffold? _suite;
    private bool _menuIconShown;
    private readonly Panel _pageHost;
    private readonly MavKeyboardAvoidingHost _keyboardHost;
    private readonly Border _bottomHost;
    private readonly Border _fabHost;
    private readonly Grid _overlayHost;
    private int _transitionVersion;
    private TopLevel? _hookedTopLevel;
    private IInsetsManager? _insetsManager;
    private Thickness _safeAreaInsets;
    private readonly GoBackNavigationCommand _goBackCommand;

    static MavNavigationPage()
    {
        ApplySafeAreaProperty.Changed.AddClassHandler<MavNavigationPage>((p, _) =>
        {
            p.AttachInsetsManager();
            p.ApplySafeAreaInsets();
        });
        SafeAreaOverrideProperty.Changed.AddClassHandler<MavNavigationPage>((p, _) => p.ApplySafeAreaInsets());
    }

    /// <summary>Creates an empty navigation page host.</summary>
    public MavNavigationPage()
    {
        // The page distributes safe-area insets to its app bar, bottom bar and
        // content itself. Disable Avalonia's automatic TopLevel padding so insets
        // are not applied twice.
        TopLevel.SetAutoSafeAreaPadding(this, false);

        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;

        _appBar = new MavTopAppBar
        {
            Variant = MavTopAppBarVariant.Small,
        };
        _appBar.NavigationClicked += (_, _) => OnAppBarNavigationClicked();

        _pageHost = new Panel
        {
            ClipToBounds = true,
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

        var pageLayer = new Grid();
        pageLayer.Children.Add(_pageHost);
        pageLayer.Children.Add(_fabHost);

        _keyboardHost = new MavKeyboardAvoidingHost(pageLayer)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        _bottomHost = new Border
        {
            IsVisible = false,
        };

        _root = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _root.Children.Add(_appBar);
        Grid.SetRow(_appBar, 0);
        _root.Children.Add(_keyboardHost);
        Grid.SetRow(_keyboardHost, 1);
        _root.Children.Add(_bottomHost);
        Grid.SetRow(_bottomHost, 2);

        _overlayHost = new Grid
        {
            IsHitTestVisible = false,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
        };
        Grid.SetRowSpan(_overlayHost, 3);
        _overlayHost.Children.CollectionChanged += (_, _) =>
            _overlayHost.IsHitTestVisible = _overlayHost.Children.Count > 0;
        _root.Children.Add(_overlayHost);

        Child = _root;
        _goBackCommand = new GoBackNavigationCommand(this);
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    /// <summary>Creates a navigation host with <paramref name="rootPage"/> as its root.</summary>
    public MavNavigationPage(MavContentPage rootPage) : this()
    {
        SetRoot(rootPage);
    }

    public event EventHandler<MavPageChangedEventArgs>? PageChanged;
    public event EventHandler? CanGoBackChanged;

    public MavNavigationTransition Transition
    {
        get => GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    public bool ApplySafeArea
    {
        get => GetValue(ApplySafeAreaProperty);
        set => SetValue(ApplySafeAreaProperty, value);
    }

    public Thickness? SafeAreaOverride
    {
        get => GetValue(SafeAreaOverrideProperty);
        set => SetValue(SafeAreaOverrideProperty, value);
    }

    /// <summary>
    /// Insets currently applied to the chrome (status bar, notch, navigation bar).
    /// </summary>
    public Thickness SafeAreaInsets => _safeAreaInsets;

    /// <summary>
    /// Host that lifts the page content above the soft keyboard and scrolls the
    /// focused field into view. Its <see cref="MavKeyboardAvoidingHost.SimulatedKeyboardHeight"/>
    /// can be set on desktop to preview the mobile behavior.
    /// </summary>
    public MavKeyboardAvoidingHost KeyboardHost => _keyboardHost;

    /// <summary>
    /// Vertical space at the bottom of the window occupied by the bottom app bar and
    /// the floating action button. <see cref="MavSnackbar"/> uses it to stay above the chrome.
    /// </summary>
    internal double SnackbarBottomOffset
    {
        get
        {
            // The bottom bar height already includes its safe-area padding.
            var offset = _bottomHost.IsVisible
                ? _bottomHost.Bounds.Height
                : _safeAreaInsets.Bottom;
            if (_fabHost.IsVisible && _fabHost.Child is not null)
                offset += _fabHost.Bounds.Height + 8;
            return offset;
        }
    }

    public IReadOnlyList<MavContentPage> NavigationStack => _stack;
    public int StackDepth => _stack.Count;
    public bool CanGoBack => _stack.Count > 1;
    public MavContentPage? CurrentPage => _stack.Count == 0 ? null : _stack[^1];

    /// <summary>
    /// MVVM-friendly command that follows <see cref="CanGoBack"/> and calls
    /// <see cref="TryGoBack"/>.
    /// </summary>
    public ICommand GoBackCommand => _goBackCommand;

    /// <summary>Replaces the whole stack with <paramref name="page"/> as the new root.</summary>
    public void SetRoot(MavContentPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var old = CurrentPage;
        if (old is not null)
        {
            old.NotifyDisappearing();
            old.Navigation = null;
        }

        DetachPage(page);
        _stack.Clear();
        _stack.Add(page);
        page.Navigation = this;

        page.NotifyAppearing();
        ShowPage(page, null, animated: false, slideFromRight: true);
        RefreshChrome();

        PageChanged?.Invoke(this, new MavPageChangedEventArgs(old, page, MavPageNavigationType.Root));
        NotifyCanGoBackChanged();
    }

    /// <summary>Pushes a page onto the stack, optionally with the transition animation.</summary>
    public void Push(MavContentPage page, bool animated = true)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (_stack.Count == 0)
        {
            SetRoot(page);
            return;
        }

        if (_stack.Contains(page))
            throw new InvalidOperationException("The page is already in this navigation stack.");

        var old = CurrentPage;
        DetachPage(page);
        page.Navigation = this;
        _stack.Add(page);

        old?.NotifyDisappearing();
        page.NotifyAppearing();

        ShowPage(page, old, animated, slideFromRight: true);
        RefreshChrome();

        PageChanged?.Invoke(this, new MavPageChangedEventArgs(old, page, MavPageNavigationType.Push));
        NotifyCanGoBackChanged();
    }

    public bool Pop(bool animated = true) => PopCore(animated, checkPage: true);

    /// <summary>
    /// Handles a system/back key operation. Overlays are dismissed first, then
    /// the current page gets a chance to consume Back, then the stack pops.
    /// </summary>
    public bool TryGoBack()
    {
        if (MavOverlay.TryDismissTop())
            return true;

        var page = CurrentPage;
        if (page?.OnBackPressed() == true)
            return true;

        if (!CanGoBack)
            return false;

        return PopCore(animated: true, checkPage: false);
    }

    private bool PopCore(bool animated, bool checkPage)
    {
        if (_stack.Count <= 1)
            return false;

        var old = CurrentPage!;
        if (checkPage && old.OnBackPressed())
            return true;

        _stack.RemoveAt(_stack.Count - 1);
        old.Navigation = null;
        old.NotifyDisappearing();

        var target = CurrentPage!;
        target.NotifyAppearing();

        ShowPage(target, old, animated, slideFromRight: false);
        RefreshChrome();

        PageChanged?.Invoke(this, new MavPageChangedEventArgs(old, target, MavPageNavigationType.Pop));
        NotifyCanGoBackChanged();
        return true;
    }

    /// <summary>Removes all pages above the root.</summary>
    public void PopToRoot(bool animated = true)
    {
        if (_stack.Count <= 1)
            return;

        var old = CurrentPage;
        while (_stack.Count > 1)
        {
            var removed = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            removed.Navigation = null;
            removed.NotifyDisappearing();
        }

        var root = CurrentPage!;
        root.NotifyAppearing();
        ShowPage(root, old, animated, slideFromRight: false);
        RefreshChrome();

        PageChanged?.Invoke(this, new MavPageChangedEventArgs(old, root, MavPageNavigationType.Pop));
        NotifyCanGoBackChanged();
    }

    /// <summary>Replaces the current page with <paramref name="page"/> without adding a history entry.</summary>
    public void Replace(MavContentPage page, bool animated = true)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (_stack.Count == 0)
        {
            SetRoot(page);
            return;
        }

        var old = CurrentPage!;
        if (ReferenceEquals(old, page))
            return;

        if (_stack.Contains(page))
            throw new InvalidOperationException("The page is already in this navigation stack.");

        _stack.RemoveAt(_stack.Count - 1);
        old.Navigation = null;
        old.NotifyDisappearing();

        DetachPage(page);
        page.Navigation = this;
        _stack.Add(page);
        page.NotifyAppearing();

        ShowPage(page, old, animated, slideFromRight: true);
        RefreshChrome();

        PageChanged?.Invoke(this, new MavPageChangedEventArgs(old, page, MavPageNavigationType.Replace));
        NotifyCanGoBackChanged();
    }

    /// <summary>
    /// Re-applies title, actions, bottom bar and FAB from the current page.
    /// Call this after mutating page metadata while the page is visible.
    /// </summary>
    public void RefreshChrome()
    {
        var page = CurrentPage;
        if (page is null)
            return;

        _appBar.IsVisible = page.ShowAppBar;
        _appBar.Title = page.Title;
        _appBar.Subtitle = page.Subtitle;
        _appBar.Variant = page.AppBarVariant;
        // A root page inside a suite gets the menu icon in compact and rail modes, unless it sets its own icon.
        var showsMenu = !CanGoBack && page.NavigationIcon is null && _suite is { ShowsNavigationMenuButton: true };
        _menuIconShown = showsMenu;
        _appBar.NavigationIcon = CanGoBack && page.ShowBackButton
            ? MavIcons.ArrowBack
            : page.NavigationIcon ?? (showsMenu ? MavIcons.Menu : null);

        _appBar.Actions.Clear();
        foreach (var action in page.Actions)
            _appBar.AddAction(action.Icon, action.OnClick);

        _bottomHost.Child = null;
        _bottomHost.Child = page.BottomBar;
        _bottomHost.IsVisible = page.BottomBar is not null;

        if (_fabHost.Child is Control oldFab)
            MavVisualTreeHelpers.DetachFromParent(oldFab);
        _fabHost.Child = page.FloatingActionButton;
        _fabHost.IsVisible = page.FloatingActionButton is not null;

        ApplySafeAreaInsets();
    }

    private void NotifyCanGoBackChanged()
    {
        _goBackCommand.RaiseCanExecuteChanged();
        CanGoBackChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnAppBarNavigationClicked()
    {
        var page = CurrentPage;
        if (page?.HandleNavigationClick() == true)
            return;

        if (CanGoBack)
        {
            Pop();
            return;
        }

        if (_menuIconShown)
            _suite?.OpenNavigationMenu();
    }

    private void OnSuiteModeChanged(object? sender, MavNavigationSuiteMode mode) => RefreshChrome();

    /// <summary>Shows the current page and raises its appearing notification.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _suite = this.FindAncestorOfType<MavNavigationSuiteScaffold>();
        if (_suite is not null)
        {
            _suite.EffectiveModeChanged += OnSuiteModeChanged;
            RefreshChrome();
        }

        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            _hookedTopLevel = topLevel;

            // Android and iOS may host the page inside a wrapper control. That wrapper
            // is the TopLevel.Content and would otherwise receive automatic safe-area
            // padding in addition to the insets this page applies itself.
            TopLevel.SetAutoSafeAreaPadding(this, false);
            if (topLevel.Content is Control host && !ReferenceEquals(host, this))
                TopLevel.SetAutoSafeAreaPadding(host, false);

            topLevel.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
            topLevel.AddHandler(TopLevel.BackRequestedEvent, OnTopLevelBackRequested);
            topLevel.ScalingChanged += OnTopLevelScalingChanged;
            MavOverlay.RegisterHost(this, _overlayHost);

            // RenderScaling may still be 1 while the platform finishes its first
            // layout. Re-read the insets once layout has settled.
            Dispatcher.UIThread.Post(ApplySafeAreaInsets, DispatcherPriority.Loaded);
        }

        AttachInsetsManager();
        ApplySafeAreaInsets();
    }

    /// <summary>Raises the disappearing notification for the current page.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_suite is not null)
        {
            _suite.EffectiveModeChanged -= OnSuiteModeChanged;
            _suite = null;
        }

        if (_hookedTopLevel is not null)
        {
            _hookedTopLevel.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
            _hookedTopLevel.RemoveHandler(TopLevel.BackRequestedEvent, OnTopLevelBackRequested);
            _hookedTopLevel.ScalingChanged -= OnTopLevelScalingChanged;
            _hookedTopLevel = null;
        }

        DetachInsetsManager();
        MavOverlay.UnregisterHost(this);
        base.OnDetachedFromVisualTree(e);
    }

    private void AttachInsetsManager()
    {
        DetachInsetsManager();
        if (!ApplySafeArea || SafeAreaOverride is not null)
            return;

        _insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insetsManager is not null)
            _insetsManager.SafeAreaChanged += OnSafeAreaChanged;
    }

    private void DetachInsetsManager()
    {
        if (_insetsManager is not null)
        {
            _insetsManager.SafeAreaChanged -= OnSafeAreaChanged;
            _insetsManager = null;
        }
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => ApplySafeAreaInsets();

    private void OnTopLevelScalingChanged(object? sender, EventArgs e) => ApplySafeAreaInsets();

    /// <summary>
    /// Distributes safe-area insets to the app bar, bottom bar and FAB so their
    /// surfaces still fill the inset area while content stays inside it.
    /// </summary>
    private void ApplySafeAreaInsets()
    {
        var insets = ApplySafeArea
            ? SafeAreaOverride ?? _insetsManager?.SafeAreaPadding ?? default
            : default;

        _safeAreaInsets = insets;

        _appBar.SafeAreaPadding = new Thickness(insets.Left, insets.Top, insets.Right, 0);
        _pageHost.Margin = new Thickness(
            insets.Left,
            0,
            insets.Right,
            _bottomHost.IsVisible ? 0 : insets.Bottom);

        if (_bottomHost.Child is MavBottomAppBar bottomBar)
            bottomBar.SafeAreaPadding = new Thickness(insets.Left, 0, insets.Right, insets.Bottom);

        // The page-level FAB sits in the content row. When there is no bottom bar,
        // that row reaches the window edge, so the gesture/navigation inset has to
        // be added to the FAB margin as well.
        var fabBottom = 16 + (_bottomHost.IsVisible ? 0 : insets.Bottom);
        _fabHost.Margin = new Thickness(16, 16, 16 + insets.Right, fabBottom);
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
            return;

        var isBackKey = (e.Key == Key.Escape && !OperatingSystem.IsAndroid()) ||
                        (e.Key == Key.Left && e.KeyModifiers.HasFlag(KeyModifiers.Alt));
        if (isBackKey && TryGoBack())
            e.Handled = true;
    }

    private void OnTopLevelBackRequested(object? sender, RoutedEventArgs e)
    {
        if (e.Handled)
            return;

        if (TryGoBack())
            e.Handled = true;
    }

    private void ShowPage(
        MavContentPage page,
        MavContentPage? previous,
        bool animated,
        bool slideFromRight)
    {
        var version = ++_transitionVersion;
        MavMotion.Cancel(page, "md_nav_transition");
        if (previous is not null)
            MavMotion.Cancel(previous, "md_nav_transition");

        DetachPage(page);
        _pageHost.Children.Clear();
        _pageHost.Children.Add(page);
        page.IsVisible = true;
        page.Opacity = 1;
        page.SetTranslation(0, 0);

        var transition = animated ? Transition : MavNavigationTransition.None;
        if (transition == MavNavigationTransition.None)
            return;

        const double offset = 36;
        var start = transition == MavNavigationTransition.Slide
            ? (slideFromRight ? offset : -offset)
            : 0;

        page.Opacity = 0;
        page.SetTranslation(start, 0);

        MavMotion.Animate(
            page,
            "md_nav_transition",
            0,
            1,
            MavMotion.Medium2,
            MavMotion.CubicOut,
            progress =>
            {
                if (version != _transitionVersion)
                    return;

                page.Opacity = progress;
                page.SetTranslation(start * (1 - progress), 0);
            },
            () =>
            {
                if (version != _transitionVersion)
                    return;

                page.Opacity = 1;
                page.SetTranslation(0, 0);
            });
    }

    private static void DetachPage(Control page)
    {
        if (page.Parent is Panel panel)
            panel.Children.Remove(page);
        else if (page.Parent is Decorator decorator && ReferenceEquals(decorator.Child, page))
            decorator.Child = null;
        else if (page.Parent is ContentControl contentControl && ReferenceEquals(contentControl.Content, page))
            contentControl.Content = null;
    }

    private void ApplyTheme(MavTheme theme)
    {
        Background = MavTheme.BrushOf(theme.Color.Background);
    }

    private sealed class GoBackNavigationCommand(MavNavigationPage owner) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => owner.CanGoBack;

        public void Execute(object? parameter) => owner.TryGoBack();

        public void RaiseCanExecuteChanged() =>
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
