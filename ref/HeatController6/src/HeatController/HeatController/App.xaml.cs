using Xamarin.CommunityToolkit.Helpers;

namespace HeatController;

public partial class App : Application
{
    public App()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZcdnVQQ2hZUE1zXUY=");
        InitializeComponent();
        LocalizationResourceManager.Current.Init(Languages.Resources.ResourceManager);

        var settings = ContainerService.Container.Resolve<SettingService>();
        
       // settings.Language = "uk";

        var languageService = ContainerService.Container.Resolve<LanguageService>();
        var themeService = ContainerService.Container.Resolve<ThemeService>();
        
        Task.WaitAll(
            themeService.Initialize(),
            languageService.Initialize()
        );

        var page = ContainerService.Container.Resolve<LoadingPage>();
        MainPage = new NavigationPage( page );
    }

    protected override void OnStart()
    {
    }

    protected override void OnSleep()
    {
    }

    protected override void OnResume()
    {
    }
}
