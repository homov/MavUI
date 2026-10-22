using Avalonia.Media;

namespace Mav.Theming;

/// <summary>
/// Configuration options for <see cref="MavTheme"/>.
/// </summary>
public sealed class MavThemeOptions
{
    public Color SeedColor { get; set; } = MavColors.Purple;
    public bool UseSystemAccentColor { get; set; }
    public MavThemeMode Mode { get; set; } = MavThemeMode.System;
    public MavSchemeVariant SchemeVariant { get; set; } = MavSchemeVariant.TonalSpot;
    public MavContrast Contrast { get; set; } = MavContrast.Standard;
    public MavDensity Density { get; set; } = MavDensity.Comfortable;
    public MavMotionScheme Motion { get; set; } = MavMotionScheme.Expressive;
    public MavShapeScale ShapeScale { get; set; } = MavShapeScale.Standard;
    public MavColorScheme? CustomLightScheme { get; set; }
    public MavColorScheme? CustomDarkScheme { get; set; }

    /// <summary>
    /// Applies these options to the current theme.
    /// </summary>
    public void Apply() => MavTheme.Configure(this);
}
