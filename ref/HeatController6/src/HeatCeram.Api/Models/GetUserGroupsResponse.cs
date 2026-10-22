using System.Text.Json.Serialization;

namespace HeatCeram.Api.Models;
public class GetUserGroupsResponse : BaseResponse
{
    public List<UserGroupInfo> Groups { get; set; } = new List<UserGroupInfo>();
}

public class UserGroupInfo
{
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; } = null;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("schedule")] 
    public string Schedule { get; set; } = string.Empty;

    [JsonPropertyName("average_temp")]
    public float? AverageTemp { get; set; } = null;

    [JsonPropertyName("average_humidity")]
    public float? AverageHumidity { get; set; } = null;

    [JsonPropertyName("manual")] 
    public string Manual { get; set; } = string.Empty;

    [JsonPropertyName("temp_room")]
    public float? TempRoom { get; set; } = null;

    [JsonPropertyName("timer")]
    public int? Timer { get; set; } = null;

    [JsonPropertyName("heat_state")]
    public int? HeatState { get; set; } = null;
    
    [JsonPropertyName("external_sensor_id")]
    public string ExternalSensorId { get; set; } = string.Empty;

    [JsonPropertyName("external_sensorstate")]
    public string ExternalSensorState { get; set; } = string.Empty;

    [JsonPropertyName("sharing_info")]
    public List<SharingInfo> SharingInfo { get; set; } = new List<SharingInfo>();

    [JsonIgnore] public bool IsHeaterOn => HeatState is > 0;
    [JsonIgnore]
    public string FormattedTemperature => AverageTemp.HasValue ? $"{AverageTemp:0}°C" : string.Empty;

    [JsonIgnore]
    public string FormattedHumidity =>
        HasHumidity ? $"{AverageHumidity:0}%" : string.Empty;

    [JsonIgnore] public bool HasHumidity => AverageHumidity is > 0;
    [JsonIgnore] public bool NotHasHumidity => !HasHumidity;
    [JsonIgnore] public bool HasTemperature => AverageTemp.HasValue;
    [JsonIgnore] public bool NotHasTemperature => !HasTemperature;
    [JsonIgnore] public bool IsControlManual => Manual == "1";
    [JsonIgnore] public bool IsControlNotManual => !IsControlManual;
    
}
