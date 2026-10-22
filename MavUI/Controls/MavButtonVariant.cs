using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Visual style of a <see cref="MavButton"/> (filled, tonal, outlined, text, and so on).</summary>
public enum MavButtonVariant
{
    Filled,
    Tonal,
    Elevated,
    Outlined,
    Text,
}
