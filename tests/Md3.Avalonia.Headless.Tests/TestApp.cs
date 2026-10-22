using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
using Md3.Avalonia.Theming;

[assembly: AvaloniaTestApplication(typeof(Md3.Avalonia.Headless.Tests.TestAppBuilder))]

namespace Md3.Avalonia.Headless.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public class TestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        MdTheme.UseLight();
    }
}
