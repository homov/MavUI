using System.Text.Json.Serialization;

namespace HeatCeram.Api.Models;

public class DevicesResponse : BaseResponse
{
    [JsonPropertyName("devices")]
    public List<UserDeviceInfo> Devices { get; set; } = new List<UserDeviceInfo>();
}


public class UserDeviceInfo
{
    public UserDeviceInfo Cleanup()
    {
        if (string.IsNullOrEmpty(ScheduleId))
            ScheduleId = null;
        if (string.IsNullOrEmpty(ExternalSensorId))
            ExternalSensorId = null;
        if (string.IsNullOrEmpty(GroupId))
            GroupId = null;
        return this;
    }

    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; set; } = null;

    [JsonPropertyName("group_work")]
    public bool GroupWork { get; set; } = false;

    [JsonPropertyName("timer")]
    public int? Timer { get; set; }

    [JsonPropertyName("is_sensor")]
    public bool IsSensor { get; set; } = false;

    [JsonPropertyName("device_type")] 
    public string DeviceType { get; set; } = String.Empty;

    [JsonPropertyName("external_sensor_id")]
    public string? ExternalSensorId { get; set; } = null;

    [JsonPropertyName("sensors")]
    public string[] Sensors { get; set; }
    
    [JsonPropertyName("control_type")]
    public string[] ControlType { get; set; }

    [JsonPropertyName("online_status")] 
    public string OnlineStatus { get; set; } = String.Empty;

    [JsonPropertyName("humidity")]
    public float? Humidity { get; set; }

    [JsonPropertyName("antifreeze_bit")]
    public int? AntifreezeBit { get; set; }

    [JsonPropertyName("humidity_mode")]
    public int? HumiBit { get; set; }

    [JsonPropertyName("gistereses")]
    public int? Gistereses { get; set; }

    [JsonPropertyName("heat_state")]
    public bool HeatState { get; set; } = false;

    [JsonPropertyName("need_heat_state")]
    public bool NeedHeatState { get; set; } = false;

    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = String.Empty;

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = String.Empty;

    [JsonPropertyName("temp_panel")]
    public float? TempPanel { get; set; } = null;

    [JsonPropertyName("temp_room")]
    public float? TempRoom { get; set; } = null;

    [JsonPropertyName("need_temp_panel")]
    public float? NeedTempPanel { get; set; } = null;

    [JsonPropertyName("need_temp_room")]
    public float? NeedTempRoom { get; set; } = null;


    [JsonPropertyName("t1_coef")]
    public float? RoomTempCoff { get; set; } = null;

    [JsonPropertyName("t2_coef")]
    public float? PanelTempCoff { get; set; } = null;
    
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; } = String.Empty;

    [JsonPropertyName("bound_state")]
    public string BoundState { get; set; } = "0";

    [JsonPropertyName("sharing_info")]
    public List<SharingInfo> SharingInfo { get; set; } = new List<SharingInfo>();

    [JsonIgnore] public bool HasGroup => !string.IsNullOrEmpty(GroupId);
    [JsonIgnore] public bool IsNotSensor => !IsSensor;
    [JsonIgnore] public bool IsOnline => OnlineStatus == "online";
    [JsonIgnore] public bool IsHeat => HeatState == true;
    [JsonIgnore] public bool IsNotHeat => !IsHeat;

    [JsonIgnore] public bool IsNotOnline => !IsOnline;
    [JsonIgnore] public bool HasExternalSensor => !string.IsNullOrEmpty(ExternalSensorId);
    [JsonIgnore] public string ImageUrl => string.IsNullOrEmpty(DeviceType) ? "" : $"{ApiConstants.ImageBaseUrl}/{this.DeviceType}.png";
    [JsonIgnore] public string FormattedTemperature => HasTemperature ? string.Format("{0:0}°C", TempPanel) : "";
    [JsonIgnore] public string FormattedRoomTemperature => HasRoomTemperature ? string.Format("{0:0}°C", TempRoom) : "";
    [JsonIgnore] public string FormattedHumidity => HasHumidity ? string.Format("{0:0}%", Humidity) : "";
    [JsonIgnore] public bool HasTemperature => TempPanel != null;
    [JsonIgnore] public bool HasRoomTemperature => TempRoom != null;
    [JsonIgnore] public bool HasHumidity => Humidity != null;

    [JsonIgnore] public bool CanControlByTime => ControlType != null && ControlType.Contains("time");
    [JsonIgnore] public bool CanControlByTemperature => ControlType != null && ControlType.Contains("temperature");
    [JsonIgnore] public bool CanControlByHumidity => ControlType != null && ControlType.Contains("humidity");
}

