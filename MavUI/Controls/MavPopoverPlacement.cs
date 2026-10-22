using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Side of the anchor on which a <see cref="MavPopover"/> opens.</summary>
public enum MavPopoverPlacement
{
    Auto,
    Bottom,
    Top,
    Left,
    Right,
}
