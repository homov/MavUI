using HeatCeram.Api.Models;
using System.ComponentModel;
using System.Linq;

namespace HeatController.Models;

public partial class UserLocation: ObservableObject
{
	public UserLocation( UserGroupInfo info )
	{
		Info = info;
	}

    public UserLocation( UserGroupInfo info, Schedule schedule ):this( info )
    {
        Schedule = schedule;
    }

    [ObservableProperty] UserGroupInfo info;
    [ObservableProperty] Schedule schedule = null;
    public string ScheduleTypeName => ScheduleItemType.GetScheduleTypeName(Schedule?.ScheduleType);
    public List<ScheduleItemType> ValidScheduleTypes => ScheduleItemType.GetScheduleItemTypes();
    public string[] ValidScheduleNames => ValidScheduleTypes.Select(it => it.Name).ToArray();

    public string Name => Info.Name;
}
