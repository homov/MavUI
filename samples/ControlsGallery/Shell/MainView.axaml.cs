using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Mav;
using Mav.Controls;
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
    private const int DefaultDestination = 8;

    private readonly Control?[] _pages = new Control?[GalleryDestinations.All.Length];
    private MavNavigationDrawer? _drawer;
    private MavNavigationRail? _rail;
    private MavModalNavigationDrawer? _modal;
    private int _current = -1;
    private bool _syncing;

    public MainView()
    {
        InitializeComponent();

        Page.AddAction(MavIcons.DarkMode, () => MavTheme.ToggleDark(), "Toggle dark mode");
        Page.AddAction(MavIcons.Globe, ToggleLanguage, "Change language");
        Page.AddAction(MavIcons.Info, ShowAbout, "About this gallery");
        Page.NavigationAction = OpenDrawer;

        SizeChanged += (_, _) => UpdateAdaptive();

        // Navigation labels are plain strings, so the navigation is rebuilt on language changes.
        MavLocalization.Observe(this, view => view.RebuildNavigation());
        Select(DefaultDestination);
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

    private void Select(int index)
    {
        if (index < 0 || index >= GalleryDestinations.All.Length)
            return;

        _current = index;
        var destination = GalleryDestinations.All[index];

        Page.Title = destination.Label;
        Page.Subtitle = destination.Subtitle;

        Host.Content = _pages[index] ??= destination.Build();
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
    }

    private void OpenDrawer()
    {
        if (Bounds.Width >= DrawerBreakpoint || _modal is null)
            return;

        _modal.Show(Page);
    }

    private void ToggleLanguage()
    {
        var isRussian = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
        MavLocalization.SetCulture(new CultureInfo(isRussian ? "en-US" : "ru-RU"));
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
