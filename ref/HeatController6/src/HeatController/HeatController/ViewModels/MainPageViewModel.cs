
using HeatController.Pages.Device.AddDevice;

namespace HeatController;

public partial class MainPageViewModel: BaseViewModel
{
    private readonly HeatCeramService heatCeramService;

    public MainPageViewModel(HeatCeramService heatCeramService)
    {
        this.heatCeramService = heatCeramService;
    }

    [RelayCommand]
    async Task Settings()
    {
        await navigationService.PushAsync<SettingsPage>();
    }

    [RelayCommand]
    async Task Add(object item)
    {
        if ((int)item == 0)
        {
            var newGroup = await messageBoxService.PromptEdit(Resources.NewGroup,Resources.EnterNewGroupName);
            if (!String.IsNullOrEmpty(newGroup))
            {
                try
                {
                    using var busy = Busy();

                    await heatCeramService.CreateLocation(newGroup);
                }
                catch (Exception exception)
                {
                    await messageBoxService.Alert(exception);
                }
            }
        }
        else
        {
            //await navigationService.PushAsync(new AddDevicePage());
            //await navigationService.PushAsync(new DeviceDataPage());
            await navigationService.PushAsync(new AddDeviceStep1Page() { BindingContext = new AddDeviceSteps() });
        }
    }
}
