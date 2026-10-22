using System;
using Avalonia.Media;

namespace Mav.Theming;

/// <summary>
/// WCAG 2.1 accessibility helpers for Mav color roles.
/// </summary>
public static class MavAccessibility
{
    /// <summary>Minimum contrast for normal-size body text (WCAG AA).</summary>
    public const double MinimumBodyTextContrast = 4.5;

    /// <summary>Minimum contrast for large text and non-text UI components (WCAG AA).</summary>
    public const double MinimumLargeTextContrast = 3.0;

    /// <summary>
    /// Relative luminance of an sRGB color as defined by WCAG 2.1.
    /// Alpha is ignored; pass opaque role colors.
    /// </summary>
    public static double RelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) +
               0.7152 * Channel(color.G) +
               0.0722 * Channel(color.B);
    }

    /// <summary>
    /// WCAG contrast ratio between two opaque colors, in the range 1.0–21.0.
    /// </summary>
    public static double ContrastRatio(Color first, Color second)
    {
        var a = RelativeLuminance(first);
        var b = RelativeLuminance(second);
        var lighter = Math.Max(a, b);
        var darker = Math.Min(a, b);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>True when the pair reaches <paramref name="minimum"/> contrast.</summary>
    public static bool MeetsContrast(Color foreground, Color background, double minimum = MinimumBodyTextContrast) =>
        ContrastRatio(foreground, background) + 1e-9 >= minimum;
}
