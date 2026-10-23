using System.Collections.Generic;
using System.Linq;

namespace HC.Api.Models;

public class GetUserGroupsResponse : BaseResponse
{
    public List<UserGroupInfo> Groups { get; set; } = new();
}

public class UserGroupInfo
{
    [JsonPropertyName("group_id")]
    public string GroupId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("schedule")]
    public string Schedule { get; set; } = "";

    [JsonPropertyName("average_temp")]
    public float? AverageTemp { get; set; }

    [JsonPropertyName("average_humidity")]
    public float? AverageHumidity { get; set; }

    [JsonPropertyName("manual")]
    public string Manual { get; set; } = "";

    [JsonPropertyName("temp_room")]
    public float? TempRoom { get; set; }

    [JsonPropertyName("timer")]
    public int? Timer { get; set; }

    [JsonPropertyName("heat_state")]
    public int? HeatState { get; set; }

    [JsonPropertyName("external_sensor_id")]
    public string ExternalSensorId { get; set; } = "";

    [JsonPropertyName("external_sensorstate")]
    public string ExternalSensorState { get; set; } = "";

    [JsonPropertyName("sharing_info")]
    public List<SharingInfo> SharingInfo { get; set; } = new();

    [JsonIgnore] public bool IsHeaterOn => HeatState is > 0;
    //[JsonIgnore] public string FormattedTemperature => AverageTemp.HasValue ? $"{AverageTemp:0}°C" : "";
    [JsonIgnore] public string FormattedTemperature => AverageTemp.HasValue ? $"{AverageTemp:0}°" : "";
    [JsonIgnore] public string FormattedHumidity => HasHumidity ? $"{AverageHumidity:0}%" : "";
    [JsonIgnore] public bool HasHumidity => AverageHumidity is > 0;
    [JsonIgnore] public bool NotHasHumidity => !HasHumidity;
    [JsonIgnore] public bool HasTemperature => AverageTemp.HasValue;
    [JsonIgnore] public bool NotHasTemperature => !HasTemperature;
    [JsonIgnore] public bool IsControlManual => Manual == "1";
    [JsonIgnore] public bool IsControlNotManual => !IsControlManual;

    public bool HasSameId(UserGroupInfo? other) =>
        other is not null &&
        string.Equals(GroupId, other.GroupId, StringComparison.Ordinal);

    public bool Equals(UserGroupInfo? other) =>
        other is not null &&
        string.Equals(GroupId, other.GroupId, StringComparison.Ordinal) &&
        string.Equals(Name, other.Name, StringComparison.Ordinal) &&
        string.Equals(Schedule, other.Schedule, StringComparison.Ordinal) &&
        Nullable.Equals(AverageTemp, other.AverageTemp) &&
        Nullable.Equals(AverageHumidity, other.AverageHumidity) &&
        string.Equals(Manual, other.Manual, StringComparison.Ordinal) &&
        Nullable.Equals(TempRoom, other.TempRoom) &&
        Nullable.Equals(Timer, other.Timer) &&
        Nullable.Equals(HeatState, other.HeatState) &&
        string.Equals(ExternalSensorId, other.ExternalSensorId, StringComparison.Ordinal) &&
        string.Equals(ExternalSensorState, other.ExternalSensorState, StringComparison.Ordinal) &&
        SharingInfo.Count == other.SharingInfo.Count &&
        SharingInfo.SequenceEqual(other.SharingInfo);

    public override bool Equals(object? obj) => Equals(obj as UserGroupInfo);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GroupId, StringComparer.Ordinal);
        hash.Add(Name, StringComparer.Ordinal);
        hash.Add(Schedule, StringComparer.Ordinal);
        hash.Add(AverageTemp);
        hash.Add(AverageHumidity);
        hash.Add(Manual, StringComparer.Ordinal);
        hash.Add(TempRoom);
        hash.Add(Timer);
        hash.Add(HeatState);
        hash.Add(ExternalSensorId, StringComparer.Ordinal);
        hash.Add(ExternalSensorState, StringComparer.Ordinal);
        foreach (var s in SharingInfo)
            hash.Add(s);
        return hash.ToHashCode();
    }

    public static bool operator ==(UserGroupInfo? a, UserGroupInfo? b) =>
        a is null ? b is null : a.Equals(b);

    public static bool operator !=(UserGroupInfo? a, UserGroupInfo? b) => !(a == b);
}
