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

/// <summary>How long a <see cref="MavSnackbar"/> stays visible before it dismisses itself.</summary>
public enum MavSnackbarDuration
{
    Short = 3000,
    Long = 5500,
    Indefinite = 0,
}
