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
/// Injectable service for showing Material Design 3 snackbar notifications from ViewModels.
/// </summary>
public interface IMavSnackbarService
{
    void Show(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MavSnackbarDuration duration = MavSnackbarDuration.Short);

    void Enqueue(
        string message,
        string? actionText = null,
        Action? onAction = null,
        bool showCloseIcon = false,
        MavSnackbarDuration duration = MavSnackbarDuration.Short);

    void Dismiss();
}
