using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Hc;
using Mav.Essentials;

[assembly: AvaloniaTestApplication(typeof(Hc.Tests.Ui.HcTestAppBuilder))]

namespace Hc.Tests.Ui;

/// <summary>The real HC application on the headless platform: no display is needed.</summary>
public static class HcTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHcDefaults()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// Keeps the tests away from the machine's real data and from the production cloud.
/// - Preferences and secure storage are replaced before the app reads them: the runner's own sign-in must not
///   leak into the tests (autologin, shown email).
/// - The cloud address points at a closed local port, so a test that forgets its fake server fails fast.
/// HC_API_BASE can still be set explicitly.
/// </summary>
internal static class TestEnvironment
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        var folder = Path.Combine(Path.GetTempPath(), "hc-ui-tests-" + Guid.NewGuid().ToString("N"));
        MavPreferences.SetDefault(new JsonFilePreferences(folder));
        MavSecureStorage.SetDefault(new FakeSecureStorage());

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HC_API_BASE")))
            Environment.SetEnvironmentVariable("HC_API_BASE", "http://127.0.0.1:9/stinexapiv1");
    }
}
