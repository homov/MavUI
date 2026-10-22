using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mav;
using Mav.Controls;
using Mav.Platform;
using Xunit;

namespace Mav.Tests;

public class NavigationSuiteScaffoldTests
{
    [AvaloniaFact]
    public void Compact_width_puts_a_menu_icon_on_the_top_bar()
    {
        var bar = new MavTopAppBar("Title");
        var scaffold = new MavNavigationSuiteScaffold { TopAppBar = bar };
        scaffold.AddDestination("Home", MavIcons.Home);
        var window = new Window { Width = 400, Height = 700, Content = scaffold };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(MavNavigationSuiteMode.NavigationBar, scaffold.EffectiveMode);
        Assert.Equal(MavIcons.Menu, bar.NavigationIcon);

        window.Close();
    }

    [AvaloniaFact]
    public void Permanent_drawer_width_removes_the_menu_icon()
    {
        var bar = new MavTopAppBar("Title");
        var scaffold = new MavNavigationSuiteScaffold { TopAppBar = bar };
        scaffold.AddDestination("Home", MavIcons.Home);
        var window = new Window { Width = 1000, Height = 700, Content = scaffold };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(MavNavigationSuiteMode.NavigationDrawer, scaffold.EffectiveMode);
        Assert.Null(bar.NavigationIcon);

        window.Close();
    }

    [AvaloniaFact]
    public void Top_bar_with_its_own_navigation_icon_keeps_it()
    {
        var bar = new MavTopAppBar("Detail") { NavigationIcon = MavIcons.ArrowBack };
        var scaffold = new MavNavigationSuiteScaffold { TopAppBar = bar };
        scaffold.AddDestination("Home", MavIcons.Home);
        var window = new Window { Width = 400, Height = 700, Content = scaffold };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(MavNavigationSuiteMode.NavigationBar, scaffold.EffectiveMode);
        Assert.Equal(MavIcons.ArrowBack, bar.NavigationIcon);

        window.Close();
    }

    [AvaloniaFact]
    public void OpenNavigationMenu_can_be_called_without_a_top_bar()
    {
        var scaffold = new MavNavigationSuiteScaffold();
        scaffold.AddDestination("Home", MavIcons.Home);
        var window = new Window { Width = 400, Height = 700, Content = scaffold };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        scaffold.OpenNavigationMenu();
        Dispatcher.UIThread.RunJobs();
        Assert.True(MavOverlay.HasActiveOverlay);

        // The drawer is an overlay in static lists; close it so later tests start clean.
        while (MavOverlay.TryDismissTop()) { }
        Dispatcher.UIThread.RunJobs();
        Assert.False(MavOverlay.HasActiveOverlay);

        window.Close();
    }
}
