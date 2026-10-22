using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

/// <summary>
/// Breaks down the cost of a theme switch (docs/ANALYSIS.md, section 0 and item 4):
/// the theme rebuild itself, and the per-control apply cost for each control type.
/// Results are written to ci/benchmark-theme-breakdown.txt.
/// </summary>
public class ThemeSwitchBreakdownBenchmarks
{
    private const int Warmup = 2;
    private const int Rounds = 6;
    private const int Count = 500;

    [AvaloniaFact]
    public void Theme_switch_breakdown()
    {
        var lines = new List<string>();

        // 1. Cost of building the theme alone (no visual tree, no subscribers).
        var build = typeof(MdTheme).GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("MdTheme.Build not found");
        var buildTimes = Measure(() => build.Invoke(null, null), 3, 8);
        lines.Add($"Build() alone (scheme generation): median {Median(buildTimes):F2} ms");

        // 2. Switch with an empty visual tree: rebuild + resource sync + no observers.
        var empty = MeasureSwitchWith(() => new StackPanel(), 0);
        lines.Add($"switch, 0 controls: median {empty:F2} ms");

        // 3. Switch cost per control type (500 attached controls of one type).
        var kinds = new (string Name, Func<Control> Create)[]
        {
            ("MdButton", () => new MdButton { Text = "x" }),
            ("MdCard", () => new MdCard { Body = new global::Avalonia.Controls.TextBlock { Text = "x" } }),
            ("MdTextField", () => new MdTextField { Label = "x" }),
            ("MdSlider", () => new MdSlider(0, 100, 50, 0)),
        };

        foreach (var (name, create) in kinds)
        {
            var t = MeasureSwitchWith(create, Count);
            lines.Add($"switch, {Count} x {name}: median {t:F2} ms (net of empty: {t - empty:F2} ms, per control {(t - empty) * 1000 / Count:F1} us)");
        }

        // 4. Same type, but the controls are NOT attached (apply should be skipped).
        var detached = MeasureSwitchDetached(() => new MdButton { Text = "x" }, Count);
        lines.Add($"switch, {Count} x MdButton detached (not in visual tree): median {detached:F2} ms");

        Report(lines);
    }

    [AvaloniaFact]
    public void Theme_switch_phases()
    {
        // Splits one switch into: synchronous part (Mode setter -> Changed -> apply of every control)
        // and deferred part (RunJobs: layout and render work queued by the property changes).
        var panel = new StackPanel();
        for (int i = 0; i < Count; i++)
            panel.Children.Add(new MdButton { Text = "x" });
        var window = new Window { Width = 800, Height = 900, Content = new ScrollViewer { Content = panel } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // MD3_PROFILE_ROUNDS lets the profiling job run the loop long enough to attach a tracer.
        var rounds = int.TryParse(Environment.GetEnvironmentVariable("MD3_PROFILE_ROUNDS"), out var n) && n > 0
            ? n
            : Rounds;

        var sync = new List<double>();
        var deferred = new List<double>();
        for (int i = 0; i < Warmup + rounds; i++)
        {
            var s = Stopwatch.StartNew();
            if (i % 2 == 0) MdTheme.UseDark(); else MdTheme.UseLight();
            s.Stop();
            var d = Stopwatch.StartNew();
            Dispatcher.UIThread.RunJobs();
            d.Stop();
            if (i >= Warmup)
            {
                sync.Add(s.Elapsed.TotalMilliseconds);
                deferred.Add(d.Elapsed.TotalMilliseconds);
            }
        }

        MdTheme.UseLight();
        Dispatcher.UIThread.RunJobs();
        window.Content = null;
        window.Close();
        Dispatcher.UIThread.RunJobs();

        Report(new List<string>
        {
            $"phases, {Count} x MdButton attached: synchronous Mode set (apply of all controls) median {Median(sync):F2} ms; " +
            $"deferred RunJobs (layout/render) median {Median(deferred):F2} ms",
        }, "benchmark-theme-phases.txt");
    }

    /// <summary>Attaches <paramref name="count"/> controls to a window and measures theme switches.</summary>
    private static double MeasureSwitchWith(Func<Control> create, int count)
    {
        var panel = new StackPanel();
        for (int i = 0; i < count; i++)
            panel.Children.Add(create());

        var window = new Window { Width = 800, Height = 900, Content = new ScrollViewer { Content = panel } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var median = MedianSwitch();

        window.Content = null;
        window.Close();
        Dispatcher.UIThread.RunJobs();
        MdTheme.UseLight();
        Dispatcher.UIThread.RunJobs();
        return median;
    }

    /// <summary>Controls are created but never attached to a window.</summary>
    private static double MeasureSwitchDetached(Func<Control> create, int count)
    {
        for (int i = 0; i < count; i++)
            _ = create();
        var median = MedianSwitch();
        MdTheme.UseLight();
        Dispatcher.UIThread.RunJobs();
        return median;
    }

    private static double MedianSwitch()
    {
        var times = new List<double>();
        for (int i = 0; i < Warmup + Rounds; i++)
        {
            var sw = Stopwatch.StartNew();
            if (i % 2 == 0) MdTheme.UseDark(); else MdTheme.UseLight();
            Dispatcher.UIThread.RunJobs();
            sw.Stop();
            if (i >= Warmup) times.Add(sw.Elapsed.TotalMilliseconds);
        }
        return Median(times);
    }

    private static List<double> Measure(Func<object?> action, int warmup, int rounds)
    {
        for (int i = 0; i < warmup; i++) action();
        var times = new List<double>();
        for (int i = 0; i < rounds; i++)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }
        return times;
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;
    }

    private static void Report(List<string> lines, string file = "benchmark-theme-breakdown.txt")
    {
        var dir = Path.Combine(FindRepoRoot(), "ci");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, file),
            string.Join(Environment.NewLine, lines) + Environment.NewLine);
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
