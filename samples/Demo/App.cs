using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Mav.Demo;

public sealed class App : Application
{
    public override void Initialize()
    {
        // Mav controls provide their own templates; no Avalonia theme is required.
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
