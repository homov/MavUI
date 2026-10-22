using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Animation used when a <see cref="MavNavigationPage"/> changes its page.</summary>
public enum MavNavigationTransition
{
    None,
    Slide,
    Fade,
}
