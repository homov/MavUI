using Android.OS;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Md3.Avalonia.Platform;

namespace Md3.Gallery.Android;

public sealed class MobileApp : Md3.Gallery.GalleryApp
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IActivityApplicationLifetime activity)
            activity.MainViewFactory = CreateRoot;
        else
            base.OnFrameworkInitializationCompleted();
    }

    private static Control CreateRoot()
    {
        var root = Md3.Gallery.GalleryShell.Create();

        // Edge-to-edge is an Android 15+ feature; on older releases the bars keep
        // the theme surface color instead of exposing the wallpaper.
        var edgeToEdge = (int)Build.VERSION.SdkInt >= 35;
        MdSystemBars.Attach(root, edgeToEdge: edgeToEdge);
        return root;
    }
}
