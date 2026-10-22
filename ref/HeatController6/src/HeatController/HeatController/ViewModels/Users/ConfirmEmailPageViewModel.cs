namespace HeatController;

public partial class ConfirmEmailPageViewModel: BaseViewModel
{

    private readonly HeatCeramService heatCeramService;
    private readonly SettingService settingService;

    public ConfirmEmailPageViewModel(HeatCeramService heatCeramService, SettingService settingService)
    {
        this.heatCeramService = heatCeramService;
        this.settingService = settingService;
    }

    [ObservableProperty]
    string code;


    private void Validate()
    {
        if (String.IsNullOrWhiteSpace(Code))
            throw new ValidationException(Resources.CodeNotSpecified);
    }

    [RelayCommand]
    async Task Verify()
    {
        try
        {
            Validate();

            using var busy = Busy();

            await heatCeramService.ValidateEmail(settingService.UserEmail, this.Code);
            await navigationService.PopToRootAsync();
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }
}
