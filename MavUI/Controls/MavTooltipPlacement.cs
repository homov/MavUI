using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Side of the target on which a <see cref="MavTooltip"/> appears.</summary>
public enum MavTooltipPlacement
{
    Top,
    Bottom,
}
