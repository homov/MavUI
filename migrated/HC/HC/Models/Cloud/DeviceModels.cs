using System.Text.Json.Serialization;

namespace Hc.Models.Cloud;

public class DevicesResponse : BaseResponse
{
    [JsonPropertyName("devices")]
    public List<UserDeviceInfo> Devices { get; set; } = [];
}

/// <summary>A device as returned by get_user_devices and get_group_devices.</summary>
public class UserDeviceInfo
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("device_type")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("online_status")]
    public string OnlineStatus { get; set; } = string.Empty;

    [JsonPropertyName("heat_state")]
    public bool HeatState { get; set; }

    [JsonPropertyName("need_heat_state")]
    public bool NeedHeatState { get; set; }

    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    [JsonPropertyName("group_work")]
    public bool GroupWork { get; set; }

    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; set; }

    [JsonPropertyName("timer")]
    public int? Timer { get; set; }

    [JsonPropertyName("is_sensor")]
    public bool IsSensor { get; set; }

    [JsonPropertyName("external_sensor_id")]
    public string? ExternalSensorId { get; set; }

    [JsonPropertyName("sensors")]
    public string[] Sensors { get; set; } = [];

    /// <summary>Allowed control modes, for example "time", "temperature", "humidity".</summary>
    [JsonPropertyName("control_type")]
    public string[] ControlTypes { get; set; } = [];

    [JsonPropertyName("humidity")]
    public float? Humidity { get; set; }

    [JsonPropertyName("humidity_mode")]
    public int? HumidityMode { get; set; }

    [JsonPropertyName("antifreeze_bit")]
    public int? AntifreezeBit { get; set; }

    [JsonPropertyName("gistereses")]
    public int? Hysteresis { get; set; }

    [JsonPropertyName("temp_panel")]
    public float? PanelTemperature { get; set; }

    [JsonPropertyName("temp_room")]
    public float? RoomTemperature { get; set; }

    [JsonPropertyName("need_temp_panel")]
    public float? NeedPanelTemperature { get; set; }

    [JsonPropertyName("need_temp_room")]
    public float? NeedRoomTemperature { get; set; }

    /// <summary>Calibration coefficient of the room sensor (field "t1_coef").</summary>
    [JsonPropertyName("t1_coef")]
    public float? RoomCoefficient { get; set; }

    /// <summary>Calibration coefficient of the panel sensor (field "t2_coef").</summary>
    [JsonPropertyName("t2_coef")]
    public float? PanelCoefficient { get; set; }

    [JsonPropertyName("bound_state")]
    public string BoundState { get; set; } = "0";

    [JsonPropertyName("sharing_info")]
    public List<SharingInfo> SharingInfo { get; set; } = [];
}

public class GetDeviceTypesResponse : BaseResponse
{
    [JsonPropertyName("device_types")]
    public List<DeviceTypeInfo> DeviceTypes { get; set; } = [];
}

/// <summary>Limits and capabilities of one device type.</summary>
public class DeviceTypeInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("picture")]
    public string Picture { get; set; } = string.Empty;

    [JsonPropertyName("temp_min")]
    public int? MinTemperature { get; set; }

    [JsonPropertyName("temp_max")]
    public int? MaxTemperature { get; set; }

    [JsonPropertyName("humi_min")]
    public int? MinHumidity { get; set; }

    [JsonPropertyName("humi_max")]
    public int? MaxHumidity { get; set; }

    /// <summary>Hysteresis limits (fields "gis_min" and "gis_max").</summary>
    [JsonPropertyName("gis_min")]
    public int? MinHysteresis { get; set; }

    [JsonPropertyName("gis_max")]
    public int? MaxHysteresis { get; set; }

    [JsonPropertyName("control_type")]
    public string[]? ControlTypes { get; set; }
}

public class ErrorCodesResponse : BaseResponse
{
    /// <summary>Error code to localized message.</summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, string> Errors { get; set; } = [];
}

public class AllErrorCodesResponse : BaseResponse
{
    /// <summary>Language to (error code to message). Used for an offline dictionary of messages.</summary>
    [JsonPropertyName("errors")]
    public Dictionary<string, Dictionary<string, string>> Errors { get; set; } = [];
}
