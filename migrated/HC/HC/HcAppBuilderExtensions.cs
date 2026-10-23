using Avalonia;
using Avalonia.Media;
using Mav;
using Mav.Theming;

namespace Hc;

public static class HcAppBuilderExtensions
{
    /// <summary>Brand seed color for the MavUI theme (primary #3E94FF from the old app).</summary>
    public static readonly Color BrandSeed = Color.Parse("#3E94FF");

    /// <summary>Common setup for every target: MavUI theme, fonts and logging.</summary>
    public static AppBuilder UseHcDefaults(this AppBuilder builder) =>
        builder
            .UseMav(options =>
            {
                options.SeedColor = BrandSeed;
                options.Mode = MavThemeMode.System;
            })
            .LogToTrace();
}
