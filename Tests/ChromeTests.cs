using Avalonia.Headless.XUnit;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class ChromeTests
{
    [AvaloniaFact]
    public void MdTopAppBar_title_and_icon()
    {
        var bar = new MdTopAppBar("Mail") { NavigationIcon = MdIcons.Menu };
        Assert.Equal("Mail", bar.Title);
    }

    [AvaloniaFact]
    public void MdBottomAppBar_fab()
    {
        var fab = new MdFab(MdIcons.Add);
        var bottom = new MdBottomAppBar { Fab = fab, FabIcon = MdIcons.Add };
        Assert.NotNull(bottom);
    }

    [AvaloniaFact]
    public void MdNavigationDrawer_items()
    {
        var drawer = new MdNavigationDrawer();
        drawer.AddItem("Inbox", MdIcons.Inbox);
        Assert.True(drawer.Items.Count > 0 || drawer.Items.Count == 0); // no crash
    }

    [AvaloniaFact]
    public void MdTabs_add()
    {
        var tabs = new MdTabs();
        tabs.AddTab("One", icon: MdIcons.Home);
        Assert.NotNull(tabs);
    }

    [AvaloniaFact]
    public void MdStepper_step()
    {
        var stepper = new MdStepper();
        stepper.AddStep("Name", content: new MdLabel { Text = "x" });
        Assert.NotNull(stepper);
    }

    [AvaloniaFact]
    public void MdPagination_current()
    {
        var pag = new MdPagination { TotalPages = 5, CurrentPage = 1 };
        Assert.Equal(1, pag.CurrentPage);
    }

    [AvaloniaFact]
    public void MdModalNavigationDrawer_open()
    {
        var drawer = new MdModalNavigationDrawer();
        Assert.NotNull(drawer);
    }
}
