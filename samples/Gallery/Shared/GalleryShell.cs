using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

/// <summary>
/// Adaptive gallery shell: permanent drawer on wide screens, a navigation rail on
/// medium screens and a modal drawer opened from the app bar on phones.
/// </summary>
internal sealed class GalleryShellView : Grid
{
    private const double RailBreakpoint = 700;
    private const double DrawerBreakpoint = 1000;

    private readonly MdContentPage _page;
    private readonly MdNavigationPage _navigation;
    private readonly MdNavigationDrawer _drawer;
    private readonly MdNavigationRail _rail;
    private readonly MdScrollViewer _railScroll;
    private readonly MdModalNavigationDrawer _modal;
    private readonly ContentControl _host = new();
    private readonly Control?[] _pages = new Control?[GalleryDestinations.All.Length];
    private int _current = -1;
    private bool _syncing;

    public GalleryShellView(MdContentPage page, MdNavigationPage navigation)
    {
        _page = page;
        _navigation = navigation;

        ColumnDefinitions = new ColumnDefinitions("Auto,*");

        _drawer = new MdNavigationDrawer("Md3 Gallery", 280);
        _rail = new MdNavigationRail();
        _modal = new MdModalNavigationDrawer("Md3 Gallery", 300);

        for (var i = 0; i < GalleryDestinations.All.Length; i++)
        {
            var index = i;
            var destination = GalleryDestinations.All[i];

            _drawer.AddItem(destination.Label, destination.Icon, onClick: () => Select(index));
            _rail.AddDestination(destination.Label, destination.Icon, destination.Icon);
            _modal.AddItem(destination.Label, destination.Icon, onClick: () => Select(index));
        }

        _drawer.SelectedIndexChanged += (_, index) =>
        {
            if (!_syncing) Select(index);
        };
        _rail.SelectedIndexChanged += (_, index) =>
        {
            if (!_syncing) Select(index);
        };
        _modal.Drawer.SelectedIndexChanged += (_, index) =>
        {
            if (!_syncing) Select(index);
        };

        _railScroll = new MdScrollViewer
        {
            Width = 88,
            Content = _rail,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        _host.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _host.VerticalContentAlignment = VerticalAlignment.Stretch;

        Children.Add(_drawer);
        SetColumn(_drawer, 0);

        Children.Add(_railScroll);
        SetColumn(_railScroll, 0);

        Children.Add(_host);
        SetColumn(_host, 1);

        SizeChanged += (_, _) => UpdateAdaptive();
        UpdateAdaptive();
        Select(8);
    }

    public void OpenDrawer()
    {
        if (Bounds.Width >= DrawerBreakpoint)
            return;

        _modal.Show(_page);
    }

    private void Select(int index)
    {
        if (index < 0 || index >= GalleryDestinations.All.Length)
            return;

        _current = index;
        var destination = GalleryDestinations.All[index];

        _page.Title = destination.Label;
        _page.Subtitle = destination.Subtitle;
        _navigation.RefreshChrome();

        _host.Content = _pages[index] ??= destination.Build();

        _syncing = true;
        if (_drawer.SelectedIndex != index)
            _drawer.SelectedIndex = index;
        if (_rail.SelectedIndex != index)
            _rail.SelectedIndex = index;
        if (_modal.Drawer.SelectedIndex != index)
            _modal.Drawer.SelectedIndex = index;
        _syncing = false;

        if (_modal.IsOpen)
            _modal.Close();
    }

    private void UpdateAdaptive()
    {
        var width = Bounds.Width;
        if (width <= 0)
            width = 400;

        var wide = width >= DrawerBreakpoint;
        var medium = width >= RailBreakpoint && !wide;

        _drawer.IsVisible = wide;
        _railScroll.IsVisible = medium;
        SetColumn(_host, wide || medium ? 1 : 0);
        SetColumnSpan(_host, wide || medium ? 1 : 2);

        _page.NavigationIcon = wide || medium ? null : MdIcons.Menu;
        _navigation.RefreshChrome();
    }
}

public static class GalleryShell
{
    public static Control Create()
    {
        var page = new MdContentPage("Control Gallery")
        {
            Subtitle = "Every Md3 control, grouped by purpose",
        };

        var navigation = new MdNavigationPage(page);
        TopLevel.SetAutoSafeAreaPadding(navigation, false);

        var shell = new GalleryShellView(page, navigation);
        page.Content = shell;
        page.NavigationAction = shell.OpenDrawer;

        page.AddAction(MdIcons.DarkMode, () => MdTheme.ToggleDark(), "Toggle dark mode");
        page.AddAction(MdIcons.Info, () => MdSnackbar.Show(
            $"{GalleryDestinations.All.Length} groups · 90+ controls",
            anchor: page), "About this gallery");

        return navigation;
    }
}
