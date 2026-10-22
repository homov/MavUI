using System.Text.Json.Serialization;

namespace HeatCeram.Api.Models;

public class GetScheduleResponse : BaseResponse
{
    public ScheduleWithReciver? Schedule { get;set; }
}

public class Schedule
{
    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; set; }

    
    [JsonPropertyName("schedule_type")]
    public string? ScheduleType { get; set; }

    [JsonPropertyName("schedule")]
    public WeekSchedule? WeekSchedule { get;set;} 
}

public class ScheduleWithReciver : Schedule
{
    [JsonPropertyName("reciver_id")]
    public string? ReciverId { get; set; }

    [JsonPropertyName("reciver_type")]
    public string? ReciverType { get; set; }
}

public class HourRecord
{
    [JsonPropertyName("h")]
    public int? Hour { get; set; }
    
    [JsonPropertyName("t")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Temperature { get; set; }

    [JsonPropertyName("p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Humidity { get; set; }

    [JsonPropertyName("s")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? State { get; set; }
}

public class WeekSchedule: Dictionary<string, List<HourRecord>>
{
}
