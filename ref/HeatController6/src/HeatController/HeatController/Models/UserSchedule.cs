using HeatCeram.Api.Models;

namespace HeatController.Models;

public class UserSchedule
{
    public UserSchedule(Schedule schedule)
    {
        Schedule = schedule;
    }

    public Schedule Schedule { get; }

    public string ScheduleType
    {
        get {  return Schedule.ScheduleType; }
        set { Schedule.ScheduleType = value; }
    }

    public string ScheduleId => Schedule.ScheduleId;
}