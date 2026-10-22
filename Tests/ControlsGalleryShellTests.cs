using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Mav.ControlsGallery.Pages;
using Mav.ControlsGallery.Shell;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// Smoke checks for the gallery shell: it loads, lays out, and its snackbars reach a visible overlay.
/// This runs in a window, which has its own overlay layer, so it does not reproduce the browser case
/// (no window template). It guards against the shell breaking the overlay path in general.
/// </summary>
public class ControlsGalleryShellTests
{
    [AvaloniaFact]
    public void Shell_loads_and_lays_out()
    {
        var view = new MainView();
        var window = new Window { Width = 1200, Height = 900, Content = view };
        window.Show();
        view.Measure(new Size(1200, 900));
        view.Arrange(new Rect(0, 0, 1200, 900));

        Assert.NotNull(OverlayHostOf(view));
        window.Close();
    }

    [AvaloniaFact]
    public void Snackbar_appears_in_a_visible_overlay()
    {
        var view = new MainView();
        var window = new Window { Width = 1200, Height = 900, Content = view };
        window.Show();

        try
        {
            MavSnackbar.Show("Saved", anchor: view);

            var inHost = OverlayHostOf(view).Children.Count > 0;
            var inLayer = OverlayLayer.GetOverlayLayer(window) is { Children.Count: > 0 };
            Assert.True(inHost || inLayer, "Snackbar was not added to any overlay.");
        }
        finally
        {
            MavSnackbar.Dismiss();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Feedback_page_snackbar_appears_when_anchored_to_the_page()
    {
        var page = new FeedbackPage();
        var window = new Window { Width = 1200, Height = 900, Content = page };
        window.Show();

        try
        {
            MavSnackbar.Show("Saved", anchor: page);

            Assert.True(OverlayLayer.GetOverlayLayer(window) is { Children.Count: > 0 },
                "Snackbar anchored to the Feedback page was not added to the window overlay.");
        }
        finally
        {
            MavSnackbar.Dismiss();
            window.Close();
        }
    }

    private static Panel OverlayHostOf(MainView view) =>
        view.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "OverlayHost");
}
