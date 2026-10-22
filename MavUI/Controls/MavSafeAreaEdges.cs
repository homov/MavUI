using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Mav.Primitives;

namespace Mav.Controls;

/// <summary>Screen edges a <see cref="MavSafeArea"/> keeps clear of system insets.</summary>
[Flags]
public enum MavSafeAreaEdges
{
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8,
    Horizontal = Left | Right,
    Vertical = Top | Bottom,
    All = Left | Top | Right | Bottom,
}
