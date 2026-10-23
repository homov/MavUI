using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Mav.Theming;

[assembly: AvaloniaTestApplication(typeof(Mav.VisualTests.VisualAppBuilder))]

namespace Mav.VisualTests;

/// <summary>
/// The real ControlsGallery application on the headless platform with Skia, so
/// <c>CaptureRenderedFrame</c> returns the drawn screen instead of an empty stub frame.
/// </summary>
public static class VisualAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<global::Mav.ControlsGallery.App>()
            .UseMav(options =>
            {
                options.SeedColor = MavColors.Indigo;
                options.Mode = MavThemeMode.Light;
            })
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
