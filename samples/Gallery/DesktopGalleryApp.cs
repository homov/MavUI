using Avalonia.Controls.ApplicationLifetimes;

namespace Md3.Gallery;

public sealed class DesktopGalleryApp : GalleryApp
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new GalleryWindow();

        base.OnFrameworkInitializationCompleted();
    }
}
