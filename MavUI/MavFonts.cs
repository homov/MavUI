using Avalonia.Media;

namespace Mav;

/// <summary>
/// Font family constants, typefaces and icon font resolution for Mav.
/// Registration with the Avalonia app is done by <c>UseMav</c> in <c>MavAppBuilderExtensions</c>.
/// Supports the classic Material Icons font, Material Symbols Rounded, and
/// Pictogrammers Material Design Icons (10,800+ combined glyphs).
/// </summary>
public static class MavFonts
{
    public const string MaterialIcons = "avares://MavUI/Resources/Fonts#Material Icons";
    public const string MaterialSymbols = "avares://MavUI/Resources/Fonts#Material Symbols Rounded";
    public const string MaterialDesignIcons = "avares://MavUI/Resources/Fonts#Material Design Icons";
    public const string Roboto = "avares://MavUI/Resources/Fonts#Roboto";

    public static readonly FontFamily MaterialIconsFont = new(MaterialIcons);
    public static readonly FontFamily MaterialSymbolsFont = new(MaterialSymbols);
    public static readonly FontFamily MaterialDesignIconsFont = new(MaterialDesignIcons);
    public static readonly FontFamily RobotoFont = new(Roboto);

    public static readonly Typeface MaterialIconsTypeface = new(MaterialIconsFont);
    public static readonly Typeface MaterialSymbolsTypeface = new(MaterialSymbolsFont);
    public static readonly Typeface MaterialDesignIconsTypeface = new(MaterialDesignIconsFont);
    public static readonly Typeface RobotoRegularTypeface = new(RobotoFont, FontStyle.Normal, FontWeight.Regular);
    public static readonly Typeface RobotoMediumTypeface = new(RobotoFont, FontStyle.Normal, FontWeight.Medium);
    public static readonly Typeface RobotoSemiBoldTypeface = new(RobotoFont, FontStyle.Normal, FontWeight.SemiBold);

    /// <summary>Returns the typeface that contains the given icon glyph, for drawing icons with <see cref="DrawingContext"/>.</summary>
    public static Typeface ResolveIconTypeface(string? glyph)
    {
        if (string.IsNullOrEmpty(glyph))
            return MaterialIconsTypeface;

        var cp = char.ConvertToUtf32(glyph, 0);
        if (cp >= 0xF0000)
            return MaterialDesignIconsTypeface;

        if (IsClassicMaterialIcon(cp))
            return MaterialIconsTypeface;

        return MaterialSymbolsTypeface;
    }

    /// <summary>
    /// Automatically resolves the appropriate icon <see cref="FontFamily"/> for a glyph string:
    /// classic Material Icons PUA codepoints use <see cref="MaterialIconsFont"/>,
    /// new Material Symbols Rounded codepoints use <see cref="MaterialSymbolsFont"/>,
    /// and Supplementary PUA-A codepoints (U+F0000+) use <see cref="MaterialDesignIconsFont"/>.
    /// </summary>
    public static FontFamily ResolveIconFont(string? glyph)
    {
        if (string.IsNullOrEmpty(glyph))
            return MaterialIconsFont;

        var cp = char.ConvertToUtf32(glyph, 0);
        if (cp >= 0xF0000)
            return MaterialDesignIconsFont;

        if (IsClassicMaterialIcon(cp))
            return MaterialIconsFont;

        return MaterialSymbolsFont;
    }

    private static bool IsClassicMaterialIcon(int cp)
    {
        if ((uint)(cp - 0xE000) >= 0x1900u)
            return false;

        int offset = cp - 0xE000;
        return (ClassicMaterialIconsMask[offset >> 5] & (1u << (offset & 31))) != 0;
    }

    // Exact 200-uint bitmask of all 2,225 PUA codepoints (U+E000..U+F8FF) present in MaterialIcons-Regular.ttf
    private static ReadOnlySpan<uint> ClassicMaterialIconsMask =>
    [
        0xFC9FFFFFu, 0x7FFFDFBFu, 0xFFFF7FFEu, 0x7FDFFFFFu, 0x01FFFFBEu, 0xBFFFFFFFu, 0xFFFFFFFFu, 0x07FFFFFFu,
        0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFBFFFFFu, 0x0F7FFFFFu, 0xFFBFFFFFu, 0x7BBFFEFFu, 0x0000FFDFu, 0x00FE7FFCu,
        0xFFFFFFFFu, 0x03FFFFFFu, 0xFFEFFFFFu, 0xFFFFFFEFu, 0xFFFFFFFFu, 0x1FFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu,
        0x3FFFFFFFu, 0x0001FFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x01FFFFFFu, 0xBFFFFFFFu, 0xFFFFFFFFu,
        0xF80FFFFFu, 0xFFFFFFFFu, 0x1FBFFFFFu, 0xFFFFFFFFu, 0xDFFFFFFFu, 0xFFFFBFEFu, 0x3FFFFFFFu, 0xFFFFFFFFu,
        0x9FFFFFFFu, 0x1FFEFFFFu, 0x3F3FFFFFu, 0xFFEFFFFEu, 0x007FFFFFu, 0x7FFF8000u, 0xFFFFFFFFu, 0x7FFFFFFFu,
        0xFFFFFFFFu, 0x01FCFFFFu, 0xFFF801FFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x000007FFu, 0xFFFF0000u, 0xFFEFFFFFu,
        0x0FFFFFFFu, 0x00FE0000u, 0xFFFFF800u, 0xFFFFFDFFu, 0xFFFFFFFFu, 0x7FFFEFFFu, 0xFFEFFFFFu, 0x3FFFFFFFu,
        0xFFFFFFFFu, 0xDFFFFFFFu, 0xFFFFFFFFu, 0x1FFFFFFFu, 0xFFFFFFFFu, 0xFBFFFFFFu, 0x7FFFFFFFu, 0xF0FFFFFFu,
        0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x9FFFFFFFu, 0x7FFFDFFFu, 0xBFFFFFFFu, 0xFDFFFFFFu, 0xBFFFFFFFu,
        0xFFFFCFFFu, 0xFFFFFFFFu, 0xFEFFFFFFu, 0x0FFFFFFFu, 0xFFFFFFFFu, 0x3FFFFFFFu, 0x0FF80000u, 0x00000000u,
        0xFFFFC000u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x000007FFu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x3FFFFFFFu, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x000FFFFFu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x80000000u,
        0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu,
        0x00FFFFFFu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u,
        0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x80000000u,
    ];

    public static readonly Uri FontCollectionKey = new("fonts:MavFonts", UriKind.Absolute);
    public static readonly Uri FontCollectionSource = new("avares://MavUI/Resources/Fonts", UriKind.Absolute);
}
