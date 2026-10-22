namespace HeatController;

public class LoadingPageViewModel: BaseViewModel
{
	private readonly HeatCeramService heatCeramService;
	private readonly SettingService settingService;
    private readonly ThemeService themeService;

    public LoadingPageViewModel(HeatCeramService heatCeramService, SettingService settingService, ThemeService themeService)
    {
		this.heatCeramService = heatCeramService;
		this.settingService = settingService;
        this.themeService = themeService;
    }

    public async Task Initialize()
    {
        await CheckLogin().ConfigureAwait(false);
    }

    public async Task CheckLogin()
    {
        using var busy = Busy();

        bool doLogin = false;
        if (!settingService.UserAutologin || settingService.UserEmail == String.Empty || settingService.UserPassword == String.Empty)
            doLogin = true;
        else
        {
            try
            {
                await heatCeramService.Login(settingService.UserEmail, settingService.UserPassword);
                doLogin = false;
            }
            catch
            {
                doLogin = true;
            }
        }
        if (doLogin)
            navigationService.SetMainPage<LoginPage>();
        else
            navigationService.SetMainPage<MainPage>();
    }
}
