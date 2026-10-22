using Avalonia.Media;

namespace Mav.Theming;

/// <summary>
/// Global multiplier for Material Design 3 corner radii.
/// </summary>
public enum MavShapeScale
{
    Compact,
    Standard,
    Expressive,
}

/// <summary>
/// Material Design 3 shape, elevation, spacing, and state-layer opacity tokens.
/// </summary>
public sealed class MavShapeTokens
{
    public static MavShapeTokens Default { get; } = new();

    public MavShapeScale Scale { get; }

    /// <summary>Creates the shape tokens for the given <paramref name="scale"/>.</summary>
    public MavShapeTokens(MavShapeScale scale = MavShapeScale.Standard)
    {
        Scale = scale;
    }

    public double ScaleMultiplier => Scale switch
    {
        MavShapeScale.Compact => 0.72,
        MavShapeScale.Expressive => 1.28,
        _ => 1.0,
    };

    /// <summary>Scales <paramref name="baseRadius"/> to the current shape scale.</summary>
    public double ScaleRadius(double baseRadius) =>
        baseRadius <= 0 || baseRadius >= 500 ? baseRadius : Math.Round(baseRadius * ScaleMultiplier, 1);

    public double None => 0;
    public double ExtraSmall => ScaleRadius(4);
    public double Small => ScaleRadius(8);
    public double Medium => ScaleRadius(12);
    public double Large => ScaleRadius(16);
    public double LargeIncreased => ScaleRadius(20);
    public double ExtraLarge => ScaleRadius(28);
    public double Full => 999;
}

/// <summary>Shadow elevation levels from the Material 3 token set.</summary>
public sealed class MavElevationTokens
{
    public static MavElevationTokens Default { get; } = new();

    public double Level0 => 0;
    public double Level1 => 1;
    public double Level2 => 3;
    public double Level3 => 6;
    public double Level4 => 8;
    public double Level5 => 12;

    /// <summary>
    /// Builds the Material Design 3 two-layer (key + ambient) <see cref="BoxShadows"/> for the given elevation level (0..5).
    /// </summary>
    public static BoxShadows ToBoxShadows(double level, Color? shadowColor = null)
    {
        if (level <= 0)
            return default;

        var baseColor = shadowColor ?? MavTheme.Current.Color.Shadow;
        var clamped = Math.Clamp(level, 0.5, 5.0);

        var (keyY, keyBlur, keyAlpha, ambY, ambBlur, ambSpread, ambAlpha) = clamped switch
        {
            <= 1.2 => (1.0, 2.0, 0.24f, 1.0, 3.0, 1.0, 0.12f),
            <= 2.2 => (1.0, 2.5, 0.26f, 2.0, 6.0, 2.0, 0.14f),
            <= 3.2 => (1.5, 3.0, 0.28f, 4.0, 8.0, 3.0, 0.14f),
            <= 4.2 => (2.0, 3.5, 0.28f, 6.0, 10.0, 4.0, 0.15f),
            _      => (4.0, 4.0, 0.30f, 8.0, 12.0, 6.0, 0.15f),
        };

        var keyShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = keyY,
            Blur = keyBlur,
            Spread = 0,
            Color = baseColor.WithAlpha(keyAlpha),
        };

        var ambientShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = ambY,
            Blur = ambBlur,
            Spread = ambSpread,
            Color = baseColor.WithAlpha(ambAlpha),
        };

        return new BoxShadows(keyShadow, [ambientShadow]);
    }

    /// <summary>
    /// Blends the Material Design 3 surface-tint overlay for tonal elevation (0%..14%).
    /// </summary>
    public static Color ApplySurfaceTint(Color surface, Color surfaceTint, double level)
    {
        if (level <= 0)
            return surface;

        var opacity = level switch
        {
            <= 1.0 => 0.05f,
            <= 2.0 => 0.08f,
            <= 3.0 => 0.11f,
            <= 4.0 => 0.12f,
            _      => 0.14f,
        };

        var inv = 1f - opacity;
        var r = (byte)Math.Clamp((int)Math.Round(surface.R * inv + surfaceTint.R * opacity), 0, 255);
        var g = (byte)Math.Clamp((int)Math.Round(surface.G * inv + surfaceTint.G * opacity), 0, 255);
        var b = (byte)Math.Clamp((int)Math.Round(surface.B * inv + surfaceTint.B * opacity), 0, 255);
        return Color.FromArgb(surface.A, r, g, b);
    }
}

/// <summary>Opacity values for hover, focus, pressed and dragged state layers.</summary>
public sealed class MavStateLayerTokens
{
    public static MavStateLayerTokens Default { get; } = new();

    public float Hover => 0.08f;
    public float Focus => 0.10f;
    public float Pressed => 0.12f;
    public float Dragged => 0.16f;
    public float DisabledContainer => 0.12f;
    public float DisabledContent => 0.38f;
}

/// <summary>Spacing density of the theme: default, comfortable or compact.</summary>
public enum MavDensity
{
    Comfortable = 0,
    Compact = -1,
    Dense = -2,
}
