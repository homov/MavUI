using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Layout of a <see cref="MavToolbar"/>: standard, floating or docked.</summary>
public enum MavToolbarVariant
{
    Floating,
    Docked,
}
