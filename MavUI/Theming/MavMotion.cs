using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mav.Theming;

/// <summary>
/// Motion profile used by Material Design 3 controls.
/// </summary>
public enum MavMotionScheme
{
    Standard,
    Expressive,
    Reduced,
    None,
}

/// <summary>
/// Material Design 3 motion durations, easing curves, high-precision animation ticker,
/// and transform helpers.
/// </summary>
public static class MavMotion
{
    public const uint Short1 = 50;
    public const uint Short2 = 100;
    public const uint Short3 = 150;
    public const uint Short4 = 200;

    public const uint Medium1 = 250;
    public const uint Medium2 = 300;
    public const uint Medium3 = 350;
    public const uint Medium4 = 400;

    public const uint Long1 = 450;
    public const uint Long2 = 500;
    public const uint Long3 = 550;
    public const uint Long4 = 600;

    public const uint ExtraLong1 = 700;
    public const uint ExtraLong2 = 800;
    public const uint ExtraLong3 = 900;
    public const uint ExtraLong4 = 1000;

    public static Func<double, double> Linear { get; } = static t => t;
    public static Func<double, double> CubicIn { get; } = static t => t * t * t;
    public static Func<double, double> CubicOut { get; } = static t => 1.0 - Math.Pow(1.0 - t, 3);
    public static Func<double, double> CubicInOut { get; } = static t =>
        t < 0.5 ? 4.0 * t * t * t : 1.0 - Math.Pow(-2.0 * t + 2.0, 3) / 2.0;
    public static Func<double, double> SinInOut { get; } = static t =>
        -(Math.Cos(Math.PI * t) - 1.0) / 2.0;

    public static Func<double, double> Standard => CubicInOut;
    public static Func<double, double> StandardDecelerate => CubicOut;
    public static Func<double, double> StandardAccelerate => CubicIn;
    public static Func<double, double> EmphasizedDecelerate => CubicOut;
    public static Func<double, double> EmphasizedAccelerate => CubicIn;

    public static Func<double, double> SpringOut { get; } = static t =>
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        const double c4 = (2 * Math.PI) / 4.2;
        return Math.Pow(2, -9 * t) * Math.Sin((t * 6.5 - 0.75) * c4) + 1;
    };

    public static double StandardInterpolate(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3.0 - 2.0 * t);
    }

    public static double DecelerateInterpolate(double t)
    {
        t = Math.Clamp(t, 0, 1);
        var inv = 1.0 - t;
        return 1.0 - inv * inv * inv;
    }

    public static double AccelerateInterpolate(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * t;
    }

    private sealed class AnimEntry
    {
        public required WeakReference<Visual> Owner;
        public required string Name;
        public required double From;
        public required double To;
        public required double DurationMs;
        public required Func<double, double> Easing;
        public required Action<double> Callback;
        public Action? Finished;
        public long StartTimestamp;
    }

    private static readonly List<AnimEntry> TickList = [];
    private static DispatcherTimer? _timer;

    public static void Animate(
        Visual owner,
        string name,
        double from,
        double to,
        uint length,
        Func<double, double>? easing,
        Action<double> callback,
        Action? finished = null)
    {
        var isContinuousLoop = ReferenceEquals(easing, Linear);
        var scheme = MavTheme.Motion;
        double effectiveLength = length;
        var effectiveEasing = easing ?? CubicOut;

        if (!isContinuousLoop)
        {
            switch (scheme)
            {
                case MavMotionScheme.None:
                    effectiveLength = 0;
                    break;
                case MavMotionScheme.Reduced:
                    effectiveLength = Math.Min(90.0, length * 0.35);
                    if (ReferenceEquals(effectiveEasing, SpringOut))
                        effectiveEasing = CubicOut;
                    break;
                case MavMotionScheme.Expressive:
                    effectiveLength = length * 1.15;
                    break;
            }
        }

        if (effectiveLength <= 1.0 || Math.Abs(to - from) < 1e-6)
        {
            Cancel(owner, name);
            callback(to);
            finished?.Invoke();
            return;
        }

        var entry = new AnimEntry
        {
            Owner = new WeakReference<Visual>(owner),
            Name = name,
            From = from,
            To = to,
            DurationMs = effectiveLength,
            Easing = effectiveEasing,
            Callback = callback,
            Finished = finished,
            StartTimestamp = Stopwatch.GetTimestamp(),
        };

        lock (TickList)
        {
            RemoveLocked(owner, name);
            TickList.Add(entry);
            EnsureTimer();
        }
    }

    public static void Cancel(Visual owner, string name)
    {
        lock (TickList)
        {
            RemoveLocked(owner, name);
        }
    }

    private static void RemoveLocked(Visual owner, string name)
    {
        for (int i = TickList.Count - 1; i >= 0; i--)
        {
            var e = TickList[i];
            if (!e.Owner.TryGetTarget(out var target) ||
                (ReferenceEquals(target, owner) && e.Name == name))
            {
                TickList.RemoveAt(i);
            }
        }
    }

    private static void EnsureTimer()
    {
        if (_timer is not null)
        {
            if (!_timer.IsEnabled) _timer.Start();
            return;
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnFrame;
        _timer.Start();
    }

    private static void OnFrame(object? sender, EventArgs e)
    {
        var now = Stopwatch.GetTimestamp();
        List<(Action<double> Cb, double Val, Action? Fin)>? invocations = null;

        lock (TickList)
        {
            for (int i = TickList.Count - 1; i >= 0; i--)
            {
                var entry = TickList[i];
                if (!entry.Owner.TryGetTarget(out _))
                {
                    TickList.RemoveAt(i);
                    continue;
                }

                var elapsedMs = Stopwatch.GetElapsedTime(entry.StartTimestamp, now).TotalMilliseconds;
                var raw = Math.Clamp(elapsedMs / entry.DurationMs, 0.0, 1.0);
                var eased = entry.Easing(raw);
                var value = entry.From + (entry.To - entry.From) * eased;
                var done = raw >= 1.0;

                invocations ??= [];
                invocations.Add((entry.Callback, done ? entry.To : value, done ? entry.Finished : null));

                if (done)
                    TickList.RemoveAt(i);
            }

            if (TickList.Count == 0 && _timer is not null)
                _timer.Stop();
        }

        if (invocations is null) return;
        foreach (var (cb, val, fin) in invocations)
        {
            try
            {
                cb(val);
                fin?.Invoke();
            }
            catch
            {
                // Ignore callbacks targeting disposed visuals.
            }
        }
    }

    public static void FadeIn(Visual target, uint duration = Short3)
    {
        target.Opacity = 0;
        Animate(target, "md_fade_in", 0, 1, duration, CubicOut, v => target.Opacity = v);
    }

    public static void ScaleIn(Visual target, double from = 0.92, uint duration = Medium1)
    {
        target.SetScale(from);
        target.Opacity = 0;
        Animate(target, "md_scale_in", 0, 1, duration, CubicOut, v =>
        {
            var s = from + (1.0 - from) * v;
            target.SetScale(s);
            target.Opacity = v;
        });
    }

    private static TransformGroup EnsureTransformGroup(Visual visual)
    {
        if (visual.RenderTransform is TransformGroup group &&
            group.Children.Count == 3 &&
            group.Children[0] is ScaleTransform &&
            group.Children[1] is RotateTransform &&
            group.Children[2] is TranslateTransform)
        {
            return group;
        }

        var created = new TransformGroup
        {
            Children =
            [
                new ScaleTransform(1, 1),
                new RotateTransform(0),
                new TranslateTransform(0, 0),
            ]
        };
        visual.RenderTransform = created;
        return created;
    }

    public static void SetScale(this Visual visual, double scale) =>
        visual.SetScale(scale, scale);

    public static void SetScale(this Visual visual, double scaleX, double scaleY)
    {
        var group = EnsureTransformGroup(visual);
        var st = (ScaleTransform)group.Children[0];
        st.ScaleX = scaleX;
        st.ScaleY = scaleY;
    }

    public static void SetRotation(this Visual visual, double angleDegrees)
    {
        var group = EnsureTransformGroup(visual);
        var rt = (RotateTransform)group.Children[1];
        rt.Angle = angleDegrees;
    }

    public static void SetTranslation(this Visual visual, double x, double y)
    {
        var group = EnsureTransformGroup(visual);
        var tt = (TranslateTransform)group.Children[2];
        tt.X = x;
        tt.Y = y;
    }

    public static void SetTranslationX(this Visual visual, double x)
    {
        var group = EnsureTransformGroup(visual);
        var tt = (TranslateTransform)group.Children[2];
        tt.X = x;
    }

    public static void SetTranslationY(this Visual visual, double y)
    {
        var group = EnsureTransformGroup(visual);
        var tt = (TranslateTransform)group.Children[2];
        tt.Y = y;
    }

    public static double GetTranslationX(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[2] is TranslateTransform tt ? tt.X : 0;

    public static double GetTranslationY(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[2] is TranslateTransform tt ? tt.Y : 0;

    public static double GetScaleX(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[0] is ScaleTransform st ? st.ScaleX : 1;

    public static double GetScaleY(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[0] is ScaleTransform st ? st.ScaleY : 1;

    public static double GetRotation(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[1] is RotateTransform rt ? rt.Angle : 0;
}

/// <summary>
/// Material Design 3 Shared-Axis X page transition for <see cref="NavigationPage"/>.
/// Uses wall-clock <see cref="MavMotion"/> timing so transitions never stall on Android
/// and always restore presenter transforms and visibility upon completion.
/// </summary>
public sealed class MavPageSlide : PageSlide
{
    public MavPageSlide() : base(TimeSpan.FromMilliseconds(MavMotion.Short4), SlideAxis.Horizontal)
    {
    }

    public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        var parent = (from ?? to)?.GetVisualParent();
        var width = parent is not null && parent.Bounds.Width > 0 ? parent.Bounds.Width : 360.0;
        var slideOffset = Math.Min(96.0, width * 0.22);
        var dir = forward ? 1.0 : -1.0;

        var fromTransform = new TranslateTransform(0, 0);
        var toTransform = new TranslateTransform(dir * slideOffset, 0);

        if (from is not null)
        {
            from.RenderTransform = fromTransform;
            from.Opacity = 1.0;
        }

        if (to is not null)
        {
            to.IsVisible = true;
            to.RenderTransform = toTransform;
            to.Opacity = 0.0;
        }

        var anchor = to ?? from;
        if (anchor is null)
            return;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var reg = cancellationToken.Register(() =>
        {
            MavMotion.Cancel(anchor, "md_page_slide");
            tcs.TrySetResult(false);
        });

        try
        {
            MavMotion.Animate(anchor, "md_page_slide", 0.0, 1.0, MavMotion.Short4, MavMotion.CubicOut, p =>
            {
                toTransform.X = dir * slideOffset * (1.0 - p);
                if (to is not null)
                    to.Opacity = Math.Clamp(p * 1.35, 0.0, 1.0);

                fromTransform.X = -dir * (slideOffset * 0.45) * p;
                if (from is not null)
                    from.Opacity = Math.Clamp(1.0 - p * 1.4, 0.0, 1.0);
            }, () => tcs.TrySetResult(true));

            await tcs.Task;
        }
        finally
        {
            if (to is not null)
            {
                to.RenderTransform = null;
                to.Opacity = 1.0;
                to.IsVisible = true;
            }

            if (from is not null)
            {
                from.RenderTransform = null;
                from.Opacity = 1.0;
                if (!cancellationToken.IsCancellationRequested)
                    from.IsVisible = false;
            }
        }
    }
}
