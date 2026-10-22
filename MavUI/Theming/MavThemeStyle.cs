using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Styling;
using Mav.Platform;

namespace Mav.Theming;

/// <summary>
/// Declarative Material Design 3 theme <see cref="Style"/> for <c>App.axaml</c>
/// (<c>&lt;mav:MavThemeStyle SeedColor="#6750A4" Mode="System" Contrast="Standard" /&gt;</c>).
/// </summary>
public class MavThemeStyle : Style
{
    public static readonly StyledProperty<Color> SeedColorProperty =
        AvaloniaProperty.Register<MavThemeStyle, Color>(nameof(SeedColor), MavColors.Purple);

    public static readonly StyledProperty<MavThemeMode> ModeProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavThemeMode>(nameof(Mode), MavThemeMode.System);

    public static readonly StyledProperty<MavSchemeVariant> VariantProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavSchemeVariant>(nameof(Variant), MavSchemeVariant.TonalSpot);

    public static readonly StyledProperty<MavContrast> ContrastProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavContrast>(nameof(Contrast), MavContrast.Standard);

    public static readonly StyledProperty<MavDensity> DensityProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavDensity>(nameof(Density), MavDensity.Comfortable);

    public static readonly StyledProperty<MavMotionScheme> MotionProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavMotionScheme>(nameof(Motion), MavMotionScheme.Expressive);

    public static readonly StyledProperty<MavShapeScale> ShapeScaleProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavShapeScale>(nameof(ShapeScale), MavShapeScale.Standard);

    public static readonly StyledProperty<bool> UseDynamicColorProperty =
        AvaloniaProperty.Register<MavThemeStyle, bool>(nameof(UseDynamicColor));

    static MavThemeStyle()
    {
        SeedColorProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.SeedColor = e.GetNewValue<Color>());
        ModeProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Mode = e.GetNewValue<MavThemeMode>());
        VariantProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Variant = e.GetNewValue<MavSchemeVariant>());
        ContrastProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Contrast = e.GetNewValue<MavContrast>());
        DensityProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Density = e.GetNewValue<MavDensity>());
        MotionProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Motion = e.GetNewValue<MavMotionScheme>());
        ShapeScaleProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.ShapeScaleMode = e.GetNewValue<MavShapeScale>());
        UseDynamicColorProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.UseSystemAccentColor = e.GetNewValue<bool>());
    }

    /// <summary>Creates the theme style that applies the library theme to the application.</summary>
    public MavThemeStyle()
    {
        _ = MavTheme.Current;
    }

    public Color SeedColor
    {
        get => GetValue(SeedColorProperty);
        set => SetValue(SeedColorProperty, value);
    }

    public MavThemeMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public MavSchemeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MavContrast Contrast
    {
        get => GetValue(ContrastProperty);
        set => SetValue(ContrastProperty, value);
    }

    public MavDensity Density
    {
        get => GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    public MavMotionScheme Motion
    {
        get => GetValue(MotionProperty);
        set => SetValue(MotionProperty, value);
    }

    public MavShapeScale ShapeScale
    {
        get => GetValue(ShapeScaleProperty);
        set => SetValue(ShapeScaleProperty, value);
    }

    public bool UseDynamicColor
    {
        get => GetValue(UseDynamicColorProperty);
        set => SetValue(UseDynamicColorProperty, value);
    }
}
