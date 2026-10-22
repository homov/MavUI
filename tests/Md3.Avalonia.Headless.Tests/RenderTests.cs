using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Headless.XUnit;
using Xunit;
using Md3.Avalonia.Controls;

namespace Md3.Avalonia.Headless.Tests;

/// <summary>
/// Renders Md3.Avalonia controls with Avalonia.Headless and saves PNG screenshots
/// to ci/screenshots (repository root), so the output can be inspected after CI.
/// </summary>
public class RenderTests
{
    [AvaloniaFact]
    public void Buttons_Render_All_Variants()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Margin = new Thickness(24) };
        foreach (var variant in Enum.GetValues<MdButtonVariant>())
            row.Children.Add(new MdButton { Text = variant.ToString(), Variant = variant });

        Capture(row, 900, 140, "buttons.png");
    }

    [AvaloniaFact]
    public void Cards_Render_All_Variants()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, Margin = new Thickness(24) };
        foreach (var variant in Enum.GetValues<MdCardVariant>())
            row.Children.Add(new MdCard(variant, new TextBlock { Text = variant.ToString(), FontSize = 16 }));

        Capture(row, 900, 180, "cards.png");
    }

    private static void Capture(Control content, double width, double height, string fileName)
    {
        var window = new Window
        {
            Width = width,
            Height = height,
            Background = Brushes.White,
            Content = content,
        };

        window.Show();
        window.Measure(new Size(width, height));
        window.Arrange(new Rect(0, 0, width, height));
        Dispatcher.UIThread.RunJobs();

        var bitmap = window.CaptureRenderedFrame();
        Assert.NotNull(bitmap);

        var dir = Path.Combine(FindRepoRoot(), "ci", "screenshots");
        Directory.CreateDirectory(dir);
        bitmap!.Save(Path.Combine(dir, fileName));

        window.Close();
    }

    private static string FindRepoRoot()
    {
        var env = Environment.GetEnvironmentVariable("MD3_REPO_ROOT");
        if (!string.IsNullOrEmpty(env))
            return env;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            dir = dir.Parent;

        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
