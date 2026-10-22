using Android.OS;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Md3.Demo.Android;

public sealed class MobileApp : global::Avalonia.Application
{
    public override void Initialize()
    {
        // Md3 controls provide their own templates; no Avalonia theme is required.
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = CreateRoot;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = CreateRoot();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Control CreateRoot()
    {
        var navigation = new Md3.Avalonia.Controls.MdNavigationPage(new HomePage());
        // MdNavigationPage distributes safe-area insets itself.
        TopLevel.SetAutoSafeAreaPadding(navigation, false);
        // Edge-to-edge is an Android 15+ (API 35) feature: on older releases the
        // transparent bars would expose the wallpaper behind the reserved strip.
        // There we keep the platform layout and paint the bars with the theme surface.
        var edgeToEdge = (int)Build.VERSION.SdkInt >= 35;
        Md3.Avalonia.Platform.MdSystemBars.Attach(navigation, edgeToEdge: edgeToEdge);
        return navigation;
    }
}
