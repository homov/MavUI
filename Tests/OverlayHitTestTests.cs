using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Md3.Avalonia.Controls;
using Xunit;

namespace Md3.Avalonia.Tests;

public class OverlayHitTestTests
{
    [AvaloniaFact]
    public void Empty_overlay_host_is_not_hit_test_visible()
    {
        var button = new MdButton("Target");
        var page = new MdContentPage("Root")
        {
            Content = new StackPanel { Margin = new Thickness(40), Children = { button } },
        };
        var navigation = new MdNavigationPage(page);
        var window = new Window { Width = 480, Height = 640, Content = navigation };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Force the fallback overlay host to exist.
        var dialog = new MdDialog { Title = "Overlay" };
        dialog.Show(button);
        Dispatcher.UIThread.RunJobs();

        var host = (InputElement)window.GetVisualDescendants()
            .First(v => v.GetType().Name == "OverlayRootGrid");
        Assert.True(host.IsHitTestVisible, "the host must accept input while an overlay is open");

        dialog.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.False(host.IsHitTestVisible, "an empty overlay host must not intercept input");

        window.Close();
    }
}
