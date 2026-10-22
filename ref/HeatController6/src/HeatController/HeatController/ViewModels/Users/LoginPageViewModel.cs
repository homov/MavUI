namespace HeatController;

public partial class LoginPageViewModel: BaseViewModel
{
    private readonly HeatCeramService heatCeramService;
    private readonly SettingService settingService;

    public LoginPageViewModel(HeatCeramService heatCeramService, SettingService settingService )
    {
        this.heatCeramService = heatCeramService;
        this.settingService = settingService;
        Email = settingService.UserEmail;
        SavePassword = settingService.UserAutologin;
    }

    [ObservableProperty]
    string email;

    [ObservableProperty]
    string password;

    [ObservableProperty]
    bool savePassword = false;


    [RelayCommand]
    async Task Forgot()
    {
        await navigationService.PushAsync<ForgotPasswordPage>();
    }

    [RelayCommand]
    async Task Register()
    {
        await navigationService.PushAsync<CreateAccountPage>();
    }

    public void Validate()
    {
        if (String.IsNullOrWhiteSpace(Email))
            throw new ValidationException(Resources.EmailNotSpecified);
        if (String.IsNullOrWhiteSpace(Password))
            throw new ValidationException(Resources.PasswordNotSpecified);
    }

    [RelayCommand]
    async Task Login()
    {
        try
        {
            Validate();

            using var busy = Busy();

            var encryptedPassword = HeatCeram.Api.PasswordEncrypter.Encrypt(Password);
            await heatCeramService.Login(Email, encryptedPassword);
            settingService.UserEmail = Email;
            if (SavePassword)
            {
                settingService.UserPassword = encryptedPassword;
                settingService.UserAutologin = true;
            }
            else
            {
                settingService.UserPassword = String.Empty;
                settingService.UserAutologin = false;
            }
            navigationService.SetMainPage<MainPage>();
        }
        catch ( Exception exception )
        {
            await messageBoxService.Alert(exception);
        }
    }
}
