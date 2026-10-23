using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mav;
using Mav.ControlsGallery.Localization;
using Mav.ControlsGallery.Pages;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// Builds every ControlsGallery page in English and Russian, shows it in a window and runs layout.
/// Any exception from AXAML loading, a missing binding target or a broken localization key fails the test.
/// </summary>
public class ControlsGalleryPagesTests
{
    private static IEnumerable<(string Name, Func<UserControl> Create)> Pages()
    {
        yield return ("Overview", () => new OverviewPage());
        yield return ("Actions", () => new ActionsPage());
        yield return ("Inputs", () => new InputsPage());
        yield return ("Selection", () => new SelectionPage());
        yield return ("Pickers", () => new PickersPage());
        yield return ("Navigation", () => new NavigationGroupPage());
        yield return ("Layout", () => new LayoutPage());
        yield return ("Collections", () => new CollectionsPage());
        yield return ("Feedback", () => new FeedbackPage());
        yield return ("Overlays", () => new OverlaysPage());
        yield return ("Theming", () => new ThemingPage());
    }

    [AvaloniaFact]
    public void Every_page_builds_and_lays_out_in_english_and_russian()
    {
        var previous = MavLocalization.Current;
        MavLocalization.Current = GalleryStrings.Instance;

        var failures = new List<string>();
        try
        {
            foreach (var culture in new[] { "en-US", "ru-RU" })
            {
                MavLocalization.SetCulture(new CultureInfo(culture));
                foreach (var (name, create) in Pages())
                {
                    try
                    {
                        var page = create();
                        var window = new Window { Width = 1200, Height = 900, Content = page };
                        window.Show();
                        page.Measure(new Size(1200, 900));
                        page.Arrange(new Rect(0, 0, 1200, 900));
                        window.Close();
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{culture} {name}: {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }
        finally
        {
            MavLocalization.SetCulture(new CultureInfo("en-US"));
            MavLocalization.Current = previous;
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [AvaloniaFact]
    public void No_page_builds_a_window_before_it_is_asked_to()
    {
        // A browser build has no windowing platform: constructing a Window throws there
        // ("Browser doesn't support windowing platform"), even when it only sits in XAML
        // resources and is never shown. The Layout page used to declare its demo window in
        // UserControl.Resources, so merely opening that page threw on the web.
        var previous = MavLocalization.Current;
        MavLocalization.Current = GalleryStrings.Instance;
        try
        {
            foreach (var (name, create) in Pages())
            {
                var page = create();
                foreach (var key in page.Resources.Keys)
                {
                    Assert.False(
                        page.Resources[key] is Window,
                        $"{name} builds a Window while loading; create it on demand instead");
                }
            }
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }
}
