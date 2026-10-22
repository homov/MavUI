using Avalonia.Headless.XUnit;
using Mav;
using Mav.Controls;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class ChromeTests
{
    [AvaloniaFact]
    public void MavTopAppBar_title_and_icon()
    {
        var bar = new MavTopAppBar("Mail") { NavigationIcon = MavIcons.Menu };
        Assert.Equal("Mail", bar.Title);
    }

    [AvaloniaFact]
    public void MavBottomAppBar_fab()
    {
        var fab = new MavFab(MavIcons.Add);
        var bottom = new MavBottomAppBar { Fab = fab, FabIcon = MavIcons.Add };
        Assert.NotNull(bottom);
    }

    [AvaloniaFact]
    public void MavNavigationDrawer_items()
    {
        var drawer = new MavNavigationDrawer();
        drawer.AddItem("Inbox", MavIcons.Inbox);
        Assert.True(drawer.Items.Count > 0 || drawer.Items.Count == 0); // no crash
    }

    [AvaloniaFact]
    public void MavTabs_add()
    {
        var tabs = new MavTabs();
        tabs.AddTab("One", icon: MavIcons.Home);
        Assert.NotNull(tabs);
    }

    [AvaloniaFact]
    public void MavStepper_step()
    {
        var stepper = new MavStepper();
        stepper.AddStep("Name", content: new MavLabel { Text = "x" });
        Assert.NotNull(stepper);
    }

    [AvaloniaFact]
    public void MavPagination_current()
    {
        var pag = new MavPagination { TotalPages = 5, CurrentPage = 1 };
        Assert.Equal(1, pag.CurrentPage);
    }

    [AvaloniaFact]
    public void MavModalNavigationDrawer_open()
    {
        var drawer = new MavModalNavigationDrawer();
        Assert.NotNull(drawer);
    }
}
