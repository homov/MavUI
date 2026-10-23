using Mav;

namespace Hc.Pages.Groups;

/// <summary>
/// Name of a schedule type for the list. The type values are the ones the old app used: manual, time,
/// temperature, humidity. Any other value shows as "unknown", as before.
/// </summary>
public static class ScheduleTypeNames
{
    public static string Localize(string? scheduleType) => scheduleType switch
    {
        "manual" => MavLocalization.Get("schedule.manual", "Manual"),
        "time" => MavLocalization.Get("schedule.time", "Time"),
        "temperature" => MavLocalization.Get("schedule.temperature", "Temperature"),
        "humidity" => MavLocalization.Get("schedule.humidity", "Humidity"),
        _ => MavLocalization.Get("schedule.unknown", "Unknown"),
    };
}
