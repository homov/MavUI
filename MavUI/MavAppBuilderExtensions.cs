using Avalonia;
using Avalonia.Media.Fonts;

namespace Mav;

/// <summary>Avalonia application setup for MavUI.</summary>
public static class MavAppBuilderExtensions
{
    /// <summary>
    /// Registers embedded Material Icons, Material Symbols Rounded, Material Design Icons,
    /// and Roboto fonts with the Avalonia <see cref="AppBuilder"/>, and optionally configures <see cref="Theming.MavTheme"/>.
    /// </summary>
    public static AppBuilder UseMav(this AppBuilder builder, Action<Theming.MavThemeOptions>? configure = null)
    {
        Platform.MavOverlay.Initialize();
        builder.ConfigureFonts(fontManager =>
        {
            fontManager.AddFontCollection(new EmbeddedFontCollection(
                MavFonts.FontCollectionKey,
                MavFonts.FontCollectionSource));
        });

        if (configure is not null)
        {
            builder.AfterSetup(_ => Theming.MavTheme.Configure(configure));
        }

        return builder;
    }

    /// <summary>Registers the fonts and applies the theme options given as a value.</summary>
    public static AppBuilder UseMav(this AppBuilder builder, Theming.MavThemeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return builder.UseMav(opts =>
        {
            opts.SeedColor = options.SeedColor;
            opts.UseSystemAccentColor = options.UseSystemAccentColor;
            opts.Mode = options.Mode;
            opts.SchemeVariant = options.SchemeVariant;
            opts.Contrast = options.Contrast;
            opts.Density = options.Density;
            opts.Motion = options.Motion;
            opts.ShapeScale = options.ShapeScale;
            opts.CustomLightScheme = options.CustomLightScheme;
            opts.CustomDarkScheme = options.CustomDarkScheme;
        });
    }
}
