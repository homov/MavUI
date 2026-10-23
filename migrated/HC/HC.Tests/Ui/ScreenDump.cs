using System.Reflection;
using Avalonia.Controls;
using Avalonia.VisualTree;

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

    private static void Walk(Visual visual, int depth, List<string> lines)
    {
        if (visual is Control { IsVisible: true } control)
        {
            var text = ReadText(control);
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

    private static string? ReadText(Control control)
    {
        var property = control.GetType().GetProperty("Text", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(control) as string;
    }
}
