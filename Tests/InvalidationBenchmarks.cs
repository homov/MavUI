using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// Isolates the deferred cost of a theme switch (docs/ANALYSIS.md, item 4):
/// a render-only change (background) versus a layout-affecting change (padding) on the same controls.
/// Results are written to ci/benchmark-invalidation.txt.
/// </summary>
public class InvalidationBenchmarks
{
    private const int Count = 500;
    private const int Warmup = 2;
    private const int Rounds = 6;

    [AvaloniaFact]
    public void Render_only_vs_layout_change_on_500_buttons()
    {
        var buttons = Enumerable.Range(0, Count).Select(i => new MavButton { Text = $"B{i}" }).ToList();
        var panel = new StackPanel();
        foreach (var b in buttons) panel.Children.Add(b);
        var window = new Window { Width = 800, Height = 900, Content = new ScrollViewer { Content = panel } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var renderOnly = new List<double>();
        var layout = new List<double>();
        var toggle = false;

        for (int i = 0; i < Warmup + Rounds; i++)
        {
            toggle = !toggle;
            var brush = toggle ? Brushes.Red : Brushes.Blue;
            var sw = Stopwatch.StartNew();
            foreach (var b in buttons) b.Background = brush;
            Dispatcher.UIThread.RunJobs();
            sw.Stop();
            if (i >= Warmup) renderOnly.Add(sw.Elapsed.TotalMilliseconds);

            var pad = toggle ? new Thickness(10, 0, 10, 0) : new Thickness(12, 0, 12, 0);
            sw.Restart();
            foreach (var b in buttons) b.Padding = pad;
            Dispatcher.UIThread.RunJobs();
            sw.Stop();
            if (i >= Warmup) layout.Add(sw.Elapsed.TotalMilliseconds);
        }

        window.Content = null;
        window.Close();
        Dispatcher.UIThread.RunJobs();

        Report(new List<string>
        {
            $"{Count} x MavButton, background change (render only): median {Median(renderOnly):F2} ms",
            $"{Count} x MavButton, padding change (layout): median {Median(layout):F2} ms",
        });
    }

    [AvaloniaFact]
    public void App_resource_sync_cost_with_500_buttons()
    {
        var buttons = Enumerable.Range(0, Count).Select(i => new MavButton { Text = $"B{i}" }).ToList();
        var panel = new StackPanel();
        foreach (var b in buttons) panel.Children.Add(b);
        var window = new Window { Width = 800, Height = 900, Content = new ScrollViewer { Content = panel } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var app = Application.Current!;
        var sync = typeof(Mav.Theming.MavTheme)
            .GetMethod("SyncAppResources", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var theme = Mav.Theming.MavTheme.Current;

        var resources = new List<double>();
        var variant = new List<double>();
        for (int i = 0; i < Warmup + Rounds; i++)
        {
            // A: all app resource writes (colors, shapes), without the variant change.
            var sw = Stopwatch.StartNew();
            sync.Invoke(null, [theme]);
            Dispatcher.UIThread.RunJobs();
            sw.Stop();
            if (i >= Warmup) resources.Add(sw.Elapsed.TotalMilliseconds);

            // B: only the RequestedThemeVariant flip (what SyncAppResources also does).
            sw.Restart();
            app.RequestedThemeVariant = (i % 2 == 0) ? ThemeVariant.Dark : ThemeVariant.Light;
            Dispatcher.UIThread.RunJobs();
            sw.Stop();
            if (i >= Warmup) variant.Add(sw.Elapsed.TotalMilliseconds);
        }

        app.RequestedThemeVariant = ThemeVariant.Default;
        window.Content = null;
        window.Close();
        Dispatcher.UIThread.RunJobs();

        var lines = new List<string>
        {
            $"{Count} x MavButton attached, SyncAppResources (app.Resources writes) + RunJobs: median {Median(resources):F2} ms",
            $"{Count} x MavButton attached, RequestedThemeVariant flip + RunJobs: median {Median(variant):F2} ms",
        };
        var dir = Path.Combine(FindRepoRoot(), "ci");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "benchmark-resources.txt"), string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;
    }

    private static void Report(List<string> lines)
    {
        var dir = Path.Combine(FindRepoRoot(), "ci");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "benchmark-invalidation.txt"),
            string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    private static string FindRepoRoot()
    {
        var env = Environment.GetEnvironmentVariable("MAV_REPO_ROOT");
        if (!string.IsNullOrEmpty(env))
            return env;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            dir = dir.Parent;

        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
