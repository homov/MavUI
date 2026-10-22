using CommunityToolkit.Mvvm.Messaging;
using HeatCeram.Api;
using HeatController.Interface;
using HeatController.Messages;
using HeatController.Models;
using System.Linq;
using System.Threading;

namespace HeatController;

//public partial class AddDevicePageModel: BaseViewModel
//{
//    readonly HeatCeramService heatCeramService = ContainerService.Container.Resolve<HeatCeramService>();
//    //readonly LocalClient localClient = new();

//    CancellationTokenSource cancelSearchDevice;
//    CancellationTokenSource cancelWaitDevice;

//    [ObservableProperty]
//    FoundDevice foundDevice;

//    [ObservableProperty]
//    string imageUrl;

//    [ObservableProperty]
//    int step = 1;

//    [ObservableProperty]
//    string selectedNetwork;

//    [ObservableProperty]
//    string password;

//    [ObservableProperty]
//    string name;

//    UserDevice newDevice;

//    public AddDevicePageModel()
//    {
        
//    }

//    public Task Start()
//    {
//        CancelAll();
//        if (cancelSearchDevice != null)
//        {
//            cancelSearchDevice.Dispose();
//            cancelSearchDevice = null;
//        }
//        if (cancelWaitDevice != null)
//        {
//            cancelWaitDevice.Dispose();
//            cancelWaitDevice = null;
//        }

//        cancelSearchDevice = new();
//        cancelWaitDevice = new();
        
//        if (Step == 1)
//            return SearchDevice(cancelSearchDevice.Token);
//        else if (Step == 3)
//            return WaitRegistration(cancelWaitDevice.Token);
        
//        return Task.CompletedTask;
//    }

//    public void CancelAll()
//    {
//        if (cancelSearchDevice !=null && !cancelSearchDevice.IsCancellationRequested)
//            cancelSearchDevice.Cancel();
//        if (cancelWaitDevice !=null && !cancelWaitDevice.IsCancellationRequested)
//            cancelWaitDevice.Cancel();
//    }

//    async Task WaitRegistration(CancellationToken token)
//    {
//        DateTime now = DateTime.Now;
//        bool isTimeOut = false;
//        int errorCount = 0;
//        while (!token.IsCancellationRequested)
//        {
//            await Task.Delay(TimeSpan.FromSeconds(4));
//            if (token.IsCancellationRequested)
//                break;

//            if ((DateTime.Now - now).TotalSeconds >= 60 )
//            {
//                isTimeOut = true;
//                break;
//            }
//            try
//            {
//                var isHostFound = await CrossConnectivity.Current.IsReachable(ApiConstants.HeatCeramHost, 1000);
//                if (isHostFound)
//                {
//                    await heatCeramService.Reset();

//                    var deviceList = await heatCeramService.GetDevices(true);
//                    if (token.IsCancellationRequested)
//                        break;

//                    var device = deviceList.FirstOrDefault(it => it.Info.DeviceId == FoundDevice.Id);
//                    if (device != null)
//                    {
//                        newDevice = device;
//                        if (token.IsCancellationRequested)
//                            break;
//                        try
//                        {
//                            await heatCeramService.SetDeviceName(device.Info.DeviceId, Name);
//                            newDevice.Info.DeviceName = Name;
//                        }
//                        catch (Exception exception)
//                        {
//                            errorCount += 1;
//                            if (errorCount >= 3)
//                            {
//                                await messageBoxService.Alert(exception);
//                                await navigationService.PopAsync();
//                                return;
//                            }
//                        }

//                        if (token.IsCancellationRequested)
//                            break;

//                        await mainThread.InvokeOnMainThreadAsync(() =>
//                        {
//                            WeakReferenceMessenger.Default.Send(new DeviceListChangedMessage(ChangeType.Created));
//                            Step = 4;
//                        });

//                        break;
//                    }
//                }
//            }
//            catch( Exception exception )
//            {
//                Console.WriteLine(exception.Message);
//            }
//        }
//        if (isTimeOut)
//        {
//            await messageBoxService.Alert( Resources.WaitDeviceTimeout );
//            await navigationService.PopAsync();
//        }
//    }

//    async Task SearchDevice(CancellationToken token)
//    {
//        while (!token.IsCancellationRequested)
//        {
//            try
//            {
//                Console.WriteLine("== Lookup device... ==");
//                var isHostFound = await CrossConnectivity.Current.IsReachable(ApiConstants.LocalDeviceHost,1000);
//                if (isHostFound)
//                {
//                    Console.WriteLine("== Get device information... ==");
//                    var localClient = new LocalClient();

//                    using var response = await localClient.Api.GetDeviceId();
//                    if (token.IsCancellationRequested)
//                        break;

//                    if (response.IsSuccessStatusCode)
//                    {
//                        var device = new FoundDevice()
//                        {
//                            Id = response.Content
//                        };
//                        Console.WriteLine($"== Lookup device id: {device.Id} ==");

//                        using var response1 = await localClient.Api.GetDeviceModel();
//                        if (token.IsCancellationRequested)
//                            break;

//                        if (!response1.IsSuccessStatusCode)
//                            continue;
//                        device.Model = response1.Content;

//                        using var response2 = await localClient.Api.GetWifiList();

//                        if (token.IsCancellationRequested)
//                            break;

//                        if (!response2.IsSuccessStatusCode)
//                            continue;
//                        device.Networks = response2.Content.Split(',');

//                        Console.WriteLine($"== Lookup device model: {device.Model} ==");

//                        await mainThread.InvokeOnMainThreadAsync(() =>
//                        {
//                            FoundDevice = device;
//                            if (device.Networks != null && device.Networks.Length > 0)
//                                SelectedNetwork = device.Networks.First();
//                            ImageUrl = $"{ApiConstants.ImageBaseUrl}/{device.Model}.png";
//                            Step = 2;
//                        });
//                        break;
//                    }
//                }
//            }
//            catch (Exception exception)
//            {
//                Console.WriteLine($"== Lookup device failed: {exception.Message} ==");
//            }
//            if (token.IsCancellationRequested)
//                break;
//            await Task.Delay(TimeSpan.FromSeconds(2));
//        }
//    }

//    [RelayCommand]
//    void WiFiSettings()
//    {
//        IWiFiService service = DependencyService.Get<IWiFiService>();
//        service.OpenSettings();
//    }

//    async Task UpdateDeviceConfiguration()
//    {
//        var localClient = new LocalClient();

//        using var response = await localClient.Api.SetUserId(heatCeramService.UserId);
//        if (response.IsSuccessStatusCode && response.Content.Contains("accepted"))
//        {
//            using var response1 = await localClient.Api.SetDeviceParameters(SelectedNetwork, Password);
//            if (!response1.IsSuccessStatusCode || !response1.Content.Contains("accepted"))
//                throw new Exception(Resources.ErrorPassingParameters);
//        }
//        else
//        {
//            throw new Exception(Resources.ErrorPassingParameters);
//        }
//    }

//    [RelayCommand]
//    async Task Register()
//    {
//        try
//        {
//            if (String.IsNullOrEmpty(SelectedNetwork))
//                throw new ValidationException(Resources.WifiNetworkNotSpecified);
//            if (String.IsNullOrEmpty(Password))
//                throw new ValidationException(Resources.PasswordNotSpecified);
//            if (String.IsNullOrEmpty(Name))
//                throw new ValidationException(Resources.NameNotSpecified);
//            await UpdateDeviceConfiguration();
//            await mainThread.InvokeOnMainThreadAsync(() =>
//            {
//                Step = 3;
//            });
//            WaitRegistration(cancelWaitDevice.Token);
//        }
//        catch ( Exception exception )
//        {
//            await messageBoxService.Alert(exception);
//        }
//    }

//    [RelayCommand]
//    async Task Configure()
//    {
//        var page = DeviceListViewModel.GetDevicePage(newDevice, 1);
//        await navigationService.PopAsync();
//        await navigationService.PushAsync(page);
//    }
//}

//public class FoundDevice
//{
//    public string Model { get; set; }
//    public string Id { get; set; }

//    public string[] Networks { get; set; }
//}
