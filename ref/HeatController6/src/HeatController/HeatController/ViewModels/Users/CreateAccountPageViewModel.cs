using HeatCeram.Api;

namespace HeatController;

public partial class CreateAccountPageViewModel: BaseViewModel
{
    private readonly SettingService settingService;
    private readonly HeatCeramService heatCeramService;

    public CreateAccountPageViewModel(SettingService settingService, HeatCeramService heatCeramService)
    {
        this.settingService = settingService;
        this.heatCeramService = heatCeramService;
    }

    [ObservableProperty]
    string email;

    [ObservableProperty]
    string password;

    [ObservableProperty]
    string confirmPassword;


    private void Validate()
    {
        if (String.IsNullOrWhiteSpace(Email))
            throw new ValidationException(Resources.EmailNotSpecified);
        if (String.IsNullOrWhiteSpace(Password))
            throw new ValidationException(Resources.PasswordNotSpecified);
        if (Password != ConfirmPassword)
            throw new ValidationException(Resources.PasswordsDoNotMatch);

    }
    [RelayCommand]
    async Task SignUp()
    {
        try
        {
            Validate();

            using var busy = Busy();

            settingService.UserEmail = this.Email;
            var encryptedPassword = PasswordEncrypter.Encrypt(this.Password);
            await heatCeramService.RegisterUser(this.Email, encryptedPassword, settingService.PreferredTimezone);
            await navigationService.PushAsync<ConfirmEmailPage>();
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }
}
