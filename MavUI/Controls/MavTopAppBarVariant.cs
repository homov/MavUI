using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Layout of a <see cref="MavTopAppBar"/>: small, center-aligned, medium or large.</summary>
public enum MavTopAppBarVariant
{
    CenterAligned,
    Small,
    Medium,
    Large,
}
