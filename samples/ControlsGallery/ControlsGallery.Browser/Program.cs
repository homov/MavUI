using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Browser;
using Mav;
using Mav.Theming;

namespace Mav.ControlsGallery.Browser;

internal static class Program
{
    private static Task Main(string[] args)
    {
        // The WASM runtime may start with an invariant/English culture regardless of the visitor's
        // browser. Read the browser's first preferred language before App creates its first page;
        // GalleryStartup remembers this as "System". Unsupported UI languages still use the
        // English text fallback, but retain the visitor's region for dates and numbers.
        using var navigator = JSHost.GlobalThis.GetPropertyAsJSObject("navigator");
        var preferred = navigator?.GetPropertyAsString("language");
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(preferred.Replace('_', '-'));
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.CurrentCulture = culture;
            }
            catch (CultureNotFoundException)
            {
                // A browser extension may supply a non-standard tag. Keep the runtime culture.
            }
        }

        return BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<ControlsGallery.App>()
            .UseBrowser()
            .UseMav(options =>
            {
                options.SeedColor = MavColors.Indigo;
                options.Mode = MavThemeMode.System;
            })
            .LogToTrace();
}
