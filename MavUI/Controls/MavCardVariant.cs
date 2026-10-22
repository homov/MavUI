using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Visual style of a <see cref="MavCard"/>.</summary>
public enum MavCardVariant
{
    Elevated,
    Filled,
    Outlined,
}
