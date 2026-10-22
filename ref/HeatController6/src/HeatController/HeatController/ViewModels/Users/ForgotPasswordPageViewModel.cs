namespace HeatController;

public partial class ForgotPasswordPageViewModel : BaseViewModel
{
    private readonly SettingService settingService;
    private readonly HeatCeramService heatCeramService;

    public ForgotPasswordPageViewModel(SettingService settingService, HeatCeramService heatCeramService)
    {
        this.settingService = settingService;
        this.heatCeramService = heatCeramService;
        Email = settingService.UserEmail;
    }

    [ObservableProperty]
    string email;

    private void Validate()
    {
        if (String.IsNullOrWhiteSpace(Email))
            throw new ValidationException(Resources.EmailNotSpecified);
    }

    [RelayCommand]
    async Task Send()
    {
        try
        {
            Validate();

            using var busy = Busy();

            settingService.UserEmail = this.Email;
            await heatCeramService.QueryRestorePassword(this.Email);
            await navigationService.PushAsync<RestorePasswordPage>();
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }
}
