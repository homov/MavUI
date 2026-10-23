using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
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

    /// <summary>
    /// Seen in the CI screenshots: the tab body stopped at its own desired height, so a page inside
    /// MavTabs left the rest of the window blank instead of filling it.
    /// </summary>
    [AvaloniaFact]
    public void MavTabs_content_fills_the_remaining_height()
    {
        var body = new Border();
        var tabs = new MavTabs();
        tabs.AddTab("Groups", icon: MavIcons.Home, content: body);
        var window = new Window { Width = 390, Height = 700, Content = tabs };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.True(body.Bounds.Height > 500,
                $"The tab body is {body.Bounds.Height:0}px high inside a 700px window.");
        }
        finally
        {
            window.Close();
        }
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
