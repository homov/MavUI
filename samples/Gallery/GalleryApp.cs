using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Md3.Gallery;

public class GalleryApp : global::Avalonia.Application
{
    public override void Initialize()
    {
        // Md3 controls provide their own templates; no Avalonia theme is required.
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = GalleryShell.Create();

        base.OnFrameworkInitializationCompleted();
    }
}
