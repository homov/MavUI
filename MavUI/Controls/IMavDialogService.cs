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

/// <summary>
/// Injectable service for presenting Material Design 3 modal alerts and confirmations from ViewModels.
/// </summary>
public interface IMavDialogService
{
    Task ShowAlertAsync(string title, string message, string? confirmText = null, string? icon = null);
    Task<bool> ShowConfirmAsync(string title, string message, string? confirmText = null, string? dismissText = null, string? icon = null);
}
