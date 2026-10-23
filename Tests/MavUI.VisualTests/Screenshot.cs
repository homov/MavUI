using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Mav.VisualTests;

/// <summary>Where the shots go: MAV_SHOT_DIR, else HC_UI_REPORT_DIR, else the temp folder.</summary>
internal static class Shots
{
    public static string Folder =>
        Environment.GetEnvironmentVariable("MAV_SHOT_DIR") is { Length: > 0 } dir
            ? dir
            : Environment.GetEnvironmentVariable("HC_UI_REPORT_DIR") is { Length: > 0 } hc
                ? hc
                : Path.Combine(Path.GetTempPath(), "mav-gallery-shots");

    private static readonly object ReportLock = new();

    /// <summary>Appends one line to gallery.txt (read by the CI annotation step as one notice).</summary>
    public static void Note(string line)
    {
        Directory.CreateDirectory(Folder);
        lock (ReportLock)
            File.AppendAllText(Path.Combine(Folder, "gallery.txt"), line + Environment.NewLine, Encoding.UTF8);
    }

    /// <summary>Clears the report file; the first test of a run calls this.</summary>
    public static void ResetReport()
    {
        Directory.CreateDirectory(Folder);
        lock (ReportLock)
            File.WriteAllText(Path.Combine(Folder, "gallery.txt"), string.Empty, Encoding.UTF8);
    }

    /// <summary>Lets layout, data binding and pending dispatcher work finish, then draws frames.</summary>
    public static void Settle(int rounds = 8)
    {
        for (var i = 0; i < rounds; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(8);
        }
    }

    /// <summary>Renders the window to <c>&lt;Folder&gt;/&lt;name&gt;.png</c> and returns the file size.</summary>
    public static long Capture(Window window, string name)
    {
        for (var i = 0; i < 3; i++)
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        using var frame = window.CaptureRenderedFrame();
        Assert.True(frame is not null, $"CaptureRenderedFrame returned no frame for {name}.");

        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, name + ".png");
        frame!.Save(path);

        var size = new FileInfo(path).Length;
        // A frame that was never drawn saves as a nearly uniform image a few hundred bytes big.
        Assert.True(size > 4096, $"{name}.png is only {size} bytes: the frame looks blank.");
        return size;
    }

    /// <summary>
    /// Visible controls whose right edge sits outside the window. Scroll content is skipped: it is
    /// allowed to be wider than the viewport, that is what the scroll viewer is for.
    /// </summary>
    public static IReadOnlyList<string> FindHorizontalOverflow(Visual root, double width)
    {
        var problems = new List<string>();
        foreach (var visual in root.GetSelfAndVisualDescendants())
        {
            if (visual is not Control { IsVisible: true } control)
                continue;
            if (control.Bounds.Width <= 0 || control.Bounds.Height <= 0)
                continue;
            if (InsideScroller(control))
                continue;

            var edge = control.TranslatePoint(new Point(control.Bounds.Width, 0), root);
            if (edge is { } point && point.X > width + 1.5)
                problems.Add($"{Describe(control)} ends at x={point.X:0} (window {width:0})");
        }

        return problems;
    }

    private static bool InsideScroller(Visual control)
    {
        for (var parent = control.GetVisualParent(); parent is not null; parent = parent.GetVisualParent())
        {
            if (parent is ScrollViewer || parent.GetType().Name.Contains("Scroll", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static string Describe(Control control) =>
        string.IsNullOrEmpty(control.Name)
            ? control.GetType().Name
            : $"{control.GetType().Name} #{control.Name}";
}
