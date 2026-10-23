using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;
using global::Mav.Controls;
using global::Mav.ControlsGallery.Localization;
using global::Mav.ControlsGallery.Shell;
using global::Mav.Theming;

namespace Mav.VisualTests;

/// <summary>
/// Screenshots and layout checks for the ControlsGallery: every destination through the real shell,
/// the three adaptive widths and the dark theme. The images land in MAV_SHOT_DIR (ui-report in CI),
/// where the annotation step publishes them as base64 chunks and tools/read_screenshots.py rebuilds
/// them; gallery.txt carries the numbers next to them.
/// All cases share the one static theme, so they run in a single collection; each case is caught
/// separately, so one broken page still lets the other screenshots through.
/// </summary>
[Collection("Gallery visuals")]
public class GalleryVisualTests
{
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
            Shots.Note("ControlsGallery visual tests. Size in px, PNG size in KB, controls = visible controls.");
        }
    }

    /// <summary>Every destination, opened through the shell, draws and stays inside the window.</summary>
    [AvaloniaFact]
    public void Every_destination_renders_without_overflow()
    {
        var failures = new List<string>();
        foreach (var destination in GalleryDestinations.All)
        {
            try
            {
                using var shell = OpenShell(1280, 900, destination.Key);
                var size = Shots.Capture(shell.Window, $"page-{destination.Key}");
                var controls = VisibleControls(shell.View);
                Shots.Note($"page-{destination.Key}.png  {shell.Window.Width:0}x{shell.Window.Height:0}  " +
                           $"{size / 1024.0:0.0} KB  controls={controls}");

                var overflow = Shots.FindHorizontalOverflow(shell.View, shell.Window.Width);
                if (overflow.Count > 0)
                    failures.Add($"{destination.Key}: {overflow.Count} control(s) outside the window: " +
                                 string.Join("; ", overflow.Take(6)));
            }
            catch (Exception error)
            {
                failures.Add($"{destination.Key}: {error.GetType().Name}: {error.Message}");
            }
        }

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
            using var shell = OpenShell(1280, 900, "Overview");
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
