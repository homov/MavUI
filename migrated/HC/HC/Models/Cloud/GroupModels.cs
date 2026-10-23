using System.Text.Json.Serialization;

namespace Hc.Models.Cloud;

public class GetUserGroupsResponse : BaseResponse
{
    [JsonPropertyName("groups")]
    public List<UserGroupInfo> Groups { get; set; } = [];
}

/// <summary>A device group (location) with its averages and control state.</summary>
public class UserGroupInfo
{
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("schedule")]
    public string Schedule { get; set; } = string.Empty;

    [JsonPropertyName("average_temp")]
    public float? AverageTemperature { get; set; }

    [JsonPropertyName("average_humidity")]
    public float? AverageHumidity { get; set; }

    /// <summary>"1" when the group is controlled manually, otherwise automatic.</summary>
    [JsonPropertyName("manual")]
    public string Manual { get; set; } = string.Empty;

    [JsonPropertyName("temp_room")]
    public float? RoomTemperature { get; set; }

    [JsonPropertyName("timer")]
    public int? Timer { get; set; }

    /// <summary>Greater than 0 means the heater of the group is on.</summary>
    [JsonPropertyName("heat_state")]
    public int? HeatState { get; set; }

    [JsonPropertyName("external_sensor_id")]
    public string ExternalSensorId { get; set; } = string.Empty;

    [JsonPropertyName("external_sensorstate")]
    public string ExternalSensorState { get; set; } = string.Empty;

    [JsonPropertyName("sharing_info")]
    public List<SharingInfo> SharingInfo { get; set; } = [];
}
