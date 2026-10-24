using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Mav;
using Mav.Controls;
using Mav.ControlsGallery.Localization;
using Mav.Platform;
using Mav.Theming;

namespace Mav.ControlsGallery.Shell;

/// <summary>
/// Adaptive gallery shell. The layout is AXAML; this class only wires navigation,
/// the adaptive breakpoints and the app-bar actions.
/// </summary>
public partial class MainView : UserControl
{
    private const double RailBreakpoint = 700;
    private const double DrawerBreakpoint = 1000;
    private const int DefaultDestination = 0;

    private readonly Control?[] _pages = new Control?[GalleryDestinations.All.Length];
    private MavNavigationDrawer? _drawer;
    private MavNavigationRail? _rail;
    private MavModalNavigationDrawer? _modal;
    private int _current = -1;
    private bool _syncing;

    public MainView()
    {
        InitializeComponent();

        // Without a window template (browser, mobile) the top level has no overlay layer, so the
        // overlays (snackbars, dialogs, menus) would silently not show. This host takes them instead.
        MavOverlay.RegisterHost(this, OverlayHost);

        Page.AddAction(MavIcons.DarkMode, () => MavTheme.ToggleDark(), "Toggle dark mode");
        Page.AddAction(MavIcons.Globe, ShowLanguageMenu, MavLocalization.Get("Language.Title", "Language"));
        Page.AddAction(MavIcons.Info, ShowAbout, "About this gallery");
        Page.NavigationAction = OpenDrawer;

        // A MavContentPage draws no app bar of its own: the navigation host does. Move the page into
        // the host so the top bar, its navigation icon (menu on narrow windows) and actions appear.
        if (Page.Parent is Panel owner)
            owner.Children.Remove(Page);
        Nav.SetRoot(Page);

        SizeChanged += (_, _) => UpdateAdaptive();

        // Navigation labels are plain strings, so the navigation is rebuilt on language changes.
        MavLocalization.Observe(this, view => view.RebuildNavigation());
        Select(DefaultDestination);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        MavOverlay.UnregisterHost(this);
    }

    private void RebuildNavigation()
    {
        var title = MavLocalization.Get("Shell.AppTitle", "Mav Controls");
        _drawer = new MavNavigationDrawer(title, 280);
        _rail = new MavNavigationRail();
        _modal = new MavModalNavigationDrawer(title, 300);

        for (var i = 0; i < GalleryDestinations.All.Length; i++)
        {
            var index = i;
            var destination = GalleryDestinations.All[i];
            _drawer.AddItem(destination.Label, destination.Icon, onClick: () => Select(index));
            _rail.AddDestination(destination.Label, destination.Icon, destination.Icon);
            _modal.AddItem(destination.Label, destination.Icon, onClick: () => Select(index));
        }

        _drawer.SelectedIndexChanged += (_, index) => { if (!_syncing) Select(index); };
        _rail.SelectedIndexChanged += (_, index) => { if (!_syncing) Select(index); };
        _modal.Drawer.SelectedIndexChanged += (_, index) => { if (!_syncing) Select(index); };

        DrawerHost.Content = _drawer;
        RailHost.Content = _rail;

        // Pages built in code (for example, elevation captions) keep the language they were built in,
        // so drop the cache and rebuild the visible page.
        Array.Clear(_pages);
        Select(_current);
        UpdateAdaptive();
    }

    /// <summary>
    /// Opens the destination with the given key ("Overview", "Actions", …). Used by deep links and by the
    /// visual tests, which screenshot every destination through the real shell.
    /// </summary>
    public bool Open(string key)
    {
        var index = Array.FindIndex(GalleryDestinations.All, d => d.Key == key);
        if (index < 0)
            return false;
        Select(index);
        return true;
    }

    private void Select(int index)
    {
        if (index < 0 || index >= GalleryDestinations.All.Length)
            return;

        _current = index;
        var destination = GalleryDestinations.All[index];

        Page.Title = destination.Label;
        Page.Subtitle = destination.Subtitle;

        Host.Content = _pages[index] ??= destination.Build();
        Nav.RefreshChrome();
        SyncSelection();

        if (_modal?.IsOpen == true)
            _modal.Close();
    }

    private void SyncSelection()
    {
        _syncing = true;
        if (_drawer is not null && _drawer.SelectedIndex != _current)
            _drawer.SelectedIndex = _current;
        if (_rail is not null && _rail.SelectedIndex != _current)
            _rail.SelectedIndex = _current;
        if (_modal is not null && _modal.Drawer.SelectedIndex != _current)
            _modal.Drawer.SelectedIndex = _current;
        _syncing = false;
    }

    private void UpdateAdaptive()
    {
        var width = Bounds.Width;
        if (width <= 0)
            width = 400;

        var wide = width >= DrawerBreakpoint;
        var medium = width >= RailBreakpoint && !wide;

        DrawerHost.IsVisible = wide;
        RailScroll.IsVisible = medium;
        Grid.SetColumn(Host, wide || medium ? 1 : 0);
        Grid.SetColumnSpan(Host, wide || medium ? 1 : 2);

        Page.NavigationIcon = wide || medium ? null : MavIcons.Menu;
        Nav.RefreshChrome();
    }

    private void OpenDrawer()
    {
        if (Bounds.Width >= DrawerBreakpoint || _modal is null)
            return;

        _modal.Show(Page);
    }

    /// <summary>Opens the language menu: every language of <see cref="GalleryLanguage.All"/>, plus the system one.</summary>
    private void ShowLanguageMenu()
    {
        var menu = new MavMenu();
        var active = GalleryLanguage.Resolve();

        menu.AddItem(
            MavLocalization.Get("Language.System", "System"),
            trailingText: active.EnglishName,
            onSelected: () => UseLanguage(null),
            isSelected: GalleryLanguage.Selected is null);

        foreach (var language in GalleryLanguage.All)
        {
            menu.AddItem(
                language.Name,
                trailingText: language.EnglishName,
                onSelected: () => UseLanguage(language),
                isSelected: ReferenceEquals(language, active));
        }

        // Anchor to the globe action in the app bar rather than to the full page: otherwise
        // the popup would open at the bottom of the screen, away from the button just tapped.
        var globe = Nav.GetVisualDescendants().OfType<MavTopAppBar>().FirstOrDefault()?
            .Actions.OfType<MavIconButton>().FirstOrDefault(button => button.Icon == MavIcons.Globe);
        menu.Show(globe ?? (Control)Page);
    }

    /// <summary>Switches the gallery to <paramref name="language"/>; <c>null</c> follows the system again.</summary>
    private void UseLanguage(GalleryLanguage? language)
    {
        GalleryLanguage.Select(language);

        // Say what happened in the new language: the snackbar is built after the switch.
        MavSnackbar.Show(
            MavLocalization.Format("Language.Current", "Language: {0}", GalleryLanguage.Resolve().Name),
            anchor: Page);
    }

    private void ShowAbout()
    {
        var text = MavLocalization.Format(
            "Shell.AboutText",
            "{0} groups · 90+ controls",
            GalleryDestinations.All.Length);
        MavSnackbar.Show(text, anchor: Page);
    }
}
