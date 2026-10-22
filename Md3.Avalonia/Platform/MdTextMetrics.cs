using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Platform;

/// <summary>
/// Measures single-line text width in DIPs for floating-label outlined cutouts and badges.
/// Also provides a bounded cache for immutable <see cref="FormattedText"/> instances used
/// by custom-drawn controls, so redraws do not allocate a new text layout every frame.
/// </summary>
public static class MdTextMetrics
{
    private const int MaxCacheEntries = 2048;
    private const int MaxFormattedTextEntries = 2048;

    /// <summary>
    /// Wrap widths are quantized to this step (DIPs) in the cache key. Without it, every
    /// new window width or animated width creates a new layout and evicts useful entries.
    /// Widths are rounded DOWN, so text never gets more room than the caller allowed.
    /// </summary>
    private const double MaxTextWidthStep = 4.0;

    private static readonly MdLruCache<(string? Culture, string? Text, double FontSize, bool Bold), double> WidthCache = new(MaxCacheEntries);
    private static readonly MdLruCache<TextKey, FormattedText> FormattedTextCache = new(MaxFormattedTextEntries);
    private static WeakReference<FontManager>? _fontManager;

    /// <summary>
    /// Returns a cached, effectively immutable text layout for custom render passes.
    /// Callers must not mutate the returned instance; use the parameters to describe
    /// wrapping and trimming instead.
    /// </summary>
    internal static FormattedText GetFormattedText(
        string text,
        double fontSize,
        Typeface typeface,
        Color color,
        double maxTextWidth = double.PositiveInfinity,
        int maxLineCount = int.MaxValue,
        TextTrimming? trimming = null)
    {
        // Layout objects reference font memory owned by the current FontManager. Clear the
        // caches when the application (or a test host) replaces that manager.
        EnsureFontManagerCurrent();

        var culture = CultureInfo.CurrentUICulture ?? CultureInfo.InvariantCulture;
        var effectiveTrimming = trimming ?? TextTrimming.WordEllipsis;
        if (!double.IsPositiveInfinity(maxTextWidth) && maxTextWidth >= MaxTextWidthStep)
            maxTextWidth = Math.Floor(maxTextWidth / MaxTextWidthStep) * MaxTextWidthStep;

        var key = new TextKey(
            culture.Name,
            text,
            fontSize,
            typeface,
            color.ToUInt32(),
            maxTextWidth,
            maxLineCount,
            effectiveTrimming);

        if (FormattedTextCache.TryGet(key, out var cached))
            return cached;

        var created = new FormattedText(
            text,
            culture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            MdTheme.BrushOf(color));

        if (!double.IsPositiveInfinity(maxTextWidth))
            created.MaxTextWidth = maxTextWidth;
        if (maxLineCount != int.MaxValue)
            created.MaxLineCount = maxLineCount;
        if (effectiveTrimming != TextTrimming.WordEllipsis)
            created.Trimming = effectiveTrimming;

        FormattedTextCache.Set(key, created);
        return created;
    }

    private static void EnsureFontManagerCurrent()
    {
        var fontManager = FontManager.Current;
        if (_fontManager is not null &&
            _fontManager.TryGetTarget(out var cachedFontManager) &&
            ReferenceEquals(cachedFontManager, fontManager))
        {
            return;
        }

        // Widths and layouts are tied to the FontManager that produced them, so
        // both caches are invalidated together.
        _fontManager = new WeakReference<FontManager>(fontManager);
        WidthCache.Clear();
        FormattedTextCache.Clear();
    }

    private readonly record struct TextKey(
        string Culture,
        string Text,
        double FontSize,
        Typeface Typeface,
        uint Color,
        double MaxTextWidth,
        int MaxLineCount,
        TextTrimming Trimming);

    public static double MeasureWidth(string? text, double fontSize, bool bold = false)
    {
        if (string.IsNullOrEmpty(text) || fontSize <= 0)
            return 0;

        EnsureFontManagerCurrent();

        var culture = CultureInfo.CurrentUICulture?.Name ?? string.Empty;
        var key = (culture, text, fontSize, bold);
        if (WidthCache.TryGet(key, out var cached))
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
                WidthCache.Set(key, result);
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
