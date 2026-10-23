namespace Hc.Pages.Groups;

/// <summary>
/// A run of hours with the same value and state, as the list shows it. <see cref="EndHour"/> is exclusive,
/// so 24 means midnight.
/// </summary>
public sealed record ScheduleInterval(int StartHour, int EndHour, int? Value, bool IsDeviceOn)
{
    public string TimeRange => $"{StartHour:00}:00 – {EndHour:00}:00";
}
