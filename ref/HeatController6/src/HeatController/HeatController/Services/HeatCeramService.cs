using CommunityToolkit.Mvvm.Messaging;
using FFImageLoading;
using HeatCeram.Api;
using HeatCeram.Api.Models;
using HeatController.Messages;
using HeatController.Models;
using Refit;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace HeatController;


public class RespApiWrap : IDisposable
{
    public HttpClient HttpClient { get;}
    public ICloudClient Client { get;  }

    public RespApiWrap(CookieContainer cookieContainer)
    {
        HttpClient = CreateHttpClient(cookieContainer);
        Client = RestService.For<ICloudClient>(HttpClient);
    }

    public RespApiWrap() : this(null) { }

    HttpClient CreateHttpClient(CookieContainer session)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            UseCookies = true,
            CookieContainer = session
        };
        handler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(ApiConstants.HttpRequestTimeout),
            BaseAddress = new Uri((ApiConstants.HeatCeramBaseAddress))
        };

        return client;
    }

    public void Dispose()
    {
        if (HttpClient!=null)
            HttpClient.Dispose();
    }
}


public class HeatCeramService
{
    //private ICloudClient client;

    object locker = new object();

    List<UserLocation> locations = new List<UserLocation>();
    List<UserDevice> devices = new List<UserDevice>();
    List<DeviceType> deviceTypes = new List<DeviceType>();
    Dictionary<string, string> errorCodes = new Dictionary<string, string>();
    string errorCodesLanguage = "en";

    public string Email { get; private set; }
    public string Password { get; private set; }
    public string UserId { get; private set; }

    public CookieContainer cookieContainer { get; } = new CookieContainer();

    public HeatCeramService()
    {
        ;
    }

    private void CheckResponse(IApiResponse response, BaseResponse baseResponse)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpApiException(response.StatusCode, response.Error);
        }
        if (baseResponse != null && !baseResponse.IsSuccess)
        {
            throw new HeatCeramApiException(baseResponse, response);
        }
        if (response.Error != null)
        {
            throw response.Error;
        }
    }

    public async Task Reset()
    {
        await this.Login(Email, Password, false);
    }

    public async Task Login(string email, string password, bool preloadImages = true)
    {
        locations.Clear();
        devices.Clear();

        var lang = "en";
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.Login(new LoginRequest(email, password,lang));
        CheckResponse(result, result.Content);
        UserId = result.Content.UserId;
        
        try
        {
            Email = email;
            Password = password;
            if (preloadImages)
                await PreloadImages();
        }
        catch (Exception)
        {
            ;
        }
    }

    public async Task PreloadImages()
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.GetDeviceTypes();
        CheckResponse(result, result.Content);
        foreach( var t in result.Content.DevceTypes)
        {
            var url = $"{ApiConstants.ImageBaseUrl}/{t.Picture}";
            try
            {
                await ImageService.Instance.LoadUrl(url).PreloadAsync();
            }
            catch(Exception ex)
            {
            }
        }
    }

    public async Task QueryRestorePassword(string email)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.QueryRestorePassword(new QueryRestorePasswordRequest(email));
        CheckResponse(result, result.Content);

    }
    public async Task RestorePassword(string email, string password, string code)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.RestorePassword(new RestorePasswordRequest(email, password, code));
        CheckResponse(result, result.Content);
    }

    public async Task ValidateEmail(string email, string code)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.ValidateEmail(new ValidateEmailRequest(email, code));
        CheckResponse(result, result.Content);
    }

    public async Task RegisterUser(string email, string password, string timeZone)
    {
        var lang = "en";
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.Register(new RegisterRequest(email, password, timeZone,lang));
        CheckResponse(result, result.Content);
    }

    public async Task DeleteUser()
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.UserDelete();
        CheckResponse(result, result.Content);
    }

    public async Task<IEnumerable<string>> GetTimeZones()
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.GetTimezones();
        CheckResponse(result, result.Content);
        return result.Content.Timezones.GetNames();
    }

    public async Task SetUserTimeZone(string timezone)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetTimezone(new SetTimezoneRequest(timezone));
        CheckResponse(result, result.Content);
    }

    public async Task ChangePassword(string email, string password, string newPassword)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.ChangePassword(new ChangePasswordRequest(email, password, newPassword));
        CheckResponse(result, result.Content);
    }


    public async Task<Dictionary<string,string>> GetErrorCodes(string language)
    {
        if (language == "uk") language = "ua";
        if (errorCodesLanguage != language || errorCodes.Count == 0)
        {
            using var restApi = new RespApiWrap(cookieContainer);
            using var result = await restApi.Client.GetErrorCodes( new ErrorCodesRequest(language));
            CheckResponse(result, result.Content);
            errorCodesLanguage = language;
            errorCodes = result.Content.Errors;
        }
        return errorCodes;
    }


    public async Task<List<UserLocation>> GetLocations(bool reload = false)
    {
        if (locations.Count == 0 || reload)
            return await LoadLocations();
        return locations;
    }

    public async Task<List<UserDevice>> GetDevices(bool reload = false)
    {
        if (devices.Count == 0 || reload)
            return await LoadDevices();
        return devices;
    }


    public async Task<List<DeviceType>> GetDeviceTypes(bool reload = false)
    {
        if (deviceTypes.Count == 0 || reload)
            return await LoadDeviceTypes();
        return deviceTypes;
    }

    public async Task<List<UserDevice>> LoadDevices()
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.GetUserDevices();
        CheckResponse(result, result.Content);

        var deviceTypes = await GetDeviceTypes();
        var localDevices = new List<UserDevice>();
        foreach (var di in result.Content.Devices)
        {
            var clean = di.Cleanup();
            var deviceType = deviceTypes.FirstOrDefault(it => it.DeviceTypeInfo.Name == clean.DeviceType);
            UserLocation location = locations.FirstOrDefault(it => it.Info.GroupId == clean.GroupId);
            var userDevice = new UserDevice(clean, location?.Info);
            userDevice.DeviceType = deviceType;
            localDevices.Add(userDevice);
        }
        lock (locker)
        {
            devices = localDevices;
        }
        return devices;
    }

    public async Task<List<DeviceType>> LoadDeviceTypes()
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.GetDeviceTypes();
        CheckResponse(result, result.Content);

        var localDevices = new List<DeviceType>();
        foreach (var di in result.Content.DevceTypes)
        {
            localDevices.Add(new DeviceType(di));
        }
        lock (locker)
        {
            deviceTypes = localDevices;
        }
        return deviceTypes;
    }

    public async Task<Schedule> LoadSchedule(string scheduleId)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.GetSchedule(new ScheduleRequest(scheduleId));
        CheckResponse(result, result.Content);
        return result.Content.Schedule;
    }

    public async Task<ScheduleModel> LoadScheduleModel(string scheduleId)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.GetSchedule(new ScheduleRequest(scheduleId));
        CheckResponse(result, result.Content);
        return ScheduleModel.FromSchedule(result.Content.Schedule);
    }

    public async Task UpdateSchedule(ScheduleModel model)
    {
        var schedule = model.ToSchedule();
#if DEBUG
        var dumpString = JsonSerializer.Serialize(schedule);
        System.Diagnostics.Debug.WriteLine("Update schedule:");
        System.Diagnostics.Debug.WriteLine(dumpString);
#endif
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.UpdateSchedule(schedule);
        CheckResponse(result, result.Content);
    }

    public async Task<List<UserLocation>> LoadLocations()
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.GetUserGroups();
        CheckResponse(result, result.Content);
                
        var localLocations = new List<UserLocation>();
        foreach (var li in result.Content.Groups)
        {
            Schedule s = null;
            if (!string.IsNullOrEmpty(li.Schedule))
                s = await LoadSchedule(li.Schedule);
            localLocations.Add(new UserLocation(li, s));
        }
        lock( locker)
        {
            locations = localLocations;
        }
        return locations;
    }

    public async Task CreateLocation(string locationName)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.CreateGroup(new CretaeGroupRequest(locationName));
        CheckResponse(result, result.Content);
        WeakReferenceMessenger.Default.Send(new LocationListChangedMessage(ChangeType.Created));
    }

    public async Task UpdateLocationName(string locationId, string name)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetGroupName(new SetGroupNameRequest(locationId, name));
        CheckResponse(result, result.Content);
    }

    public async Task UpdateLocationSensor(string locationId, string sensorId)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetGroupSensor(
            new SetGroupSensorRequest(locationId, string.IsNullOrEmpty(sensorId) ? null : sensorId));
        CheckResponse(result, result.Content);
    }

    public async Task UpdateScheduleType(string scheduleId, string type)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetScheduleType(new SetScheduleTypeRequest(scheduleId, type));
        CheckResponse(result, result.Content);
    }

    public async Task RemoveLocation(string locationId)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.RemoveGroup(new GroupRequest(locationId));
        CheckResponse(result, result.Content);
        WeakReferenceMessenger.Default.Send(new LocationListChangedMessage(ChangeType.Deleted));
    }

    public async Task SetDeviceState(string deviceId, bool newState)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetDeviceHeatState(new SetDeviceHeatStateRequest(deviceId, newState ? "true" : "false"));
        CheckResponse(result, result.Content);
        WeakReferenceMessenger.Default.Send(new DeviceStateChanged(deviceId, newState));
    }

    public async Task SetDeviceName(string deviceId, string name)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetDeviceName(new SetDeviceNameRequest(deviceId, name));
        CheckResponse(result, result.Content);
    }

    public async Task SetDeviceGroup(string deviceId, string groupId)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetDeviceGroup(new SetDeviceGroupRequest(deviceId, groupId));
        CheckResponse(result, result.Content);
    }

    public async Task SetDeviceSensor(string deviceId, string sensorId)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetDeviceSensor(new SetDeviceSensorRequest(deviceId, sensorId));
        CheckResponse(result, result.Content);
    }

    public async Task SetAntifreezeMode(string deviceId, bool isOn)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetAntifreezeMode(new SetAntifreezeModeRequest(deviceId, isOn ? 1 : 0));
        CheckResponse(result, result.Content);
    }

    public async Task SetHumiMode(string deviceId, bool isOn)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetHumiMode(new SetHumiModeRequest(deviceId, isOn ? 1 : 0));
        CheckResponse(result, result.Content);
    }

    public async Task SetPanelCoeff(string deviceId, int value)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetPanelCoeff(new SetCoeffRequest(deviceId, value));
        CheckResponse(result, result.Content);
    }

    public async Task SetRoomCoeff(string deviceId, int value)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetRoomCoeff(new SetCoeffRequest(deviceId, value));
        CheckResponse(result, result.Content);
    }

    public async Task SetPanelTemp(string deviceId, int panelTemp)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetPanelTemp(new SetTempRequest(deviceId, panelTemp));
        CheckResponse(result, result.Content);
    }

    public async Task SetHisteresis(string deviceId, int panelTemp)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.SetHisteresis(new SetTempRequest(deviceId, panelTemp));
        CheckResponse(result, result.Content);
    }

    public async Task DeleteDevice(string deviceId)
    {
        using var restApi = new RespApiWrap(cookieContainer);
        using var result = await restApi.Client.DeleteDevice(new DeviceRequest(deviceId));
        CheckResponse(result, result.Content);
    }
}
