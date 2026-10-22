using CommunityToolkit.Mvvm.Messaging;
using HeatController.Messages;
using HeatController.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Headers;

namespace HeatController;

public partial class DeviceListViewModel : BaseViewModel, IRecipient<DeviceListChangedMessage>
{
	private readonly HeatCeramService heatCeramService;

    [ObservableProperty]
    ObservableCollection<UserDevice> devices = new();

    [ObservableProperty]
    bool isRefreshing;


    [ObservableProperty]
    string locationId;

    private TimerHelper timerHelper;

    public DeviceListViewModel(HeatCeramService heatCeramService)
	{
		this.heatCeramService = heatCeramService;
        WeakReferenceMessenger.Default.Register<DeviceListChangedMessage>(this);
        timerHelper = new TimerHelper(TimeSpan.FromSeconds(30), ReloadDevices, true);
    }

    public void ReloadDevices()
    {
        //Console.WriteLine("Begin reload device list...");
        mainThread.BeginInvokeOnMainThread(() => IsRefreshing = true);
        //Console.WriteLine("End reload device list");
    }

    public void StartAutoreload()
    {
        timerHelper.Start();
    }

    public void StopAutoreload()
    {
        timerHelper.Stop();
    }

    [RelayCommand]
    async Task Load()
    {
        await LoadDevices();
        await mainThread.InvokeOnMainThreadAsync(() => IsRefreshing = false);
    }


    [RelayCommand]
    async Task Show(UserDevice device)
    {
        using var busy = Busy();
        try
        {
            var page = GetDevicePage(device);
            await navigationService.PushAsync(page);
        }
        catch(Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

    public static ContentPage GetDevicePage(UserDevice device, int selectedPage = -1)
    {
        ContentPage page;
        if (device.Info.IsSensor || String.IsNullOrEmpty(device.Info.ScheduleId))
        {
            var model = new DevicePageViewModel(device);
            page = new SimpleDevicePage(model);
        }
        else
        {
            var model = new DevicePageViewModel(device);
            if (selectedPage != -1)
                model.SelectedPage = selectedPage;
            page = new DevicePage(model);
        }
        return page;
    }


    async Task Turn(UserDevice device, bool state)
    {
        try
        {
            using var busy = Busy();
            await heatCeramService.SetDeviceState(device.Id, state);
            device.Info.NeedHeatState = state;
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

    [RelayCommand]
    async Task TurnOn(UserDevice device)
    {
        await Turn(device, true);
    }

    [RelayCommand]
    async Task TurnOff(UserDevice device)
    {
        await Turn(device, false);
    }

    async Task LoadDevices()
    {
        try
        {
            
            if (LocationId == null)
                return;

            var list = await heatCeramService.GetDevices(true);
            if (LocationId != "all")
                list = list.Where(it => it.GroupId == LocationId).ToList();
            await mainThread.InvokeOnMainThreadAsync(() => Devices = new ObservableCollection<UserDevice>( list ));
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

    public void SetLocation(string id)
    {
        mainThread.InvokeOnMainThreadAsync(async () =>
        {
            LocationId = id;
            using var busy = Busy();
            await LoadDevices();
        });
    }

    public void Receive(DeviceListChangedMessage message)
    {
        mainThread.BeginInvokeOnMainThread(() => IsRefreshing = true);
    }
}
