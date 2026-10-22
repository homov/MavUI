using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mav.Controls;
using Mav.Platform;
using Xunit;

namespace Mav.Tests;

// Showing bottom sheets, side sheets and rich tooltips in a real host.
// Closing is animated, so only synchronous facts are asserted (the overlay exists, IsOpen, TryDismissTop);
// every test clears the overlay stack in a finally block so no state reaches other tests.
public class OverlayScenarioTests
{
    private static (Window Window, MavNavigationPage Navigation) CreateHost()
    {
        var navigation = new MavNavigationPage(new MavContentPage("Root"));
        var window = new Window
        {
            Width = 800,
            Height = 600,
            Content = navigation,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, navigation);
    }

    private static void Cleanup(Window window)
    {
        while (MavOverlay.TryDismissTop()) { }
        window.Close();
    }

    [AvaloniaFact]
    public void BottomSheet_Show_opens_an_overlay_with_its_title_and_body()
    {
        var (window, _) = CreateHost();
        try
        {
            var body = new TextBlock { Text = "Share with" };

            var sheet = MavBottomSheet.Show("Share", body);

            Assert.True(MavOverlay.HasActiveOverlay);
            Assert.Equal("Share", sheet.Title);
            Assert.Same(body, sheet.Body);
        }
        finally
        {
            Cleanup(window);
        }
    }

    [AvaloniaFact]
    public void SideSheet_Show_opens_the_sheet_and_registers_an_overlay()
    {
        var (window, _) = CreateHost();
        try
        {
            var sheet = new MavSideSheet { Title = "Filters" };

            sheet.Show();

            Assert.True(sheet.IsOpen);
            Assert.True(MavOverlay.HasActiveOverlay);
        }
        finally
        {
            Cleanup(window);
        }
    }

    [AvaloniaFact]
    public void SideSheet_shown_overlay_can_be_dismissed_by_the_overlay_stack()
    {
        var (window, _) = CreateHost();
        try
        {
            var sheet = new MavSideSheet { Title = "Filters" };
            sheet.Show();

            Assert.True(MavOverlay.TryDismissTop());
        }
        finally
        {
            Cleanup(window);
        }
    }

    [AvaloniaFact]
    public void RichTooltip_Show_registers_an_overlay_anchored_to_the_control()
    {
        var (window, navigation) = CreateHost();
        try
        {
            var tooltip = new MavRichTooltip { Text = "Saves the draft" };

            tooltip.Show(navigation);

            Assert.True(MavOverlay.HasActiveOverlay);
        }
        finally
        {
            Cleanup(window);
        }
    }
}
