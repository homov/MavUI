namespace HeatController;

public static class ContainerService
{
    static readonly Lazy<IContainer> _containerHolder = new(CreateContainer);

    static readonly Lazy<ContainerBuilder> _containerBuilderHolder = new(()=>new ContainerBuilder());

    public static ContainerBuilder Builder => _containerBuilderHolder.Value;
    public static IContainer Container => _containerHolder.Value;

    
    private static IContainer CreateContainer()
    {
        var builder = Builder;

        //builder.RegisterType<AppInfoImplementation>().As<IAppInfo>().SingleInstance();
        //builder.RegisterType<DeviceInfoImplementation>().As<IDeviceInfo>().SingleInstance();
        //builder.RegisterType<FileSystemImplementation>().As<IFileSystem>().SingleInstance();
        //builder.RegisterType<LauncherImplementation>().As<ILauncher>().SingleInstance();
        builder.RegisterType<MainThreadImplementation>().As<IMainThread>().SingleInstance();
        builder.RegisterType<PreferencesImplementation>().As<IPreferences>().SingleInstance();
        //builder.RegisterType<SecureStorageImplementation>().As<ISecureStorage>().SingleInstance();
        //builder.RegisterType<VersionTrackingImplementation>().As<IVersionTracking>().SingleInstance();
        
        builder.RegisterType<MessageBoxService>().AsSelf().SingleInstance();
        builder.RegisterType<ThemeService>().AsSelf().SingleInstance();
        builder.RegisterType<LanguageService>().AsSelf().SingleInstance();
        builder.RegisterType<SettingService>().AsSelf().SingleInstance();
        builder.RegisterType<NavigationService>().AsSelf().SingleInstance();
        
        builder.RegisterType<LoadingPageViewModel>().AsSelf();
        builder.RegisterType<LoginPageViewModel>().AsSelf();
        builder.RegisterType<ForgotPasswordPageViewModel>().AsSelf();
        builder.RegisterType<RestorePasswordPageViewModel>().AsSelf();
        builder.RegisterType<CreateAccountPageViewModel>().AsSelf();
        builder.RegisterType<ConfirmEmailPageViewModel>().AsSelf();
        builder.RegisterType<ChangePasswordPageViewModel>().AsSelf();
        builder.RegisterType<MainPageViewModel>().AsSelf();
        builder.RegisterType<SettingsPageViewModel>().AsSelf();
        builder.RegisterType<LocationListViewModel>().AsSelf();
        builder.RegisterType<DeviceListViewModel>().AsSelf();

        builder.RegisterType<LoginPage>().AsSelf();
        builder.RegisterType<MainPage>().AsSelf();
        builder.RegisterType<LoadingPage>().AsSelf();
        builder.RegisterType<ForgotPasswordPage>().AsSelf();
        builder.RegisterType<CreateAccountPage>().AsSelf();
        builder.RegisterType<ConfirmEmailPage>().AsSelf();
        builder.RegisterType<ChangePasswordPage>().AsSelf();
        builder.RegisterType<SettingsPage>().AsSelf();
        builder.RegisterType<LocationPage>().AsSelf();
        builder.RegisterType<RestorePasswordPage>().AsSelf();

#if DEBUG        
        builder.RegisterType<TestPage>().AsSelf();
#endif

        builder.RegisterType<HeatCeramService>().AsSelf().SingleInstance();
        
        return builder.Build();
    }
}
