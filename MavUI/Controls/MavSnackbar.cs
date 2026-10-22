using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 inverse-surface Snackbar notification with optional action button,
/// close button, and non-blocking overlay presentation.
/// </summary>
public static class MavSnackbar
{
    private sealed record QueuedSnackbar(
        string Message,
        string? ActionText,
        Action? OnAction,
        bool ShowCloseIcon,
        MavSnackbarDuration Duration,
        Visual? Anchor);

    private static readonly Queue<QueuedSnackbar> Queue = new();
    private static IDisposable? _activeHandle;
    private static DispatcherTimer? _dismissTimer;

    public static int QueueCount => Queue.Count;

    /// <summary>Adds a snackbar to the queue and shows it when no other snackbar is visible.</summary>
    public static void Enqueue(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MavSnackbarDuration duration = MavSnackbarDuration.Short,
        Visual? anchor = null)
    {
        if (_activeHandle is not null)
        {
            Queue.Enqueue(new QueuedSnackbar(message, actionText, onAction, showCloseIcon, duration, anchor));
            return;
        }

        ShowCore(message, actionText, onAction, showCloseIcon, duration, anchor);
    }

    /// <summary>
    /// Removes all snackbars waiting in the queue.
    /// </summary>
    public static void ClearQueue() => Queue.Clear();

    /// <summary>Shows a snackbar immediately, replacing the one that is visible.</summary>
    public static void Show(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MavSnackbarDuration duration = MavSnackbarDuration.Short,
        Visual? anchor = null)
    {
        Queue.Clear();
        ShowCore(message, actionText, onAction, showCloseIcon, duration, anchor);
    }

    private static void ShowCore(
        string message,
        string? actionText,
        Action? onAction,
        bool showCloseIcon,
        MavSnackbarDuration duration,
        Visual? anchor)
    {
        DismissCurrentOnly();

        var t = MavTheme.Current;
        var c = t.Color;

        // An indefinite snackbar has no auto-dismiss timer, so it must always offer
        // an explicit close affordance (especially on touch/web where there is no
        // Escape key and the action text may be absent).
        var showClose = showCloseIcon || duration == MavSnackbarDuration.Indefinite;

        var msgLbl = new TextBlock
        {
            Text = message,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        };
        msgLbl.Apply(MavTypeRole.BodyMedium, t.Typography);
        msgLbl.Foreground = MavTheme.BrushOf(c.InverseOnSurface);

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Add(msgLbl, 0, 0);

        if (!string.IsNullOrEmpty(actionText))
        {
            var actLbl = new TextBlock
            {
                Text = actionText,
                VerticalAlignment = VerticalAlignment.Center,
            };
            actLbl.Apply(MavTypeRole.LabelLarge, t.Typography);
            actLbl.Foreground = MavTheme.BrushOf(c.InversePrimary);

            var actBtn = new MavInkSurface
            {
                MinHeight = 36,
                CornerRadius = 18,
                Padding = new Thickness(12, 0),
                RippleColor = c.InversePrimary,
                Content = actLbl,
            };
            actBtn.Clicked += (_, _) =>
            {
                // Dismiss first, then run the callback, so the callback can show a new snackbar
                // without the old one still occupying the slot.
                Dismiss();
                onAction?.Invoke();
            };
            row.Add(actBtn, 1, 0);
        }

        if (showClose)
        {
            // The standard variant is tinted for light surfaces and disappears on the inverse surface;
            // the tonal variant keeps its own container, so the glyph stays visible here.
            var closeBtn = new MavIconButton(MavIcons.Close, MavIconButtonVariant.Tonal)
            {
                ButtonSize = 32,
                TouchTargetSize = 36,
                VerticalAlignment = VerticalAlignment.Center,
            };
            MavTooltip.SetText(closeBtn, MavLocalization.Dismiss);
            closeBtn.Clicked += (_, _) => Dismiss();
            row.Add(closeBtn, 2, 0);
        }

        var bar = new MavSurface
        {
            MinWidth = 280,
            MaxWidth = 560,
            MinHeight = 48,
            Radius = 4,
            Elevation = 3,
            Padding = new Thickness(16, 6, showClose || !string.IsNullOrEmpty(actionText) ? 8 : 16, 6),
            FillColor = c.InverseSurface,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Content = row,
        };

        // Keep the bar clear of the gesture navigation bar and of the bottom app bar,
        // also after a rotation changes the safe-area insets.
        void Reflow()
        {
            var insets = MavOverlay.SafeAreaOf(anchor);
            bar.Margin = new Thickness(
                16 + insets.Left,
                0,
                16 + insets.Right,
                20 + ResolveBottomOffset(anchor) + insets.Bottom);
        }

        Reflow();
        IDisposable? handle = null;
        handle = MavOverlay.Show(
            anchor,
            bar,
            passThroughScrim: true,
            onViewportChanged: Reflow,
            onClosed: () => OnActiveOverlayClosed(handle));
        _activeHandle = handle;
        bar.Opacity = 0;
        bar.SetTranslationY(24);
        MavMotion.Animate(bar, "sb_in", 0, 1, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            bar.Opacity = v;
            bar.SetTranslationY(24 * (1 - v));
        });

        var ms = (int)duration;
        if (ms > 0)
        {
            _dismissTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ms)
            };
            _dismissTimer.Tick += (_, _) => Dismiss();
            _dismissTimer.Start();
        }
    }

    /// <summary>Hides the visible snackbar and clears the queue.</summary>
    public static void Dismiss()
    {
        DismissCurrentOnly();
        if (Queue.TryDequeue(out var next))
        {
            ShowCore(next.Message, next.ActionText, next.OnAction, next.ShowCloseIcon, next.Duration, next.Anchor);
        }
    }

    /// <summary>
    /// Distance from the window bottom that the snackbar must keep free: the
    /// system navigation bar and, when the anchor lives in an
    /// <see cref="MavNavigationPage"/>, its bottom app bar and FAB.
    /// </summary>
    private static double ResolveBottomOffset(Visual? anchor)
    {
        var topLevel = anchor is not null ? TopLevel.GetTopLevel(anchor) : MavOverlay.ResolveTopLevel(null);
        if (topLevel is null)
            return 0;

        var navigation = anchor is not null
            ? anchor.FindAncestorOfType<MavNavigationPage>(includeSelf: true)
            : topLevel.GetVisualDescendants().OfType<MavNavigationPage>().FirstOrDefault();

        return navigation?.SnackbarBottomOffset
               ?? topLevel.InsetsManager?.SafeAreaPadding.Bottom
               ?? 0;
    }

    private static void DismissCurrentOnly()
    {
        StopDismissTimer();

        // Null the field first: the handle's onClosed callback is then a no-op,
        // so an explicit Dismiss still advances the queue exactly once.
        var handle = _activeHandle;
        _activeHandle = null;
        handle?.Dispose();
    }

    private static void OnActiveOverlayClosed(IDisposable? handle)
    {
        if (handle is null || !ReferenceEquals(_activeHandle, handle))
            return;

        // The overlay was closed outside MavSnackbar (top-level close, page
        // teardown or a direct handle.Dispose). Its queued messages belong to the
        // same dead context, so drop them instead of showing them in another window.
        _activeHandle = null;
        StopDismissTimer();
        Queue.Clear();
    }

    private static void StopDismissTimer()
    {
        if (_dismissTimer is null)
            return;

        _dismissTimer.Stop();
        _dismissTimer = null;
    }
}
