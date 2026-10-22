using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Platform;

/// <summary>
/// Attaches a Material Design 3 adaptive text editing popup menu (<see cref="MavTextContextFlyout"/>)
/// to any <see cref="TextBox"/>:
/// <list type="bullet">
///   <item><description><b>Mobile / Touch (Android &amp; iOS)</b>: compact horizontal floating pill toolbar above the selection/caret with context-filtered actions (Cut, Copy, Paste, Select all, Undo, Redo), horizontal scroll on narrow screens, and non-focus-stealing transient behavior that preserves selection handles and soft keyboard.</description></item>
///   <item><description><b>Desktop (Mouse &amp; Keyboard)</b>: vertical Material 3 menu at the pointer or caret with leading icons, localized labels, and platform keyboard shortcut hints.</description></item>
/// </list>
/// </summary>
public static class MavTextContextMenu
{
    /// <summary>Attaches the platform context menu to <paramref name="textBox"/>, using the given <paramref name="mode"/>.</summary>
    public static MavTextContextFlyout Attach(TextBox textBox, MavTextContextMenuMode mode = MavTextContextMenuMode.Auto)
    {
        ArgumentNullException.ThrowIfNull(textBox);

        if (textBox.Text is null)
            textBox.SetCurrentValue(TextBox.TextProperty, string.Empty);

        if (textBox.ContextFlyout is MavTextContextFlyout existing)
        {
            existing.Mode = mode;
            return existing;
        }

        textBox.ContextMenu = null;
        var flyout = new MavTextContextFlyout(textBox)
        {
            Mode = mode,
        };
        textBox.ContextFlyout = flyout;
        return flyout;
    }
}
