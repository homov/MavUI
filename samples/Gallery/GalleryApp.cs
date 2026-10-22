using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Mav.Gallery;

public class GalleryApp : global::Avalonia.Application
{
    public override void Initialize()
    {
        // Mav controls provide their own templates; no Avalonia theme is required.
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = GalleryShell.Create();

        base.OnFrameworkInitializationCompleted();
    }
}
