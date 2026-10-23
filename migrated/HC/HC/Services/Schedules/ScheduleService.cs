using Hc.Models.Cloud;
using Hc.Services.Cloud;

namespace Hc.Services.Schedules;

/// <summary>Weekly schedules: read, change the type, and save the full week.</summary>
public sealed class ScheduleService(CloudClient cloud)
{
    /// <summary>Returns the schedule, or null if the server has none with that id.</summary>
    public async Task<ScheduleInfo?> GetAsync(string scheduleId, CancellationToken ct = default)
    {
        var response = await cloud.GetSchedule(new ScheduleRequest(scheduleId), ct).ConfigureAwait(false);
        return CloudResults.Require(response).Schedule;
    }

    public async Task SetTypeAsync(string scheduleId, string scheduleType, CancellationToken ct = default)
    {
        var response = await cloud.SetScheduleType(new SetScheduleTypeRequest(scheduleId, scheduleType), ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }

    public async Task SaveAsync(ScheduleInfo schedule, CancellationToken ct = default)
    {
        var response = await cloud.UpdateSchedule(schedule, ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }
}
