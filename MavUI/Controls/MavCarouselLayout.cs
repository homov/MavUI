using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Layout mode of a <see cref="MavCarousel"/>: how neighbouring items are shown around the focused one.</summary>
public enum MavCarouselLayout
{
    Hero,
    CenterAligned,
    MultiBrowse,
    Uncontained,
}
