using Avalonia;
using Avalonia.Controls;

namespace Md3.Avalonia.Platform;

/// <summary>
/// Dismisses the soft keyboard and clears text input focus on the active <see cref="TopLevel"/>.
/// </summary>
public static class MdKeyboard
{
    public static void Hide(Visual? scope = null)
    {
        try
        {
            var topLevel = scope is not null
                ? TopLevel.GetTopLevel(scope)
                : MdOverlay.ResolveTopLevel(null);

            topLevel?.FocusManager?.Focus(null);
        }
        catch
        {
            // Ignore when no TopLevel is active.
        }
    }
}
