using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Mav.Platform;
using Mav.Primitives;

namespace Mav.Controls;

/// <summary>Kind of navigation that changed the visible page in a <see cref="MavNavigationPage"/>.</summary>
public enum MavPageNavigationType
{
    Root,
    Push,
    Pop,
    Replace,
}
