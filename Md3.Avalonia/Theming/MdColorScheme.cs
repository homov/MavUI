using Avalonia.Media;
using Md3.Avalonia.ColorUtilities.ColorAppearance;
using Md3.Avalonia.ColorUtilities.Palettes;
using PaletteStyle = Md3.Avalonia.ColorUtilities.Palettes.Style;

namespace Md3.Avalonia.Theming;

/// <summary>
/// Controls how a dynamic Material Design 3 color scheme is derived from a seed color.
/// </summary>
public enum MdSchemeVariant
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

/// <summary>
/// Semantic Material Design 3 color roles.
/// </summary>
public enum MdColorRole
{
    Primary,
    OnPrimary,
    PrimaryContainer,
    OnPrimaryContainer,
    PrimaryFixed,
    PrimaryFixedDim,
    OnPrimaryFixed,
    OnPrimaryFixedVariant,
    Secondary,
    OnSecondary,
    SecondaryContainer,
    OnSecondaryContainer,
    SecondaryFixed,
    SecondaryFixedDim,
    OnSecondaryFixed,
    OnSecondaryFixedVariant,
    Tertiary,
    OnTertiary,
    TertiaryContainer,
    OnTertiaryContainer,
    TertiaryFixed,
    TertiaryFixedDim,
    OnTertiaryFixed,
    OnTertiaryFixedVariant,
    Error,
    OnError,
    ErrorContainer,
    OnErrorContainer,
    Background,
    OnBackground,
    Surface,
    OnSurface,
    SurfaceVariant,
    OnSurfaceVariant,
    SurfaceDim,
    SurfaceBright,
    SurfaceContainerLowest,
    SurfaceContainerLow,
    SurfaceContainer,
    SurfaceContainerHigh,
    SurfaceContainerHighest,
    Outline,
    OutlineVariant,
    Shadow,
    Scrim,
    InverseSurface,
    InverseOnSurface,
    InversePrimary,
    SurfaceTint,
}

/// <summary>
/// Full Material Design 3 color role set, including surface container tiers
/// and fixed accent roles. Generated from a seed color via HCT tonal palettes.
/// </summary>
public sealed record MdColorScheme
{
    public required bool IsDark { get; init; }
    public required Color Seed { get; init; }
    public MdSchemeVariant Variant { get; init; } = MdSchemeVariant.TonalSpot;

    public required Color Primary { get; init; }
    public required Color OnPrimary { get; init; }
    public required Color PrimaryContainer { get; init; }
    public required Color OnPrimaryContainer { get; init; }
    public required Color PrimaryFixed { get; init; }
    public required Color PrimaryFixedDim { get; init; }
    public required Color OnPrimaryFixed { get; init; }
    public required Color OnPrimaryFixedVariant { get; init; }

    public required Color Secondary { get; init; }
    public required Color OnSecondary { get; init; }
    public required Color SecondaryContainer { get; init; }
    public required Color OnSecondaryContainer { get; init; }
    public required Color SecondaryFixed { get; init; }
    public required Color SecondaryFixedDim { get; init; }
    public required Color OnSecondaryFixed { get; init; }
    public required Color OnSecondaryFixedVariant { get; init; }

    public required Color Tertiary { get; init; }
    public required Color OnTertiary { get; init; }
    public required Color TertiaryContainer { get; init; }
    public required Color OnTertiaryContainer { get; init; }
    public required Color TertiaryFixed { get; init; }
    public required Color TertiaryFixedDim { get; init; }
    public required Color OnTertiaryFixed { get; init; }
    public required Color OnTertiaryFixedVariant { get; init; }

    public required Color Error { get; init; }
    public required Color OnError { get; init; }
    public required Color ErrorContainer { get; init; }
    public required Color OnErrorContainer { get; init; }

    public required Color Background { get; init; }
    public required Color OnBackground { get; init; }
    public required Color Surface { get; init; }
    public required Color OnSurface { get; init; }
    public required Color SurfaceVariant { get; init; }
    public required Color OnSurfaceVariant { get; init; }

    public required Color SurfaceDim { get; init; }
    public required Color SurfaceBright { get; init; }
    public required Color SurfaceContainerLowest { get; init; }
    public required Color SurfaceContainerLow { get; init; }
    public required Color SurfaceContainer { get; init; }
    public required Color SurfaceContainerHigh { get; init; }
    public required Color SurfaceContainerHighest { get; init; }

    public required Color Outline { get; init; }
    public required Color OutlineVariant { get; init; }
    public required Color Shadow { get; init; }
    public required Color Scrim { get; init; }
    public required Color InverseSurface { get; init; }
    public required Color InverseOnSurface { get; init; }
    public required Color InversePrimary { get; init; }
    public required Color SurfaceTint { get; init; }

    public static MdColorScheme FromSeed(
        Color seed,
        bool dark,
        MdContrast contrast = MdContrast.Standard,
        MdSchemeVariant variant = MdSchemeVariant.TonalSpot)
    {
        var argb = ToArgb(seed);
        var core = CorePalette.Of(argb, ToPaletteStyle(variant));
        if (variant == MdSchemeVariant.Monochrome)
        {
            var hue = Hct.FromInt(argb).Hue;
            core.Primary = TonalPalette.FromHueAndChroma(hue, 0);
            core.Secondary = TonalPalette.FromHueAndChroma(hue, 0);
            core.Tertiary = TonalPalette.FromHueAndChroma(hue, 0);
            core.Neutral = TonalPalette.FromHueAndChroma(hue, 0);
            core.NeutralVariant = TonalPalette.FromHueAndChroma(hue, 0);
        }
        else if (variant == MdSchemeVariant.Fidelity)
        {
            var hct = Hct.FromInt(argb);
            core.Primary = TonalPalette.FromHueAndChroma(hct.Hue, Math.Max(32, hct.Chroma));
            core.Secondary = TonalPalette.FromHueAndChroma(hct.Hue, Math.Max(16, hct.Chroma * 0.36));
            core.Tertiary = TonalPalette.FromHueAndChroma((hct.Hue + 60.0) % 360.0, Math.Max(24, hct.Chroma * 0.52));
        }

        return FromCorePalette(seed, core, dark, contrast, variant);
    }

    private static PaletteStyle ToPaletteStyle(MdSchemeVariant variant) => variant switch
    {
        MdSchemeVariant.Neutral => PaletteStyle.Spritz,
        MdSchemeVariant.Vibrant => PaletteStyle.Vibrant,
        MdSchemeVariant.Expressive => PaletteStyle.Expressive,
        MdSchemeVariant.Monochrome => PaletteStyle.Spritz,
        MdSchemeVariant.Fidelity => PaletteStyle.Content,
        MdSchemeVariant.Content => PaletteStyle.Content,
        MdSchemeVariant.Rainbow => PaletteStyle.Rainbow,
        MdSchemeVariant.FruitSalad => PaletteStyle.FruitSalad,
        _ => PaletteStyle.TonalSpot,
    };

    internal static MdColorScheme FromCorePalette(
        Color seed,
        CorePalette core,
        bool dark,
        MdContrast contrast = MdContrast.Standard,
        MdSchemeVariant variant = MdSchemeVariant.TonalSpot)
    {
        TonalPalette p = core.Primary;
        TonalPalette s = core.Secondary;
        TonalPalette t = core.Tertiary;
        TonalPalette n = core.Neutral;
        TonalPalette nv = core.NeutralVariant;
        TonalPalette e = core.Error;

        double delta = contrast switch
        {
            MdContrast.Medium => 6.0,
            MdContrast.High => 12.0,
            _ => 0.0
        };

        Color Tone(TonalPalette pal, double tone) =>
            FromArgb(pal.Tone((uint)Math.Clamp((int)Math.Round(tone), 0, 100)));

        if (!dark)
        {
            return new MdColorScheme
            {
                IsDark = false,
                Seed = seed,
                Variant = variant,

                Primary = Tone(p, 40 - delta),
                OnPrimary = Tone(p, 100),
                PrimaryContainer = Tone(p, 90 - delta * 0.5),
                OnPrimaryContainer = Tone(p, Math.Max(0, 10 - delta * 0.5)),
                PrimaryFixed = Tone(p, 90),
                PrimaryFixedDim = Tone(p, 80),
                OnPrimaryFixed = Tone(p, 10),
                OnPrimaryFixedVariant = Tone(p, 30),

                Secondary = Tone(s, 40 - delta),
                OnSecondary = Tone(s, 100),
                SecondaryContainer = Tone(s, 90 - delta * 0.5),
                OnSecondaryContainer = Tone(s, Math.Max(0, 10 - delta * 0.5)),
                SecondaryFixed = Tone(s, 90),
                SecondaryFixedDim = Tone(s, 80),
                OnSecondaryFixed = Tone(s, 10),
                OnSecondaryFixedVariant = Tone(s, 30),

                Tertiary = Tone(t, 40 - delta),
                OnTertiary = Tone(t, 100),
                TertiaryContainer = Tone(t, 90 - delta * 0.5),
                OnTertiaryContainer = Tone(t, Math.Max(0, 10 - delta * 0.5)),
                TertiaryFixed = Tone(t, 90),
                TertiaryFixedDim = Tone(t, 80),
                OnTertiaryFixed = Tone(t, 10),
                OnTertiaryFixedVariant = Tone(t, 30),

                Error = Tone(e, 40 - delta),
                OnError = Tone(e, 100),
                ErrorContainer = Tone(e, 90),
                OnErrorContainer = Tone(e, 10),

                Background = Tone(n, 98),
                OnBackground = Tone(n, 10),
                Surface = Tone(n, 98),
                OnSurface = Tone(n, Math.Max(0, 10 - delta * 0.5)),
                SurfaceVariant = Tone(nv, 90),
                OnSurfaceVariant = Tone(nv, Math.Max(0, 30 - delta)),

                SurfaceDim = Tone(n, 87),
                SurfaceBright = Tone(n, 98),
                SurfaceContainerLowest = Tone(n, 100),
                SurfaceContainerLow = Tone(n, 96),
                SurfaceContainer = Tone(n, 94),
                SurfaceContainerHigh = Tone(n, 92),
                SurfaceContainerHighest = Tone(n, 90),

                Outline = Tone(nv, Math.Max(0, 50 - delta * 1.5)),
                OutlineVariant = Tone(nv, Math.Max(0, 80 - delta * 1.5)),
                Shadow = Tone(n, 0),
                Scrim = Tone(n, 0),
                InverseSurface = Tone(n, 20),
                InverseOnSurface = Tone(n, 95),
                InversePrimary = Tone(p, 80),
                SurfaceTint = Tone(p, 40),
            };
        }

        return new MdColorScheme
        {
            IsDark = true,
            Seed = seed,
            Variant = variant,

            Primary = Tone(p, 80 + delta * 0.8),
            OnPrimary = Tone(p, Math.Max(0, 20 - delta)),
            PrimaryContainer = Tone(p, 30 + delta * 0.5),
            OnPrimaryContainer = Tone(p, Math.Min(100, 90 + delta * 0.5)),
            PrimaryFixed = Tone(p, 90),
            PrimaryFixedDim = Tone(p, 80),
            OnPrimaryFixed = Tone(p, 10),
            OnPrimaryFixedVariant = Tone(p, 30),

            Secondary = Tone(s, 80 + delta * 0.8),
            OnSecondary = Tone(s, Math.Max(0, 20 - delta)),
            SecondaryContainer = Tone(s, 30 + delta * 0.5),
            OnSecondaryContainer = Tone(s, Math.Min(100, 90 + delta * 0.5)),
            SecondaryFixed = Tone(s, 90),
            SecondaryFixedDim = Tone(s, 80),
            OnSecondaryFixed = Tone(s, 10),
            OnSecondaryFixedVariant = Tone(s, 30),

            Tertiary = Tone(t, 80 + delta * 0.8),
            OnTertiary = Tone(t, Math.Max(0, 20 - delta)),
            TertiaryContainer = Tone(t, 30 + delta * 0.5),
            OnTertiaryContainer = Tone(t, Math.Min(100, 90 + delta * 0.5)),
            TertiaryFixed = Tone(t, 90),
            TertiaryFixedDim = Tone(t, 80),
            OnTertiaryFixed = Tone(t, 10),
            OnTertiaryFixedVariant = Tone(t, 30),

            Error = Tone(e, 80 + delta * 0.5),
            OnError = Tone(e, 20),
            ErrorContainer = Tone(e, 30),
            OnErrorContainer = Tone(e, 90),

            Background = Tone(n, 6),
            OnBackground = Tone(n, 90),
            Surface = Tone(n, 6),
            OnSurface = Tone(n, Math.Min(100, 90 + delta * 0.5)),
            SurfaceVariant = Tone(nv, 30),
            OnSurfaceVariant = Tone(nv, Math.Min(100, 80 + delta)),

            SurfaceDim = Tone(n, 6),
            SurfaceBright = Tone(n, 24),
            SurfaceContainerLowest = Tone(n, 4),
            SurfaceContainerLow = Tone(n, 10),
            SurfaceContainer = Tone(n, 12),
            SurfaceContainerHigh = Tone(n, 17),
            SurfaceContainerHighest = Tone(n, 22),

            Outline = Tone(nv, Math.Min(100, 60 + delta * 1.5)),
            OutlineVariant = Tone(nv, Math.Min(100, 30 + delta * 1.5)),
            Shadow = Tone(n, 0),
            Scrim = Tone(n, 0),
            InverseSurface = Tone(n, 90),
            InverseOnSurface = Tone(n, 20),
            InversePrimary = Tone(p, 40),
            SurfaceTint = Tone(p, 80),
        };
    }

    public Color Get(MdColorRole role) => role switch
    {
        MdColorRole.Primary => Primary,
        MdColorRole.OnPrimary => OnPrimary,
        MdColorRole.PrimaryContainer => PrimaryContainer,
        MdColorRole.OnPrimaryContainer => OnPrimaryContainer,
        MdColorRole.PrimaryFixed => PrimaryFixed,
        MdColorRole.PrimaryFixedDim => PrimaryFixedDim,
        MdColorRole.OnPrimaryFixed => OnPrimaryFixed,
        MdColorRole.OnPrimaryFixedVariant => OnPrimaryFixedVariant,
        MdColorRole.Secondary => Secondary,
        MdColorRole.OnSecondary => OnSecondary,
        MdColorRole.SecondaryContainer => SecondaryContainer,
        MdColorRole.OnSecondaryContainer => OnSecondaryContainer,
        MdColorRole.SecondaryFixed => SecondaryFixed,
        MdColorRole.SecondaryFixedDim => SecondaryFixedDim,
        MdColorRole.OnSecondaryFixed => OnSecondaryFixed,
        MdColorRole.OnSecondaryFixedVariant => OnSecondaryFixedVariant,
        MdColorRole.Tertiary => Tertiary,
        MdColorRole.OnTertiary => OnTertiary,
        MdColorRole.TertiaryContainer => TertiaryContainer,
        MdColorRole.OnTertiaryContainer => OnTertiaryContainer,
        MdColorRole.TertiaryFixed => TertiaryFixed,
        MdColorRole.TertiaryFixedDim => TertiaryFixedDim,
        MdColorRole.OnTertiaryFixed => OnTertiaryFixed,
        MdColorRole.OnTertiaryFixedVariant => OnTertiaryFixedVariant,
        MdColorRole.Error => Error,
        MdColorRole.OnError => OnError,
        MdColorRole.ErrorContainer => ErrorContainer,
        MdColorRole.OnErrorContainer => OnErrorContainer,
        MdColorRole.Background => Background,
        MdColorRole.OnBackground => OnBackground,
        MdColorRole.Surface => Surface,
        MdColorRole.OnSurface => OnSurface,
        MdColorRole.SurfaceVariant => SurfaceVariant,
        MdColorRole.OnSurfaceVariant => OnSurfaceVariant,
        MdColorRole.SurfaceDim => SurfaceDim,
        MdColorRole.SurfaceBright => SurfaceBright,
        MdColorRole.SurfaceContainerLowest => SurfaceContainerLowest,
        MdColorRole.SurfaceContainerLow => SurfaceContainerLow,
        MdColorRole.SurfaceContainer => SurfaceContainer,
        MdColorRole.SurfaceContainerHigh => SurfaceContainerHigh,
        MdColorRole.SurfaceContainerHighest => SurfaceContainerHighest,
        MdColorRole.Outline => Outline,
        MdColorRole.OutlineVariant => OutlineVariant,
        MdColorRole.Shadow => Shadow,
        MdColorRole.Scrim => Scrim,
        MdColorRole.InverseSurface => InverseSurface,
        MdColorRole.InverseOnSurface => InverseOnSurface,
        MdColorRole.InversePrimary => InversePrimary,
        MdColorRole.SurfaceTint => SurfaceTint,
        _ => OnSurface,
    };

    public Color this[MdColorRole role] => Get(role);

    public IReadOnlyDictionary<MdColorRole, Color> ToDictionary()
    {
        var dict = new Dictionary<MdColorRole, Color>(42);
        foreach (var role in Enum.GetValues<MdColorRole>())
            dict[role] = Get(role);
        return dict;
    }

    public Color SurfaceAtElevation(int level) => level switch
    {
        <= 0 => Surface,
        1 => SurfaceContainerLow,
        2 => SurfaceContainer,
        3 => SurfaceContainerHigh,
        4 => Blend(SurfaceContainerHigh, SurfaceContainerHighest, 0.5),
        _ => SurfaceContainerHighest,
    };

    public static Color Blend(Color from, Color to, double amount)
    {
        var t = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (byte)Math.Round(from.A + (to.A - from.A) * t),
            (byte)Math.Round(from.R + (to.R - from.R) * t),
            (byte)Math.Round(from.G + (to.G - from.G) * t),
            (byte)Math.Round(from.B + (to.B - from.B) * t));
    }

    public static Color FromArgb(uint argb)
    {
        var a = (byte)((argb >> 24) & 0xFF);
        var r = (byte)((argb >> 16) & 0xFF);
        var g = (byte)((argb >> 8) & 0xFF);
        var b = (byte)(argb & 0xFF);
        return Color.FromArgb(a, r, g, b);
    }

    public static uint ToArgb(Color c) =>
        ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;

    /// <summary>
    /// Extracts the most suitable Material You seed color from raw ARGB pixels using HCT hue-chroma scoring.
    /// </summary>
    public static Color ExtractSeedFromPixels(ReadOnlySpan<uint> argbPixels, Color? fallback = null)
    {
        var defaultColor = fallback ?? MdColors.Purple;
        if (argbPixels.IsEmpty)
            return defaultColor;

        const int bins = 36;
        Span<int> counts = stackalloc int[bins];
        Span<double> chromaSum = stackalloc double[bins];
        Span<uint> bestPixel = stackalloc uint[bins];
        Span<double> bestChroma = stackalloc double[bins];

        var step = Math.Max(1, argbPixels.Length / 4096);
        var validCount = 0;

        for (int i = 0; i < argbPixels.Length; i += step)
        {
            var argb = argbPixels[i];
            var a = (argb >> 24) & 0xFF;
            if (a < 128)
                continue;

            var hct = Hct.FromInt(argb | 0xFF000000u);
            if (hct.Chroma < 12.0 || hct.Tone < 10.0 || hct.Tone > 95.0)
                continue;

            var bin = Math.Clamp((int)(hct.Hue / 10.0), 0, bins - 1);
            counts[bin]++;
            chromaSum[bin] += hct.Chroma;
            validCount++;

            if (hct.Chroma > bestChroma[bin])
            {
                bestChroma[bin] = hct.Chroma;
                bestPixel[bin] = argb | 0xFF000000u;
            }
        }

        if (validCount == 0)
            return defaultColor;

        double topScore = double.NegativeInfinity;
        uint winner = ToArgb(defaultColor);

        for (int b = 0; b < bins; b++)
        {
            if (counts[b] == 0)
                continue;

            // Smooth population across neighboring hue bins
            double smoothedPop = counts[b] * 3.0
                + counts[(b - 1 + bins) % bins] * 2.0
                + counts[(b + 1) % bins] * 2.0
                + counts[(b - 2 + bins) % bins]
                + counts[(b + 2) % bins];

            var popShare = smoothedPop / (validCount * 9.0);
            var avgChroma = chromaSum[b] / counts[b];
            var chromaScore = (avgChroma - 48.0) * (avgChroma >= 48.0 ? 0.3 : 0.15);
            var score = popShare * 70.0 + chromaScore;

            if (score > topScore)
            {
                topScore = score;
                winner = bestPixel[b];
            }
        }

        return FromArgb(winner);
    }

    /// <summary>
    /// Extracts the most suitable Material You seed color from raw RGBA byte data (4 bytes per pixel).
    /// </summary>
    public static Color ExtractSeedFromRgba(ReadOnlySpan<byte> rgbaBytes, Color? fallback = null)
    {
        var pixelCount = rgbaBytes.Length / 4;
        if (pixelCount <= 0)
            return fallback ?? MdColors.Purple;

        var step = Math.Max(1, pixelCount / 4096);
        var sampled = new List<uint>(Math.Min(4096, pixelCount));
        for (int i = 0; i < pixelCount; i += step)
        {
            var idx = i * 4;
            var r = rgbaBytes[idx];
            var g = rgbaBytes[idx + 1];
            var b = rgbaBytes[idx + 2];
            var a = rgbaBytes[idx + 3];
            sampled.Add(((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b);
        }

        return ExtractSeedFromPixels( System.Runtime.InteropServices.CollectionsMarshal.AsSpan(sampled), fallback);
    }

    /// <summary>
    /// Builds a Material Design 3 color scheme directly from image ARGB pixels.
    /// </summary>
    public static MdColorScheme FromImagePixels(
        ReadOnlySpan<uint> argbPixels,
        bool dark = false,
        MdContrast contrast = MdContrast.Standard,
        MdSchemeVariant variant = MdSchemeVariant.TonalSpot)
    {
        var seed = ExtractSeedFromPixels(argbPixels);
        return FromSeed(seed, dark, contrast, variant);
    }
}

public enum MdContrast
{
    Standard,
    Medium,
    High,
}
