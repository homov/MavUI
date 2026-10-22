using CommunityToolkit.Mvvm.Messaging;
using HeatController.Messages;
using HeatController.Models;
using System.Collections.ObjectModel;

namespace HeatController;

public partial class LocationListViewModel: BaseViewModel, IRecipient<LocationListChangedMessage>
{
    private readonly HeatCeramService heatCeramService;
    
    [ObservableProperty]
    ObservableCollection<UserLocation> locations = new();

    [ObservableProperty]
    bool isRefreshing;

    public LocationListViewModel(HeatCeramService heatCeramService)
    {
        this.heatCeramService = heatCeramService;
        WeakReferenceMessenger.Default.Register<LocationListChangedMessage>(this);
    }

    [RelayCommand]
    async Task Load()
    {
        await LoadLocations();
        await mainThread.InvokeOnMainThreadAsync(() => IsRefreshing = false);
    }

    [RelayCommand]
    async Task Show(UserLocation location)
    {
        var model = new LocationPageViewModel(location);
        var page = new LocationPage(model);
        await navigationService.PushAsync(page);
    }

    async Task LoadLocations()
    {
        try
        {
            var locationList = await heatCeramService.LoadLocations();
            await mainThread.InvokeOnMainThreadAsync(() => Locations = new ObservableCollection<UserLocation>( locationList ));
        }
        catch( Exception exception )
        {
            await messageBoxService.Alert(exception);
        }
    }

    public void Receive(LocationListChangedMessage message)
    {
        mainThread.BeginInvokeOnMainThread(() => IsRefreshing = true);
    }
}
