using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Drawing style of a <see cref="MavSparkline"/>: line or bars.</summary>
public enum MavSparklineKind
{
    Line,
    Bar,
    Donut,
}
