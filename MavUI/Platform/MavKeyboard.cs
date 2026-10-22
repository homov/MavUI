using Avalonia;
using Avalonia.Controls;

namespace Mav.Platform;

/// <summary>
/// Dismisses the soft keyboard and clears text input focus on the active <see cref="TopLevel"/>.
/// </summary>
public static class MavKeyboard
{
    public static void Hide(Visual? scope = null)
    {
        try
        {
            var topLevel = scope is not null
                ? TopLevel.GetTopLevel(scope)
                : MavOverlay.ResolveTopLevel(null);

            topLevel?.FocusManager?.Focus(null);
        }
        catch
        {
            // Ignore when no TopLevel is active.
        }
    }
}
