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

/// <summary>When a text input shows its context menu: never, on right click, or on long press.</summary>
public enum MavTextContextMenuMode
{
    Auto,
    Desktop,
    Mobile,
}
