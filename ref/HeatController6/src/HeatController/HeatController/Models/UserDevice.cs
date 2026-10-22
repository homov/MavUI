using HeatCeram.Api.Models;

namespace HeatController.Models;

public partial class UserDevice: ObservableObject
{
    public UserDevice(UserDeviceInfo info)
    {
        Info = info;
        isOn = info.NeedHeatState;
    }

    public UserDevice(UserDeviceInfo info, UserGroupInfo location): this( info )
    {
        Location = location;
    }

    public UserDevice(UserDeviceInfo info, UserGroupInfo location, Schedule schedule): this(info,location)
    {
        Schedule = schedule;
    }

    [ObservableProperty] bool isOn;

    [ObservableProperty] UserDeviceInfo info;

    [ObservableProperty] UserGroupInfo location;

    [ObservableProperty] DeviceType deviceType;

    [ObservableProperty] Schedule schedule;

    public string Name => Info.DeviceName;

    public string GroupId => Info.GroupId;
    public string GroupName => Location?.Name;

    public string Id => Info.DeviceId;

    //public bool HasGroup => !string.IsNullOrEmpty(Info.GroupId);

    

    //public string Model => Info.DeviceType;

    //public bool IsSensor => Info.IsSensor;
    //public bool IsNotSensor => !IsSensor;

    //public bool IsOnline => Info.OnlineStatus == "online";
    

    //public bool HasExternalSensor => !string.IsNullOrEmpty(ExternalSensorId);
    //public string ExternalSensorId => Info.ExternalSensorId;

    //public string ImageUrl => string.IsNullOrEmpty(Model) ? "" : $"https://api.heatceram.com.ua/images/{this.Model}.png";

    //public string FormattedTemperature => HasTemperature ? string.Format("{0:0}°C", Info.TempPanel) : "";

    //public string FormattedRoomTemperature => HasRoomTemperature ? string.Format("{0:0}°C", Info.TempRoom) : "";

    //public string FormattedHumidity => HasHumidity ? string.Format("{0:0}%", Info.Humidity) : "";

    //public bool HasTemperature => Info.TempPanel != null;
    
    //public bool HasRoomTemperature => Info.TempRoom != null;
    
    //public bool HasHumidity => Info.Humidity != null;

}
