using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Mav.Theming;

/// <summary>
/// Motion profile used by Material Design 3 controls.
/// </summary>
public enum MavMotionScheme
{
    Standard,
    Expressive,
    Reduced,
    None,
}
