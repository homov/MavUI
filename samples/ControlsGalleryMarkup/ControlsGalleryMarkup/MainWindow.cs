using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Mav;
using Mav.Controls;
using Mav.ControlsGalleryMarkup.Pages;
using Mav.Theming;

namespace Mav.ControlsGalleryMarkup;

/// <summary>
/// Adaptive gallery shell, the same layout as the AXAML gallery: a permanent drawer when wide, a rail
/// when medium, and a modal drawer from the app bar when narrow. Texts come from MavLocalization, so
/// the window is rebuilt whenever the language changes.
/// </summary>
public sealed class MainWindow : MavWindow
{
    private const double RailBreakpoint = 700;
    private const double DrawerBreakpoint = 1000;

    private static readonly (string Key, string English, string Subtitle, string Icon, Func<Control> Build)[] Destinations =
    [
        ("Overview", "Overview", "Theme, typography, color and motion", MavIcons.Home, () => OverviewPage.Build()),
        ("Actions", "Actions", "Buttons, FABs and action groups", MavIcons.SmartButton, () => ActionsPage.Build()),
        ("Inputs", "Inputs", "Text fields, validation and forms", MavIcons.Edit, () => InputsPage.Build()),
        ("Selection", "Selection", "Toggles, sliders, chips and value pickers", MavIcons.CheckBox, () => SelectionPage.Build()),
        ("Pickers", "Pickers", "Date, time and color selection", MavIcons.CalendarMonth, () => PickersPage.Build()),
    ];

    private int _selected;
    private bool _syncing;
    private MavNavigationPage? _nav;
    private MavContentPage? _page;
    private MavNavigationDrawer? _drawer;
    private MavNavigationRail? _rail;
    private MavModalNavigationDrawer? _modal;
    private ContentControl? _drawerHost;
    private MavScrollViewer? _railScroll;
    private ContentControl? _host;

    public MainWindow()
    {
        Width = 1100;
        Height = 720;
        SizeChanged += (_, _) => UpdateAdaptive();
        Rebuild();
        MavLocalization.Changed += (_, _) => Rebuild();
    }

    private void Rebuild()
    {
        Title = MavLocalization.Get("Shell.WindowTitle", "ControlsGallery (markup)");
        var appTitle = MavLocalization.Get("Shell.AppTitle", "Mav Controls");

        _drawer = new MavNavigationDrawer(appTitle, 280);
        _rail = new MavNavigationRail();
        _modal = new MavModalNavigationDrawer(appTitle, 300);
        for (var i = 0; i < Destinations.Length; i++)
        {
            var index = i;
            var destination = Destinations[i];
            var label = Label(destination);
            _drawer.AddItem(label, destination.Icon, onClick: () => Select(index));
            _rail.AddDestination(label, destination.Icon, destination.Icon);
            _modal.AddItem(label, destination.Icon, onClick: () => Select(index));
        }

        _drawer.SelectedIndexChanged += (_, index) => { if (!_syncing) Select(index); };
        _rail.SelectedIndexChanged += (_, index) => { if (!_syncing) Select(index); };
        _modal.Drawer.SelectedIndexChanged += (_, index) => { if (!_syncing) Select(index); };

        _page = new MavContentPage();
        _page.AddAction(MavIcons.DarkMode, () => MavTheme.ToggleDark(), "Toggle dark mode");
        _page.AddAction(MavIcons.Globe, ToggleLanguage, "Change language");
        _page.AddAction(MavIcons.Info, ShowAbout, "About this gallery");
        _page.NavigationAction = OpenDrawer;

        _drawerHost = new ContentControl().GridColumn(0).Content(_drawer);
        _railScroll = new MavScrollViewer()
            .GridColumn(0)
            .Width(88)
            .HorizontalScrollBarVisibility(ScrollBarVisibility.Disabled)
            .Content(_rail);
        _host = new ContentControl()
            .GridColumn(1)
            .HorizontalContentAlignment(HorizontalAlignment.Stretch)
            .VerticalContentAlignment(VerticalAlignment.Stretch);

        var body = new Grid().Cols("Auto, *").Children(_drawerHost, _railScroll, _host);
        _page.Child = body;

        _nav = new MavNavigationPage();
        _nav.SetRoot(_page);
        Content = _nav;

        Select(_selected);
        UpdateAdaptive();
    }

    private void Select(int index)
    {
        if (index < 0 || index >= Destinations.Length || _page is null || _host is null || _nav is null)
            return;

        _selected = index;
        var destination = Destinations[index];
        _page.Title = Label(destination);
        _page.Subtitle = MavLocalization.Get($"Nav.{destination.Key}.Subtitle", destination.Subtitle);

        _host.Content = destination.Build();
        _nav.RefreshChrome();
        SyncSelection();

        if (_modal?.IsOpen == true)
            _modal.Close();
    }

    private void SyncSelection()
    {
        _syncing = true;
        if (_drawer is not null && _drawer.SelectedIndex != _selected)
            _drawer.SelectedIndex = _selected;
        if (_rail is not null && _rail.SelectedIndex != _selected)
            _rail.SelectedIndex = _selected;
        if (_modal is not null && _modal.Drawer.SelectedIndex != _selected)
            _modal.Drawer.SelectedIndex = _selected;
        _syncing = false;
    }

    private void UpdateAdaptive()
    {
        if (_page is null || _host is null || _drawerHost is null || _railScroll is null || _nav is null)
            return;

        var width = Bounds.Width;
        if (width <= 0)
            width = 400;

        var wide = width >= DrawerBreakpoint;
        var medium = width >= RailBreakpoint && !wide;

        _drawerHost.IsVisible = wide;
        _railScroll.IsVisible = medium;
        Grid.SetColumn(_host, wide || medium ? 1 : 0);
        Grid.SetColumnSpan(_host, wide || medium ? 1 : 2);

        _page.NavigationIcon = wide || medium ? null : MavIcons.Menu;
        _nav.RefreshChrome();
    }

    private void OpenDrawer()
    {
        if (Bounds.Width >= DrawerBreakpoint || _modal is null || _page is null)
            return;

        _modal.Show(_page);
    }

    private static string Label((string Key, string English, string Subtitle, string Icon, Func<Control> Build) destination) =>
        MavLocalization.Get($"Nav.{destination.Key}.Label", destination.English);

    private static void ToggleLanguage()
    {
        var isRussian = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";
        MavLocalization.SetCulture(new System.Globalization.CultureInfo(isRussian ? "en-US" : "ru-RU"));
    }

    private void ShowAbout()
    {
        if (_page is null)
            return;

        var text = MavLocalization.Format("Shell.AboutText", "{0} groups · 90+ controls", Destinations.Length);
        MavSnackbar.Show(text, anchor: _page);
    }
}
