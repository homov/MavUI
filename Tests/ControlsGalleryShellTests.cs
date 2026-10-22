using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Controls.Primitives;
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

        Assert.NotNull(view.OverlayHost);
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
            MavSnackbar.Show("Saved");

            var inHost = view.OverlayHost.Children.Count > 0;
            var inLayer = OverlayLayer.GetOverlayLayer(window) is { Children.Count: > 0 };
            Assert.True(inHost || inLayer, "Snackbar was not added to any overlay.");
        }
        finally
        {
            MavSnackbar.Dismiss();
            window.Close();
        }
    }
}
