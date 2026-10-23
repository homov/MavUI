using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Mav.ControlsGalleryMarkup;

/// <summary>Shared application for the markup gallery. The desktop head configures the builder and theme.</summary>
public class GalleryApp : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
