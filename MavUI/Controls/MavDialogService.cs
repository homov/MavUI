using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Default implementation of <see cref="IMavDialogService"/>; shows dialogs on the current top-level window.</summary>
public sealed class MavDialogService : IMavDialogService
{
    public static MavDialogService Instance { get; } = new();

    /// <summary>Shows an alert with the given title and message in the current window.</summary>
    public Task ShowAlertAsync(string title, string message, string? confirmText = null, string? icon = null) =>
        MavDialog.ShowAlertAsync(title, message, confirmText, icon);

    /// <summary>Shows a confirmation with the given title and message and returns <c>true</c> if the user confirmed.</summary>
    public Task<bool> ShowConfirmAsync(string title, string message, string? confirmText = null, string? dismissText = null, string? icon = null) =>
        MavDialog.ShowConfirmAsync(title, message, confirmText, dismissText, icon);
}
