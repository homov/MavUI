using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdSnackbarDuration
{
    Short = 3000,
    Long = 5500,
    Indefinite = 0,
}

/// <summary>
/// Material Design 3 inverse-surface Snackbar notification with optional action button,
/// close button, and non-blocking overlay presentation.
/// </summary>
public static class MdSnackbar
{
    private sealed record QueuedSnackbar(
        string Message,
        string? ActionText,
        Action? OnAction,
        bool ShowCloseIcon,
        MdSnackbarDuration Duration,
        Visual? Anchor);

    private static readonly Queue<QueuedSnackbar> Queue = new();
    private static IDisposable? _activeHandle;
    private static DispatcherTimer? _dismissTimer;

    public static int QueueCount => Queue.Count;

    public static void Enqueue(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MdSnackbarDuration duration = MdSnackbarDuration.Short,
        Visual? anchor = null)
    {
        if (_activeHandle is not null)
        {
            Queue.Enqueue(new QueuedSnackbar(message, actionText, onAction, showCloseIcon, duration, anchor));
            return;
        }

        ShowCore(message, actionText, onAction, showCloseIcon, duration, anchor);
    }

    public static void ClearQueue() => Queue.Clear();

    public static void Show(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MdSnackbarDuration duration = MdSnackbarDuration.Short,
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
        MdSnackbarDuration duration,
        Visual? anchor)
    {
        DismissCurrentOnly();

        var t = MdTheme.Current;
        var c = t.Color;

        // An indefinite snackbar has no auto-dismiss timer, so it must always offer
        // an explicit close affordance (especially on touch/web where there is no
        // Escape key and the action text may be absent).
        var showClose = showCloseIcon || duration == MdSnackbarDuration.Indefinite;

        var msgLbl = new TextBlock
        {
            Text = message,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        };
        msgLbl.Apply(MdTypeRole.BodyMedium, t.Typography);
        msgLbl.Foreground = MdTheme.BrushOf(c.InverseOnSurface);

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
            actLbl.Apply(MdTypeRole.LabelLarge, t.Typography);
            actLbl.Foreground = MdTheme.BrushOf(c.InversePrimary);

            var actBtn = new MdInkSurface
            {
                MinHeight = 36,
                CornerRadius = 18,
                Padding = new Thickness(12, 0),
                RippleColor = c.InversePrimary,
                Content = actLbl,
            };
            actBtn.Clicked += (_, _) =>
            {
                Dismiss();
                onAction?.Invoke();
            };
            row.Add(actBtn, 1, 0);
        }

        if (showClose)
        {
            var closeBtn = new MdIconButton(MdIcons.Close, MdIconButtonVariant.Standard)
            {
                ButtonSize = 32,
                TouchTargetSize = 36,
                VerticalAlignment = VerticalAlignment.Center,
            };
            MdTooltip.SetText(closeBtn, MdLocalization.Dismiss);
            closeBtn.Clicked += (_, _) => Dismiss();
            row.Add(closeBtn, 2, 0);
        }

        var bar = new MdSurface
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
            var insets = MdOverlay.SafeAreaOf(anchor);
            bar.Margin = new Thickness(
                16 + insets.Left,
                0,
                16 + insets.Right,
                20 + ResolveBottomOffset(anchor) + insets.Bottom);
        }

        Reflow();
        IDisposable? handle = null;
        handle = MdOverlay.Show(
            anchor,
            bar,
            passThroughScrim: true,
            onViewportChanged: Reflow,
            onClosed: () => OnActiveOverlayClosed(handle));
        _activeHandle = handle;
        bar.Opacity = 0;
        bar.SetTranslationY(24);
        MdMotion.Animate(bar, "sb_in", 0, 1, MdMotion.Short4, MdMotion.CubicOut, v =>
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
    /// <see cref="MdNavigationPage"/>, its bottom app bar and FAB.
    /// </summary>
    private static double ResolveBottomOffset(Visual? anchor)
    {
        var topLevel = anchor is not null ? TopLevel.GetTopLevel(anchor) : MdOverlay.ResolveTopLevel(null);
        if (topLevel is null)
            return 0;

        var navigation = anchor is not null
            ? anchor.FindAncestorOfType<MdNavigationPage>(includeSelf: true)
            : topLevel.GetVisualDescendants().OfType<MdNavigationPage>().FirstOrDefault();

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

        // The overlay was closed outside MdSnackbar (top-level close, page
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

/// <summary>
/// Injectable service for showing Material Design 3 snackbar notifications from ViewModels.
/// </summary>
public interface IMdSnackbarService
{
    void Show(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MdSnackbarDuration duration = MdSnackbarDuration.Short);

    void Enqueue(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MdSnackbarDuration duration = MdSnackbarDuration.Short);

    void Dismiss();
}

public sealed class MdSnackbarService : IMdSnackbarService
{
    public static MdSnackbarService Instance { get; } = new();

    public void Show(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MdSnackbarDuration duration = MdSnackbarDuration.Short) =>
        MdSnackbar.Show(message, actionText, onAction, showCloseIcon, duration);

    public void Enqueue(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MdSnackbarDuration duration = MdSnackbarDuration.Short) =>
        MdSnackbar.Enqueue(message, actionText, onAction, showCloseIcon, duration);

    public void Dismiss() => MdSnackbar.Dismiss();
}
