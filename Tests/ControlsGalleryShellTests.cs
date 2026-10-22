using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using System.Globalization;
using Mav;
using Mav.ControlsGallery.Localization;
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

            // The snackbar goes to the nearest overlay host (the navigation page's, above the app bar),
            // so check that its text is rendered somewhere in the shell.
            Assert.True(view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Saved"),
                "Snackbar anchored to the Feedback page was not rendered inside the shell.");
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
            var options = page.GetVisualDescendants().OfType<MavRadioButton>().ToList();

            Assert.Equal(8, options.Count);
            Assert.Equal(1, options.Count(rb => rb.IsChecked));

            options[2].IsChecked = true;

            Assert.Equal(1, options.Count(rb => rb.IsChecked));
            Assert.True(options[2].IsChecked);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Shell_hosts_its_page_in_a_navigation_page_with_app_bar()
    {
        var view = new MainView();
        var window = new Window { Width = 500, Height = 800, Content = view };
        window.Show();

        try
        {
            // The app bar (navigation icon, title, actions) comes from the navigation host.
            var host = view.GetVisualDescendants().OfType<MavNavigationPage>().Single();
            Assert.NotNull(host.CurrentPage);
            Assert.False(string.IsNullOrEmpty(host.CurrentPage!.Title));
            Assert.Equal(3, host.CurrentPage.Actions.Count);
            Assert.NotNull(host.CurrentPage.NavigationAction);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Library_controls_strings_are_translated_to_russian()
    {
        var previousProvider = MavLocalization.Current;
        var previousCulture = MavLocalization.Culture;
        try
        {
            MavLocalization.Current = GalleryStrings.Instance;
            MavLocalization.SetCulture(new CultureInfo("ru-RU"));

            Assert.Equal("Отмена", MavLocalization.Cancel);
            Assert.Equal("ОК", MavLocalization.Ok);
            Assert.Equal("Выберите дату", MavLocalization.SelectDate);
            Assert.Equal("Недавние запросы", MavLocalization.Get("SearchView.RecentSearches", "Recent searches"));
            Assert.Equal("Смена языка", MavLocalization.Get("Theming.Language.Title", "Language switch"));
            // The hint is parsed as ISO, so it must not be translated.
            Assert.Equal("yyyy-mm-dd", MavLocalization.DateFieldHint);
        }
        finally
        {
            MavLocalization.Current = previousProvider;
            MavLocalization.SetCulture(previousCulture);
        }
    }

    private static Panel OverlayHostOf(MainView view) =>
        view.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "OverlayHost");
}
