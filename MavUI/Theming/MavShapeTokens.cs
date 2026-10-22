using Avalonia.Media;

namespace Mav.Theming;

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
