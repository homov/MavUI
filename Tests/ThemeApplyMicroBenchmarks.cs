using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Mav.Controls;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// Step-by-step cost of the per-control theme apply.
/// Private apply steps are called through reflection on one attached control, alternating
/// dark/light schemes so that every call changes colors (the real theme-switch path).
/// Results are written to ci/benchmark-apply-micro.txt.
/// </summary>
public class ThemeApplyMicroBenchmarks
{
    private const int Iterations = 300;

    [AvaloniaFact]
    public void Apply_steps_per_control()
    {
        var button = new MavButton { Text = "x", Variant = MavButtonVariant.Filled };
        var field = new MavTextField { Label = "x" };
        var panel = new StackPanel { Children = { button, field } };
        var window = new Window { Width = 400, Height = 300, Content = panel };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var buildMethod = typeof(MavTheme).GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Static)!;
        var modeField = typeof(MavTheme).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Static)!;
        var originalMode = modeField.GetValue(null);

        const BindingFlags inst = BindingFlags.NonPublic | BindingFlags.Instance;
        var sizeTokens = typeof(MavButton).GetMethod("ApplySizeTokens", inst)!;
        var shape = typeof(MavButton).GetMethod("ApplyShape", inst)!;
        var buttonTheme = typeof(MavButton).GetMethod("ApplyTheme", inst, [typeof(MavTheme)])!;
        var fieldTheme = typeof(MavTextField).GetMethod("ApplyTheme", inst, [typeof(MavTheme)])!;

        var tSize = new List<double>();
        var tShape = new List<double>();
        var tButton = new List<double>();
        var tField = new List<double>();

        for (int i = 0; i < Iterations; i++)
        {
            // Alternate schemes without raising MavTheme.Changed: Build() reads _mode directly.
            modeField.SetValue(null, i % 2 == 0 ? MavThemeMode.Dark : MavThemeMode.Light);
            var theme = (MavTheme)buildMethod.Invoke(null, null)!;

            tSize.Add(Time(() => sizeTokens.Invoke(button, [theme])));
            tShape.Add(Time(() => shape.Invoke(button, [theme])));
            tButton.Add(Time(() => buttonTheme.Invoke(button, [theme])));
            tField.Add(Time(() => fieldTheme.Invoke(field, [theme])));
        }

        modeField.SetValue(null, originalMode);
        MavTheme.UseLight();
        window.Close();
        Dispatcher.UIThread.RunJobs();

        var lines = new List<string>
        {
            $"MavButton.ApplySizeTokens: median {Median(tSize):F1} us",
            $"MavButton.ApplyShape:      median {Median(tShape):F1} us",
            $"MavButton.ApplyTheme (full): median {Median(tButton):F1} us",
            $"MavTextField.ApplyTheme:   median {Median(tField):F1} us",
            $"(iterations={Iterations}; ApplyTheme of button includes size tokens and shape)",
        };
        Report(lines);
    }

    private static double Time(Action action)
    {
        var start = Stopwatch.GetTimestamp();
        action();
        var ticks = Stopwatch.GetTimestamp() - start;
        return ticks * 1_000_000.0 / Stopwatch.Frequency; // microseconds
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
        File.WriteAllText(Path.Combine(dir, "benchmark-apply-micro.txt"),
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
