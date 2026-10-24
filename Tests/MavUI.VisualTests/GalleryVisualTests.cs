using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;
using global::Mav.Controls;
using global::Mav.ControlsGallery.Shared;
using global::Mav.ControlsGallery.Shell;
using global::Mav.Primitives;
using global::Mav.Theming;

namespace Mav.VisualTests;

/// <summary>
/// Screenshots and layout checks for the ControlsGallery. Every gallery page is taller than the window,
/// so a single frame only proves the first screen: each destination gets
///   page-NAME.png        what the user sees on opening it (1280x900),
///   page-NAME-full.png   the whole page, rendered in a window grown to the full content height,
/// and each one is checked for scrolling and horizontal overflow.
/// The images land in MAV_SHOT_DIR (ui-report in CI), where the publish step pushes them to the
/// ci/reports branch and tools/read_screenshots.py fetches them back; gallery.txt carries the numbers.
/// All cases share the one static theme, so they run in a single collection; each destination is
/// caught separately, so one broken page still lets the other screenshots through.
/// </summary>
[Collection("Gallery visuals")]
public class GalleryVisualTests
{
    private const double ViewportWidth = 1280;
    private const double ViewportHeight = 900;
    /// <summary>A Skia surface is happy with this; a page above it is reported instead of cut silently.</summary>
    private const double MaxFullHeight = 12000;

    private static readonly object SetupLock = new();
    private static bool _prepared;

    public GalleryVisualTests() => Prepare();

    private static void Prepare()
    {
        lock (SetupLock)
        {
            if (_prepared)
                return;
            _prepared = true;
            global::Mav.MavLocalization.Current = GalleryStrings.Instance;
            global::Mav.MavLocalization.SetCulture(new CultureInfo("en-US"));
            MavTheme.Mode = MavThemeMode.Light;
            Shots.ResetReport();
            Shots.Note("ControlsGallery visual tests. Size in px, PNG size in KB.");
            Shots.Note("page-NAME.png = first screen; page-NAME-full.png = the whole page at content height.");
        }
    }

    /// <summary>Every destination draws, scrolls to its end and is captured in full.</summary>
    [AvaloniaFact]
    public void Every_destination_shows_its_whole_content()
    {
        var failures = new List<string>();
        foreach (var destination in GalleryDestinations.All)
        {
            var key = destination.Key;
            try
            {
                using var shell = OpenShell(ViewportWidth, ViewportHeight, key);
                var window = shell.Window;
                var page = PageControl(shell.View);
                var scroller = page.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()
                               ?? throw new InvalidOperationException("the page has no scroll viewer.");

                var viewport = scroller.Viewport;
                var extent = scroller.Extent;
                var needsScrolling = extent.Height > viewport.Height + 1;

                // 1. The first screen, as the user sees it.
                var topSize = Shots.Capture(window, $"page-{key}");

                // 2. The end of the content must be reachable, otherwise the tail is lost.
                var maxOffset = Math.Max(0, extent.Height - viewport.Height);
                scroller.Offset = new Vector(0, maxOffset);
                Shots.Settle(3);
                var reached = Math.Abs(scroller.Offset.Y - maxOffset) < 1.5;
                if (needsScrolling && !reached)
                    failures.Add($"{key}: scrolling stopped at {scroller.Offset.Y:0} of {maxOffset:0} px.");

                // 3. The whole page in one frame: grow the window by the content height.
                scroller.Offset = new Vector(0, 0);
                var chrome = window.Height - scroller.Bounds.Height;
                var wanted = chrome + extent.Height;
                window.Height = Math.Min(MaxFullHeight, wanted);
                Shots.Settle();
                var grew = window.Height;
                var fits = scroller.Extent.Height <= scroller.Viewport.Height + 1.5;
                var fullSize = Shots.Capture(window, $"page-{key}-full");
                if (!fits)
                    failures.Add($"{key}: the page needs {wanted:0} px, the capture stopped at {grew:0} px, "
                                 + $"so {scroller.Extent.Height - scroller.Viewport.Height:0} px are not in the image.");

                // 4. Every item host on the page must have built its items. Avalonia's ItemsControl
                //    draws nothing without a theme, and that is how swatches, type scale and palette
                //    buttons silently disappeared from these very screenshots.
                var hosts = page.GetVisualDescendants().OfType<MavItems>().ToList();
                var empty = hosts.Where(host => host.Children.Count == 0).Select(host =>
                    string.IsNullOrEmpty(host.Name) ? nameof(MavItems) : $"{nameof(MavItems)} #{host.Name}").ToList();
                if (empty.Count > 0)
                    failures.Add($"{key}: {empty.Count} item host(s) built no children: {string.Join(", ", empty)}");
                var built = hosts.Sum(host => host.Children.Count);

                // 5. A gallery page never scrolls sideways: wrapped text must fit the width.
                if (extent.Width > viewport.Width + 1)
                    failures.Add($"{key}: content is {extent.Width:0} px wide in a {viewport.Width:0} px viewport "
                                 + "(horizontal scrolling).");

                // 6. And nothing may stick out of the window itself.
                window.Height = ViewportHeight;
                Shots.Settle(3);
                var overflow = Shots.FindHorizontalOverflow(shell.View, ViewportWidth);
                if (overflow.Count > 0)
                    failures.Add($"{key}: {overflow.Count} control(s) outside the window: "
                                 + string.Join("; ", overflow.Take(6)));

                Shots.Note($"page-{key}.png  {topSize / 1024.0:0.0} KB  " +
                           $"extent {extent.Height:0}x{extent.Width:0}  viewport {viewport.Height:0}x{viewport.Width:0}  " +
                           $"scrolls={needsScrolling}  " +
                           $"full {grew:0}x{ViewportWidth:0}px {fullSize / 1024.0:0.0} KB  fits={fits}  " +
                           $"items {hosts.Count} host(s)/{built} child(ren)");
            }
            catch (Exception error)
            {
                failures.Add($"{key}: {error.GetType().Name}: {error.Message}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// The FAB menu opens above its FAB: the pills float in the overlay, so the button keeps its
    /// place. A frame of the open menu is captured for the record.
    /// </summary>
    [AvaloniaFact]
    public void Fab_menu_opens_above_the_fab_without_moving_it()
    {
        using var shell = OpenShell(ViewportWidth, ViewportHeight, "Actions");
        var page = PageControl(shell.View);
        var scroller = page.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()
                       ?? throw new InvalidOperationException("the page has no scroll viewer.");
        var menu = page.GetVisualDescendants().OfType<MavFabMenu>().FirstOrDefault()
                   ?? throw new InvalidOperationException("the page has no FAB menu.");

        // Bring the FAB into view: it sits near the bottom of the Actions page.
        scroller.Offset = new Vector(0, Math.Max(0, scroller.Extent.Height - scroller.Viewport.Height));
        Shots.Settle(3);

        var before = menu.Bounds;
        menu.Open();
        Shots.Settle(3);

        var size = Shots.Capture(shell.Window, "fab-menu-open");

        // The pills live in the overlay, so they are found from the window, not from the page.
        var pills = shell.Window.GetVisualDescendants().OfType<MavInkSurface>()
            .Where(surface => surface.MinHeight > 0 && surface.CornerRadius > 20)
            .ToList();
        var failures = new List<string>();
        if (menu.Bounds != before)
            failures.Add($"the FAB moved: {before} -> {menu.Bounds}.");
        if (pills.Count == 0)
            failures.Add("no action pill is drawn.");

        var origin = menu.TranslatePoint(default, shell.Window) ?? default;
        Shots.Note($"fab-menu-open.png  {ViewportWidth:0}x{ViewportHeight:0}  {size / 1024.0:0.0} KB  " +
                   $"open={menu.IsOpen}  fab={menu.Bounds.Width:0}x{menu.Bounds.Height:0} at " +
                   $"{origin.X:0},{origin.Y:0} in the window  pillShaped={pills.Count}");

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>The shell picks the drawer, the rail or the modal-drawer layout from the window width.</summary>
    [AvaloniaFact]
    public void Shell_adapts_to_width()
    {
        var cases = new (string Name, double Width, double Height)[]
        {
            ("wide", 1600, 1000),
            ("medium", 980, 900),
            ("narrow", 420, 820),
        };

        var failures = new List<string>();
        foreach (var (name, width, height) in cases)
        {
            try
            {
                using var shell = OpenShell(width, height, "Overview");
                var size = Shots.Capture(shell.Window, $"shell-{name}");
                Shots.Note($"shell-{name}.png  {width:0}x{height:0}  {size / 1024.0:0.0} KB  " +
                           $"controls={VisibleControls(shell.View)}");

                var overflow = Shots.FindHorizontalOverflow(shell.View, width);
                if (overflow.Count > 0)
                    failures.Add($"{name}: {overflow.Count} control(s) outside the window: " +
                                 string.Join("; ", overflow.Take(6)));
            }
            catch (Exception error)
            {
                failures.Add($"{name}: {error.GetType().Name}: {error.Message}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>A dark-theme frame is drawn with the dark scheme, not with the light one.</summary>
    [AvaloniaFact]
    public void Dark_theme_renders()
    {
        try
        {
            MavTheme.Mode = MavThemeMode.Dark;
            using var shell = OpenShell(ViewportWidth, ViewportHeight, "Overview");
            var size = Shots.Capture(shell.Window, "page-Overview-dark");
            Shots.Note($"page-Overview-dark.png  {shell.Window.Width:0}x{shell.Window.Height:0}  " +
                       $"{size / 1024.0:0.0} KB  dark={MavTheme.Current.IsDark}");
            Assert.True(MavTheme.Current.IsDark, "The dark capture ran with the light scheme.");
        }
        finally
        {
            MavTheme.Mode = MavThemeMode.Light;
        }
    }

    private static int VisibleControls(Visual view) =>
        view.GetVisualDescendants().OfType<Control>().Count(control => control.IsVisible);

    /// <summary>The page the shell put into its Host content control.</summary>
    private static Control PageControl(MainView view) =>
        view.GetVisualDescendants().OfType<ContentControl>().FirstOrDefault(c => c.Name == "Host")?.Content as Control
        ?? throw new InvalidOperationException("The shell host holds no page.");

    /// <summary>Builds the real shell in a window, opens the destination and waits for layout to settle.</summary>
    private static ShellWindow OpenShell(double width, double height, string key)
    {
        var view = new MainView();
        var window = new MavWindow
        {
            Width = width,
            Height = height,
            Content = view,
        };
        window.Show();
        Assert.True(view.Open(key), $"The gallery has no destination named {key}.");
        Shots.Settle();
        return new ShellWindow(window, view);
    }

    private sealed class ShellWindow(Window window, MainView view) : IDisposable
    {
        public Window Window { get; } = window;
        public MainView View { get; } = view;
        public void Dispose() => Window.Close();
    }
}
