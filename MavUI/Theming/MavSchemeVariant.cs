using Avalonia.Media;
using Mav.ColorUtilities.ColorAppearance;
using Mav.ColorUtilities.Palettes;
using PaletteStyle = Mav.ColorUtilities.Palettes.Style;

namespace Mav.Theming;

/// <summary>
/// Controls how a dynamic Material Design 3 color scheme is derived from a seed color.
/// </summary>
public enum MavSchemeVariant
{
    TonalSpot,
    Neutral,
    Vibrant,
    Expressive,
    Monochrome,
    Fidelity,
    Content,
    Rainbow,
    FruitSalad,
}
