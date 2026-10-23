namespace HC.Api.Models;

public class GetScheduleResponse : BaseResponse
{
    public ScheduleWithReceiver? Schedule { get; set; }
}

public class Schedule
{
    [JsonPropertyName("schedule_id")]
    public string? ScheduleId { get; set; }

    [JsonPropertyName("schedule_type")]
    public string? ScheduleType { get; set; }

    [JsonPropertyName("schedule")]
    public WeekSchedule? WeekSchedule { get; set; }

    public static Schedule FromTemperature(float value)
    {
        value = (float)((int)value);
        
        var schedule = new Schedule()
        {
            ScheduleType = "manual",
            WeekSchedule = new WeekSchedule()
            {
                { "monday", new () { new HourRecord() { Hour = 0, Temperature = value }} },
                { "tuesday", new () { new HourRecord() { Hour = 0, Temperature = value }} },
                { "wednesday", new () { new HourRecord() { Hour = 0, Temperature = value }} },
                { "thursday", new () { new HourRecord() { Hour = 0, Temperature = value }} },
                { "friday", new () { new HourRecord() { Hour = 0, Temperature = value }} },
                { "saturday", new () { new HourRecord() { Hour = 0, Temperature = value }} },
                { "sunday", new () { new HourRecord() { Hour = 0, Temperature = value }} },
            }
        };
        return schedule;
    }
    
    public static Schedule FromHumidity(float value)
    {
        value = (float)((int)value);
        
        var schedule = new Schedule()
        {
            ScheduleType = "humidity",
            WeekSchedule = new WeekSchedule()
            {
                { "monday", new () { new HourRecord() { Hour = 0, Humidity = value }} },
                { "tuesday", new () { new HourRecord() { Hour = 0, Humidity = value }} },
                { "wednesday", new () { new HourRecord() { Hour = 0, Humidity = value }} },
                { "thursday", new () { new HourRecord() { Hour = 0, Humidity = value }} },
                { "friday", new () { new HourRecord() { Hour = 0, Humidity = value }} },
                { "saturday", new () { new HourRecord() { Hour = 0, Humidity = value }} },
                { "sunday", new () { new HourRecord() { Hour = 0, Humidity = value }} },
            }
        };
        return schedule;
    }
}

public class ScheduleWithReceiver : Schedule
{
    [JsonPropertyName("reciver_id")]
    public string? ReceiverId { get; set; }

    [JsonPropertyName("reciver_type")]
    public string? ReceiverType { get; set; }
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

public class WeekSchedule : Dictionary<string, List<HourRecord>>
{
}
