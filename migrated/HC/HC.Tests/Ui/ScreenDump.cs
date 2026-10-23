using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Xunit;

namespace Hc.Tests.Ui;

/// <summary>
/// Writes a plain-text outline of the visible controls (type, name, text, bounds). The CI reads these files and
/// shows them as annotations, so a test run can be inspected without a display or downloadable artifacts.
/// The folder comes from HC_UI_REPORT_DIR; without it the files go to the temp folder.
/// </summary>
internal static class ScreenDump
{
    private const int MaxLines = 400;

    public static string ReportFolder =>
        Environment.GetEnvironmentVariable("HC_UI_REPORT_DIR") is { Length: > 0 } dir
            ? dir
            : Path.Combine(Path.GetTempPath(), "hc-ui-report");

    public static string Write(Visual root, string name)
    {
        var lines = new List<string>();
        Walk(root, 0, lines);
        if (lines.Count > MaxLines)
        {
            lines.RemoveRange(MaxLines, lines.Count - MaxLines);
            lines.Add($"... truncated after {MaxLines} lines");
        }

        Directory.CreateDirectory(ReportFolder);
        var path = Path.Combine(ReportFolder, name + ".txt");
        File.WriteAllLines(path, lines);
        return path;
    }

    /// <summary>Saves the rendered window as PNG. The CI turns the file into base64 annotations.</summary>
    public static string Capture(Window window, string name)
    {
        Directory.CreateDirectory(ReportFolder);
        var path = Path.Combine(ReportFolder, name + ".png");
        // Headless windows draw only on a render tick; force a few so the frame is ready.
        for (var i = 0; i < 3; i++)
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = window.CaptureRenderedFrame();
        Assert.True(frame is not null, $"CaptureRenderedFrame returned no frame for {name}.");
        // PngBitmapEncoderOptions: the overload with an int? quality is obsolete in Avalonia 12.1.
        frame!.Save(path, PngBitmapEncoderOptions.Default);
        return path;
    }

    private static void Walk(Visual visual, int depth, List<string> lines)
    {
        if (visual is Control { IsVisible: true } control)
        {
            var text = ReadText(control);
            // Typed values are never written out: the annotations are visible to everyone who can read the checks.
            if (text is { Length: > 0 } && IsInput(control))
                text = $"<{text.Length} chars hidden>";
            var bounds = control.Bounds;
            var name = string.IsNullOrEmpty(control.Name) ? string.Empty : " #" + control.Name;
            var shown = text is null ? string.Empty : $" \"{text.Replace("\r", " ").Replace("\n", " ")}\"";
            lines.Add($"{new string(' ', depth * 2)}{control.GetType().Name}{name}{shown} " +
                      $"[{bounds.X:0},{bounds.Y:0} {bounds.Width:0}x{bounds.Height:0}]");
            depth++;
        }

        foreach (var child in visual.GetVisualChildren())
            Walk(child, depth, lines);
    }

    private static bool IsInput(Control control)
    {
        var type = control.GetType().Name;
        return control is TextBox || type.Contains("TextField") || type.Contains("Entry") || type.Contains("PasswordBox")
               || type == "TextPresenter" || type == "TextEditor";
    }

    private static string? ReadText(Control control)
    {
        var property = control.GetType().GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(control) as string;
    }
}
