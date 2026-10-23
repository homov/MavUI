using System.Text.Json.Serialization;

namespace Hc.Models.Cloud;

public class GetScheduleResponse : BaseResponse
{
    [JsonPropertyName("schedule")]
    public ScheduleInfo? Schedule { get; set; }
}

/// <summary>A weekly schedule. Used for both reading and updating (update_schedule sends JSON).</summary>
public class ScheduleInfo
{
    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; set; }

    [JsonPropertyName("schedule_type")]
    public string? ScheduleType { get; set; }

    /// <summary>Hours by weekday. Keys are the values of <see cref="WeekDays"/>.</summary>
    [JsonPropertyName("schedule")]
    public Dictionary<string, List<HourRecord>>? Week { get; set; }

    /// <summary>Receiver of the schedule (returned by get_schedule_by_id).</summary>
    [JsonPropertyName("reciver_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReceiverId { get; set; }

    [JsonPropertyName("reciver_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReceiverType { get; set; }
}

/// <summary>One hour of a day. Null fields are not written, so each control type sends only its own value.</summary>
public class HourRecord
{
    /// <summary>Hour of the day (field "h").</summary>
    [JsonPropertyName("h")]
    public int? Hour { get; set; }

    /// <summary>Target temperature (field "t").</summary>
    [JsonPropertyName("t")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Temperature { get; set; }

    /// <summary>Target humidity (field "p").</summary>
    [JsonPropertyName("p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Humidity { get; set; }

    /// <summary>Hour state for time control (field "s").</summary>
    [JsonPropertyName("s")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? State { get; set; }
}

/// <summary>Weekday keys used in the schedule dictionary.</summary>
public static class WeekDays
{
    public const string Monday = "monday";
    public const string Tuesday = "tuesday";
    public const string Wednesday = "wednesday";
    public const string Thursday = "thursday";
    public const string Friday = "friday";
    public const string Saturday = "saturday";
    public const string Sunday = "sunday";
}
