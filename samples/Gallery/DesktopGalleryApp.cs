using Avalonia.Controls.ApplicationLifetimes;

namespace Mav.Gallery;

public sealed class DesktopGalleryApp : GalleryApp
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new GalleryWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
