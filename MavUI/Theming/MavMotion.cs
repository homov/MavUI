using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mav.Theming;

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

    /// <summary>Applies the standard easing curve to <paramref name="t"/>.</summary>
    public static double StandardInterpolate(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3.0 - 2.0 * t);
    }

    /// <summary>Applies the deceleration easing curve to <paramref name="t"/>.</summary>
    public static double DecelerateInterpolate(double t)
    {
        t = Math.Clamp(t, 0, 1);
        var inv = 1.0 - t;
        return 1.0 - inv * inv * inv;
    }

    /// <summary>Applies the acceleration easing curve to <paramref name="t"/>.</summary>
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
    private static WeakReference<TopLevel>? _frameSource;
    private static bool _frameRequested;

    /// <summary>Animates a property from one value to another over the given duration and easing.</summary>
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
        // Linear easing marks endless loops (e.g. an indeterminate indicator). Those keep their
        // full duration: reduced or disabled motion would stop the loop for no benefit.
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

        // Nothing to animate (motion disabled, or the value is already there): apply the final
        // value immediately, so callers still get the callback and the finished notification.
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

        // A new animation replaces any running one with the same owner and name. The tick list
        // is guarded by a lock because the frame callback reads it as well.
        lock (TickList)
        {
            RemoveLocked(owner, name);
            TickList.Add(entry);
            EnsureTicking(owner);
        }
    }

    /// <summary>
    /// Applies a checked/toggled state: animates it when the owner is on screen, snaps to the target
    /// when it is not. Items hosts and bindings set state before a control is attached; animating
    /// from 0 there left the control drawn in the old state until a frame ran, which read as
    /// "the current choice is not marked" (and never showed up in a screenshot).
    /// </summary>
    public static void AnimateState(
        Visual owner,
        string name,
        double from,
        double to,
        uint length,
        Func<double, double>? easing,
        Action<double> callback)
    {
        if (TopLevel.GetTopLevel(owner) is null)
        {
            Cancel(owner, name);
            callback(to);
            return;
        }

        Animate(owner, name, from, to, length, easing, callback);
    }

    /// <summary>Stops the animation with the given <paramref name="name"/> on <paramref name="owner"/>.</summary>
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

    /// <summary>
    /// Schedules the frame loop. Attached owners are driven by <see cref="TopLevel.RequestAnimationFrame"/>,
    /// so each step lands on a display frame. A fixed-interval timer does not line up with the display,
    /// which read as stutter in the browser and on Android. The timer is only a fallback for owners that
    /// are not attached to a top level yet. Called under <c>lock (TickList)</c>.
    /// </summary>
    private static void EnsureTicking(Visual owner)
    {
        var top = TopLevel.GetTopLevel(owner);
        if (top is not null)
        {
            _frameSource = new WeakReference<TopLevel>(top);
            RequestFrameLocked(top);
            return;
        }

        if (_timer is null)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += OnTimerTick;
        }

        if (!_timer.IsEnabled) _timer.Start();
    }

    private static void RequestFrameLocked(TopLevel top)
    {
        if (_frameRequested) return;
        _frameRequested = true;
        top.RequestAnimationFrame(OnFrame);
    }

    private static void ScheduleNextLocked()
    {
        if (_frameSource is not null && _frameSource.TryGetTarget(out var top))
        {
            RequestFrameLocked(top);
            return;
        }

        if (_timer is not null && !_timer.IsEnabled) _timer.Start();
    }

    private static void OnTimerTick(object? sender, EventArgs e) => Tick();

    private static void OnFrame(TimeSpan _)
    {
        lock (TickList)
        {
            _frameRequested = false;
        }

        Tick();

        lock (TickList)
        {
            if (TickList.Count > 0)
                ScheduleNextLocked();
        }
    }

    private static void Tick()
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

    /// <summary>Fades the target in from transparent to opaque.</summary>
    public static void FadeIn(Visual target, uint duration = Short3)
    {
        target.Opacity = 0;
        Animate(target, "md_fade_in", 0, 1, duration, CubicOut, v => target.Opacity = v);
    }

    /// <summary>Scales the target in from <paramref name="from"/> to its normal size while it fades in.</summary>
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

    /// <summary>Sets the uniform scale of the visual.</summary>
    public static void SetScale(this Visual visual, double scale) =>
        visual.SetScale(scale, scale);

    /// <summary>Sets the horizontal and vertical scale of the visual.</summary>
    public static void SetScale(this Visual visual, double scaleX, double scaleY)
    {
        var group = EnsureTransformGroup(visual);
        var st = (ScaleTransform)group.Children[0];
        st.ScaleX = scaleX;
        st.ScaleY = scaleY;
    }

    /// <summary>Sets the rotation of the visual, in degrees.</summary>
    public static void SetRotation(this Visual visual, double angleDegrees)
    {
        var group = EnsureTransformGroup(visual);
        var rt = (RotateTransform)group.Children[1];
        rt.Angle = angleDegrees;
    }

    /// <summary>Sets the horizontal and vertical offset of the visual.</summary>
    public static void SetTranslation(this Visual visual, double x, double y)
    {
        var group = EnsureTransformGroup(visual);
        var tt = (TranslateTransform)group.Children[2];
        tt.X = x;
        tt.Y = y;
    }

    /// <summary>Sets the horizontal offset of the visual.</summary>
    public static void SetTranslationX(this Visual visual, double x)
    {
        var group = EnsureTransformGroup(visual);
        var tt = (TranslateTransform)group.Children[2];
        tt.X = x;
    }

    /// <summary>Sets the vertical offset of the visual.</summary>
    public static void SetTranslationY(this Visual visual, double y)
    {
        var group = EnsureTransformGroup(visual);
        var tt = (TranslateTransform)group.Children[2];
        tt.Y = y;
    }

    /// <summary>Returns the horizontal offset of the visual.</summary>
    public static double GetTranslationX(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[2] is TranslateTransform tt ? tt.X : 0;

    /// <summary>Returns the vertical offset of the visual.</summary>
    public static double GetTranslationY(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[2] is TranslateTransform tt ? tt.Y : 0;

    /// <summary>Returns the horizontal scale of the visual.</summary>
    public static double GetScaleX(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[0] is ScaleTransform st ? st.ScaleX : 1;

    /// <summary>Returns the vertical scale of the visual.</summary>
    public static double GetScaleY(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[0] is ScaleTransform st ? st.ScaleY : 1;

    /// <summary>Returns the rotation of the visual, in degrees.</summary>
    public static double GetRotation(this Visual visual) =>
        visual.RenderTransform is TransformGroup g && g.Children.Count == 3 && g.Children[1] is RotateTransform rt ? rt.Angle : 0;
}
