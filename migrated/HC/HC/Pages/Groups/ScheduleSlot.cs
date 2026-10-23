namespace Hc.Pages.Groups;

/// <summary>One hour of a day. <see cref="Value"/> is null for the time type, which keeps only the on/off state.</summary>
public sealed record ScheduleSlot(int? Value, bool IsDeviceOn);
