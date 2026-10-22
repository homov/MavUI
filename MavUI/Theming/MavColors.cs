using Avalonia.Media;

namespace Mav.Theming;

/// <summary>
/// Built-in Material Design 3 seed presets and color utilities.
/// </summary>
public static class MavColors
{
    public static readonly Color Purple = Color.FromRgb(0x67, 0x50, 0xA4);
    public static readonly Color Indigo = Color.FromRgb(0x3F, 0x51, 0xB5);
    public static readonly Color Blue = Color.FromRgb(0x0B, 0x57, 0xD0);
    public static readonly Color Teal = Color.FromRgb(0x00, 0x6A, 0x6A);
    public static readonly Color Green = Color.FromRgb(0x2E, 0x7D, 0x32);
    public static readonly Color Lime = Color.FromRgb(0x55, 0x8B, 0x2F);
    public static readonly Color Amber = Color.FromRgb(0xE6, 0x51, 0x00);
    public static readonly Color Orange = Color.FromRgb(0xBF, 0x36, 0x0C);
    public static readonly Color Red = Color.FromRgb(0xB3, 0x26, 0x1E);
    public static readonly Color Pink = Color.FromRgb(0x98, 0x40, 0x61);
    public static readonly Color Rose = Color.FromRgb(0x8E, 0x3A, 0x59);
    public static readonly Color Slate = Color.FromRgb(0x45, 0x5A, 0x64);

    public static IReadOnlyList<(string Name, Color Color)> Presets { get; } =
    [
        ("Purple", Purple),
        ("Blue", Blue),
        ("Teal", Teal),
        ("Green", Green),
        ("Amber", Amber),
        ("Red", Red),
        ("Pink", Pink),
        ("Indigo", Indigo),
        ("Slate", Slate),
    ];

    /// <summary>Returns the color with the given alpha, in the range 0 to 1.</summary>
    public static Color WithAlpha(this Color c, float alpha) =>
        Color.FromArgb((byte)Math.Clamp((int)Math.Round(alpha * 255f), 0, 255), c.R, c.G, c.B);

    /// <summary>Returns the color with the given alpha, in the range 0 to 1.</summary>
    public static Color WithAlpha(this Color c, double alpha) =>
        Color.FromArgb((byte)Math.Clamp((int)Math.Round(alpha * 255.0), 0, 255), c.R, c.G, c.B);

    /// <summary>
    /// Converts the color to a solid brush.
    /// </summary>
    public static ISolidColorBrush ToBrush(this Color c) => new SolidColorBrush(c);
}
