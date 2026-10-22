using System.Collections.Concurrent;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace Md3.Avalonia.Platform;

/// <summary>
/// Measures single-line text width in DIPs for floating-label outlined cutouts and badges.
/// </summary>
public static class MdTextMetrics
{
    private const int MaxCacheEntries = 2048;
    private static readonly ConcurrentDictionary<(string? Culture, string? Text, double FontSize, bool Bold), double> WidthCache = new();

    public static double MeasureWidth(string? text, double fontSize, bool bold = false)
    {
        if (string.IsNullOrEmpty(text) || fontSize <= 0)
            return 0;

        var culture = CultureInfo.CurrentUICulture?.Name ?? string.Empty;
        var key = (culture, text, fontSize, bold);
        if (WidthCache.TryGetValue(key, out var cached))
            return cached;

        double result = 0;
        try
        {
            var ft = new FormattedText(
                text,
                CultureInfo.CurrentUICulture ?? CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                bold ? MdFonts.RobotoMediumTypeface : MdFonts.RobotoRegularTypeface,
                fontSize,
                Brushes.Black);
            if (ft.Width > 0)
            {
                result = ft.Width + 1.0;
                if (WidthCache.Count < MaxCacheEntries)
                    WidthCache[key] = result;
                return result;
            }
        }
        catch
        {
            // Fallback to proportional table when FontManager is not yet initialized.
        }

        double units = 0;
        foreach (var ch in text)
        {
            units += ch switch
            {
                ' ' => 0.28,
                'i' or 'l' or 'I' or 'j' or '!' or '.' or ',' or ':' or ';' or '\'' or '|' => 0.28,
                'f' or 't' or 'r' or '(' or ')' or '[' or ']' or '-' => 0.36,
                'm' or 'w' or 'M' or 'W' or '@' or '%' => 0.86,
                >= 'A' and <= 'Z' => 0.66,
                >= '0' and <= '9' => 0.56,
                >= '\u0400' and <= '\u04FF' => char.IsUpper(ch) ? 0.68 : 0.56,
                _ => 0.54,
            };
        }

        if (bold) units *= 1.05;
        return units * fontSize + 2.0;
    }
}
