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
    public void Feedback_page_snackbar_appears_in_the_shell_overlay()
    {
        // The page runs inside the shell, as in the app: the shell registers the overlay host.
        var view = new MainView();
        var window = new Window { Width = 1200, Height = 900, Content = view };
        window.Show();

        try
        {
            var page = new FeedbackPage();
            view.GetVisualDescendants().OfType<ContentControl>().Single(c => c.Name == "Host").Content = page;

            MavSnackbar.Show("Saved", anchor: page);

            Assert.True(OverlayHostOf(view).Children.Count > 0,
                "Snackbar anchored to the Feedback page did not reach the shell overlay host.");
        }
        finally
        {
            MavSnackbar.Dismiss();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Theming_seed_colors_are_a_single_choice_group()
    {
        var page = new ThemingPage();
        var window = new Window { Width = 1200, Height = 900, Content = page };
        window.Show();

        try
        {
            var group = page.GetVisualDescendants().OfType<MavRadioGroup>().Single();
            var options = page.GetVisualDescendants().OfType<MavRadioButton>()
                .Where(rb => group.GetVisualDescendants().Contains(rb))
                .ToList();

            Assert.Equal(8, options.Count);
            Assert.Equal(1, options.Count(rb => rb.IsChecked));

            group.SelectedIndex = 2;

            Assert.Equal(2, group.SelectedIndex);
            Assert.Equal(1, options.Count(rb => rb.IsChecked));
            Assert.True(options[2].IsChecked);
        }
        finally
        {
            window.Close();
        }
    }

    private static Panel OverlayHostOf(MainView view) =>
        view.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "OverlayHost");
}
