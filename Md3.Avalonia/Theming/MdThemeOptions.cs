using Avalonia.Media;

namespace Md3.Avalonia.Theming;

/// <summary>
/// Configuration options for <see cref="MdTheme"/>.
/// </summary>
public sealed class MdThemeOptions
{
    public Color SeedColor { get; set; } = MdColors.Purple;
    public bool UseSystemAccentColor { get; set; }
    public MdThemeMode Mode { get; set; } = MdThemeMode.System;
    public MdSchemeVariant SchemeVariant { get; set; } = MdSchemeVariant.TonalSpot;
    public MdContrast Contrast { get; set; } = MdContrast.Standard;
    public MdDensity Density { get; set; } = MdDensity.Comfortable;
    public MdMotionScheme Motion { get; set; } = MdMotionScheme.Expressive;
    public MdShapeScale ShapeScale { get; set; } = MdShapeScale.Standard;
    public MdColorScheme? CustomLightScheme { get; set; }
    public MdColorScheme? CustomDarkScheme { get; set; }

    public void Apply() => MdTheme.Configure(this);
}
