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

    /// <summary>
    /// A schedule that keeps one temperature all week ("manual" type, hour 0 of every day).
    /// The value is truncated to a whole degree, as the old app did.
    /// </summary>
    public static ScheduleInfo FromTemperature(float value) =>
        Constant("manual", new HourRecord { Hour = 0, Temperature = (int)value });

    /// <summary>A schedule that keeps one humidity all week ("humidity" type). The value is truncated to a whole number.</summary>
    public static ScheduleInfo FromHumidity(float value) =>
        Constant("humidity", new HourRecord { Hour = 0, Humidity = (int)value });

    private static ScheduleInfo Constant(string type, HourRecord record)
    {
        var week = new Dictionary<string, List<HourRecord>>();
        foreach (var day in WeekDays.All)
            week[day] = [record];

        return new ScheduleInfo { ScheduleType = type, Week = week };
    }

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

    public static readonly string[] All = [Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday];
}
