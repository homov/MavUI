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

/// <summary>Default implementation of <see cref="IMavSnackbarService"/>; forwards to the static <see cref="MavSnackbar"/> API.</summary>
public sealed class MavSnackbarService : IMavSnackbarService
{
    public static MavSnackbarService Instance { get; } = new();

    /// <summary>Shows this snackbar instance immediately.</summary>
    public void Show(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MavSnackbarDuration duration = MavSnackbarDuration.Short) =>
        MavSnackbar.Show(message, actionText, onAction, showCloseIcon, duration);

    /// <summary>Adds this snackbar instance to the queue.</summary>
    public void Enqueue(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MavSnackbarDuration duration = MavSnackbarDuration.Short) =>
        MavSnackbar.Enqueue(message, actionText, onAction, showCloseIcon, duration);

    public void Dismiss() => MavSnackbar.Dismiss();
}
