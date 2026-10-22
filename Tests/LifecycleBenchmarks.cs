using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

/// <summary>
/// Baseline measurements for the analysis in docs/ANALYSIS.md (item 1: leaks, theme subscriptions).
/// Results are written to the test output (ci/last-build.log) so runs can be compared.
/// </summary>
public class LifecycleBenchmarks
{
    private const int ControlsPerKind = 125; // 4 kinds x 125 = 500 controls

    private readonly ITestOutputHelper _output;

    public LifecycleBenchmarks(ITestOutputHelper output) => _output = output;

    [AvaloniaFact]
    public void Create_and_close_500_controls_releases_theme_subscriptions_and_memory()
    {
        var baseline = CountThemeSubscribers();

        var refs = BuildAndShow(out var window);
        Dispatcher.UIThread.RunJobs();
        var peak = CountThemeSubscribers();

        window.Content = null;
        window.Close();
        Dispatcher.UIThread.RunJobs();

        var after = CountThemeSubscribers();
        var alive = CollectAndCountAlive(refs);

        Report(
            "lifecycle",
            $"controls={refs.Count}, theme subscribers: baseline={baseline}, " +
            $"while shown={peak}, after close={after}; controls still alive after GC={alive}");

        Assert.Equal(baseline, after);
    }

    [AvaloniaFact]
    public void Theme_switch_cost_with_500_attached_controls()
    {
        var refs = BuildAndShow(out var window);
        Dispatcher.UIThread.RunJobs();

        const int rounds = 6;
        var times = new List<double>();
        for (int i = 0; i < rounds; i++)
        {
            var sw = Stopwatch.StartNew();
            if (i % 2 == 0) MdTheme.UseDark(); else MdTheme.UseLight();
            Dispatcher.UIThread.RunJobs();
            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        MdTheme.UseLight();
        Dispatcher.UIThread.RunJobs();

        Report(
            "theme",
            $"{refs.Count} attached controls, switch ms: " +
            string.Join(", ", times.Select(t => t.ToString("F1"))) +
            $"; median={Median(times):F1} ms");

        window.Content = null;
        window.Close();
    }

    /// <summary>
    /// VSTest does not show xUnit v3 test output in the console log, so results are
    /// written to ci/benchmark-&lt;name&gt;.txt (published to the branch by the CI workflow).
    /// </summary>
    private void Report(string name, string line)
    {
        _output.WriteLine(line);
        var dir = Path.Combine(FindRepoRoot(), "ci");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"benchmark-{name}.txt"), line + Environment.NewLine);
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

    private static List<WeakReference> BuildAndShow(out Window window)
    {
        var refs = new List<WeakReference>();
        var panel = new StackPanel();
        for (int i = 0; i < ControlsPerKind; i++)
        {
            var button = new MdButton { Text = $"B{i}" };
            var card = new MdCard { Body = new global::Avalonia.Controls.TextBlock { Text = $"C{i}" } };
            var field = new MdTextField { Label = $"F{i}" };
            var slider = new MdSlider(0, 100, i % 100, 0);

            refs.Add(new WeakReference(button));
            refs.Add(new WeakReference(card));
            refs.Add(new WeakReference(field));
            refs.Add(new WeakReference(slider));

            panel.Children.Add(button);
            panel.Children.Add(card);
            panel.Children.Add(field);
            panel.Children.Add(slider);
        }

        window = new Window { Width = 600, Height = 800, Content = new ScrollViewer { Content = panel } };
        window.Show();
        return refs;
    }

    private static int CollectAndCountAlive(List<WeakReference> refs)
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return refs.Count(r => r.IsAlive);
    }

    /// <summary>
    /// Number of delegates attached to the static <see cref="MdTheme.Changed"/> event.
    /// Reads the compiler-generated backing field via reflection (test-only).
    /// </summary>
    private static int CountThemeSubscribers()
    {
        var field = typeof(MdTheme).GetField("Changed", BindingFlags.NonPublic | BindingFlags.Static);
        var handler = field?.GetValue(null) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;
    }
}
