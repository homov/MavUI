using System.Linq;
using System.Net.Http;
using CommunityToolkit.Mvvm.Messaging;
using HeatCeram.Api;
using HeatCeram.Api.Models;
using HeatController.Messages;
using HeatController.Models;
using HeatController.Pages.Device.AddDevice;
using HeatController.Services.WifiManager;
using Xamarin.Essentials;

namespace HeatController;


public class AccessPoint
{
    public string Ssid { get; set; }
}

public partial class AddDeviceSteps: BaseViewModel
{
    IWifiNetworkService wifiService = ContainerService.Container.Resolve<IWifiNetworkService>();

    HeatCeramService heatCeramService = ContainerService.Container.Resolve<HeatCeramService>();

    [ObservableProperty] string controllerName;
    [ObservableProperty] string groupName;
    [ObservableProperty] List<UserLocation> locationList;
    [ObservableProperty] UserLocation selectedLocation;
    [ObservableProperty] List<AccessPoint> accespointList;
    [ObservableProperty] AccessPoint selectedAccespoint;
    [ObservableProperty] string accespointPassword;
    [ObservableProperty] string deviceId;
    [ObservableProperty] string deviceModel;
    [ObservableProperty] string deviceImage;
    [ObservableProperty] bool isRegistering;

    [ObservableProperty] bool searchingAccessPoint = false;

    public AddDeviceSteps()
    {
        IsReady = false;
        Task.Run(() => Load());
    }

    private async Task Load()
    {
        try
        {
            using var busy = Busy();

            var list = (await heatCeramService.GetLocations(reload:true)).OrderBy(it => it.Name).ToList();
            list.Insert(0, new UserLocation(new UserGroupInfo() { Name = Resources.WithoutLocation }));

            await Device.InvokeOnMainThreadAsync(() =>
            {
                LocationList = list;
                SelectedLocation = LocationList.First();
                IsReady = true;
            });
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

    [RelayCommand]
    async Task NewLocation()
    {
        var newGroup = await messageBoxService.PromptEdit(Resources.NewGroup, Resources.EnterNewGroupName);
        if (!String.IsNullOrEmpty(newGroup))
        {
            try
            {
                using var busy = Busy();
                await heatCeramService.CreateLocation(newGroup);
                await Load();

                await Device.InvokeOnMainThreadAsync(() =>
                {
                    SelectedLocation = LocationList.Where(it => it.Name == newGroup).FirstOrDefault();
                    if (SelectedLocation == null)
                    {
                        SelectedLocation = LocationList.First();
                    }
                });
            }
            catch (Exception exception)
            {
                await messageBoxService.Alert(exception);
            }
        }
    }


    [RelayCommand]
    async Task Instruction()
    {
        if (String.IsNullOrEmpty(ControllerName))
        {
            await messageBoxService.Alert(Resources.NameNotSpecified);
            return;
        }

        await navigationService.PushAsync(new AddDeviceStep2Page() { BindingContext = this });
    }

    [RelayCommand]
    async Task RetryConnect()
    {
        await navigationService.PopAsync().ContinueWith(t => TryConnect());
    }

    [RelayCommand]
    async Task GoSettings()
    {
        await wifiService.OpenWifiSetting();
    }

    async Task UpdateSearchStatus(bool isSearching)
    {
        await Device.InvokeOnMainThreadAsync(() =>
        {
            this.SearchingAccessPoint = isSearching;
        });

    }

    [RelayCommand]
    async Task Register()
    {
        //var isHostFound = await CrossConnectivity.Current.IsReachable(ApiConstants.LocalDeviceHost, 1000);
        //if (isHostFound)
        //{
            try
            {
                await AddDevice();
                await WaitRegistration();
            }
            catch (Exception exception)
            {
                await messageBoxService.Alert(exception);
            }
        //}
    }


    async Task AddDevice()
    {
        if (SelectedAccespoint == null)
            throw new ValidationException(Resources.WifiNetworkNotSpecified);

        if (String.IsNullOrEmpty(AccespointPassword))
            throw new ValidationException(Resources.PasswordNotSpecified);

        //var isHostFound = await CrossConnectivity.Current.IsReachable(ApiConstants.LocalDeviceHost, 1000);
        //if (isHostFound)
        //{
            var localClient = new LocalClient();

            using var response = await localClient.Api.SetUserId(heatCeramService.UserId);
            if (response.IsSuccessStatusCode && response.Content.Contains("accepted"))
            {
                using var response1 = await localClient.Api.SetDeviceParameters(SelectedAccespoint.Ssid, AccespointPassword);
                if (!response1.IsSuccessStatusCode || !response1.Content.Contains("accepted"))
                    throw new Exception(Resources.ErrorPassingParameters);
            }
            else
            {
                throw new Exception(Resources.ErrorPassingParameters);
            }
        //}
        //else
        //{
        //    throw new Exception(Resources.ErrorPassingParameters);
        //}
    }

    async Task WaitRegistration()
    {
        await Device.InvokeOnMainThreadAsync( () => IsRegistering = true );


        DateTime now = DateTime.Now;
        bool isTimeOut = false;
        int errorCount = 0;
        while (true)
        {
            if ((DateTime.Now - now).TotalSeconds >= 60)
            {
                isTimeOut = true;
                break;
            }
            await Task.Delay(1000);
            try
            {
                //var isHostFound = await CrossConnectivity.Current.IsReachable(ApiConstants.HeatCeramHost, 1000);
                //if (isHostFound)
                //{
                    //await heatCeramService.Reset();

                    var deviceList = await heatCeramService.GetDevices(true);
                    var device = deviceList.FirstOrDefault(it => it.Info.DeviceId == DeviceId);
                    if (device != null)
                    {
                        try
                        {
                            await heatCeramService.SetDeviceName(device.Info.DeviceId, ControllerName);
                            await Task.Delay(1000);
                            if (SelectedLocation.Info.GroupId !=null)
                            {
                                await heatCeramService.SetDeviceGroup(device.Info.DeviceId, SelectedLocation.Info.GroupId);
                            }
                            await mainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                WeakReferenceMessenger.Default.Send(new DeviceListChangedMessage(ChangeType.Created));
                                await navigationService.PopToRootAsync();
                            });
                            break;
                        }
                        catch (Exception exception)
                        {
                            errorCount += 1;
                            if (errorCount >= 3)
                            {
                                await messageBoxService.Alert(exception.Message);
                                await navigationService.PopToRootAsync();
                                return;
                            }
                        }
                       
                    }
                //}
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
        if (isTimeOut)
        {
            await messageBoxService.Alert(Resources.WaitDeviceTimeout);
            await navigationService.PopAsync();
        }
    }

    async Task ConfigureHeatCream()
    {
        await UpdateSearchStatus(false);

        //var isHostFound = await CrossConnectivity.Current.IsReachable(ApiConstants.LocalDeviceHost, 1000);
        //if (isHostFound)
        //{
            try
            {
                var localClient = new LocalClient();

                using var deviceIdResponse = await localClient.Api.GetDeviceId();
                if (!deviceIdResponse.IsSuccessStatusCode)
                    return;

                using var deviceModelResponse = await localClient.Api.GetDeviceModel();
                if (!deviceModelResponse.IsSuccessStatusCode)
                    return;

                using var wifiListResponse = await localClient.Api.GetWifiList();
                if (!wifiListResponse.IsSuccessStatusCode)
                    return;

                DeviceId = deviceIdResponse.Content;
                DeviceModel = deviceModelResponse.Content;
                DeviceImage = $"{ApiConstants.ImageBaseUrl}/{DeviceModel}.png";
                AccespointList = wifiListResponse.Content.Split(',').Select(it => new AccessPoint() { Ssid = it }).ToList();
                SelectedAccespoint = AccespointList.FirstOrDefault();
                IsRegistering = false;

                await Device.InvokeOnMainThreadAsync(async () =>
                    await navigationService.PushAsync(new AddDeviceStep3Page() { BindingContext = this })
                );
            }
            catch (Exception ex)
            {
                await messageBoxService.Alert(ex.Message);
            }
        //}
        //else
        //{
        //    return;
        //}

    }

    async Task NavigateToError(string? message)
    {
        await Device.InvokeOnMainThreadAsync( async () =>
        {
            await navigationService.PushAsync(new AddDeviceStepErrorPage() { BindingContext = this });
        });
    }

    async Task ConnectHeatCream()
    {
        int connectTryCount = 5;
        int totalTryCount = 5;
        
        
        while (totalTryCount > 0)
        {
            while (connectTryCount > 0)
            {
                try
                {
                    var netInfo = await wifiService.GetNetworkInfo();
                    System.Diagnostics.Debug.WriteLine($"Connected SSID: {netInfo?.Data.Ssid}");

                    //var localClient = new LocalClient();
                    //var deviceIdResponse =  await localClient.Api.GetDeviceId();

                    using var httpClient = new HttpClient()
                    {
                        Timeout = TimeSpan.FromSeconds(20),
                        BaseAddress = new Uri(ApiConstants.LocalDeviceBaseAddress)
                    };
                    var content = new StringContent("");
                    var deviceIdResponse = await httpClient.PostAsync("/dev_id", content);

                    if (deviceIdResponse.IsSuccessStatusCode)
                    {
                        await ConfigureHeatCream();
                        return;
                    }

                    //if (deviceIdResponse.IsSuccessful)
                    //{
                    //    await ConfigureHeatCream();
                    //    return;
                    //}
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                connectTryCount -= 1;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            //var scanResponse = await wifiService.ScanWifiNetworks();
            //var message = scanResponse.ErrorMessage;
            //if (scanResponse.ErrorCode != WifiErrorCodes.Success)
            //{
            //    await UpdateSearchStatus(false).ContinueWith(t => NavigateToError(scanResponse.ErrorMessage ?? "Не можу прочитати список WiFi точок доступу"));
            //    return;
            //}

            var response = await wifiService.ConnectWifi("HeatCeram", String.Empty);
            if (response.ErrorCode != WifiErrorCodes.Success)
            {
                await UpdateSearchStatus(false).ContinueWith(t => NavigateToError(response.ErrorMessage ?? "Не можу з'єднатися з пристроєм"));
                return;
            }
            totalTryCount--;
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
    

    [RelayCommand]
    async Task TryConnect()
    {
        PermissionStatus status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await messageBoxService.Alert(Resources.NoWiFIPermissions);
            return;
        }

        await UpdateSearchStatus(true).ContinueWith(t => ConnectHeatCream());
    }
}
