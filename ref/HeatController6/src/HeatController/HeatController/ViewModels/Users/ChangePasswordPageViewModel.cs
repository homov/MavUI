using HeatCeram.Api;

namespace HeatController;

public partial class ChangePasswordPageViewModel: BaseViewModel
{
    private readonly HeatCeramService heatCeramService;
    private readonly SettingService settingService;

    public ChangePasswordPageViewModel(HeatCeramService heatCeramService, SettingService settingService)
    {
        this.heatCeramService = heatCeramService;
        this.settingService = settingService;
    }

    [ObservableProperty]
    string currentPassword;

    [ObservableProperty]
    string newPassword;

    [ObservableProperty]
    string confirmPassword;

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
            throw new ValidationException(Resources.PasswordNotSpecified);
        if (string.IsNullOrWhiteSpace(NewPassword))
            throw new ValidationException(Resources.NewPasswordNotSpecified);
        if (NewPassword != ConfirmPassword)
            throw new ValidationException(Resources.PasswordsDoNotMatch);
    }

    [RelayCommand]
    async Task Save()
    {
        try
        {
            Validate();

            using var busy = Busy();
            await heatCeramService.ChangePassword(settingService.UserEmail, PasswordEncrypter.Encrypt(CurrentPassword), PasswordEncrypter.Encrypt(NewPassword));
            navigationService.SetMainPage<LoginPage>();
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }
}
