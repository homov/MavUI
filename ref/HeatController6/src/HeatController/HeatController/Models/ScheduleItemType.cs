using System.Linq;

namespace HeatController.Models;

public class ScheduleItemType
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    public static string GetScheduleTypeName(string schedule)
    {
        if (schedule == "manual")
            return Resources.ControlManual;
        else if (schedule == "time")
            return Resources.ControlScheduleTime;
        else if (schedule == "temperature")
            return Resources.ControlScheduleTemperature;
        return Resources.ControlUnknown;
    }

    //public static List<ScheduleItemType> GetScheduleItemTypes()
    //{
    //    return new List<ScheduleItemType>()
    //    {
    //        new ScheduleItemType() { Id = "manual", Name=Resources.ControlManual},
    //        new ScheduleItemType() { Id = "time", Name=Resources.ControlScheduleTime},
    //        new ScheduleItemType() { Id = "temperature", Name=Resources.ControlScheduleTemperature},
    //    };
    //}

    public static List<ScheduleItemType> GetScheduleItemTypes()
    {
        return new List<ScheduleItemType>()
        {
            new ScheduleItemType() { Id = "manual", Name=Resources.ControlManual},
            new ScheduleItemType() { Id = "time", Name=Resources.ControlScheduleTime},
            new ScheduleItemType() { Id = "temperature", Name=Resources.ControlScheduleTemperature},
            new ScheduleItemType() { Id = "humidity", Name=Resources.Humidity},
        };
    }

    //public static List<ScheduleItemType> GetScheduleItemTypes(string[] ids)
    //{
    //    if (ids == null)
    //        return new List<ScheduleItemType>();
    //    else if( ids.Contains("temperature"))
    //    {
    //        return GetScheduleItemTypes();
    //    }
    //    else if( ids.Contains("time"))
    //    {
    //        return new List<ScheduleItemType>()
    //        {
    //            new ScheduleItemType() { Id = "time", Name=Resources.ControlScheduleTime},
    //        };
    //    }
    //    return new List<ScheduleItemType>();
    //}

    public static List<ScheduleItemType> GetScheduleItemTypes(string[] ids)
    {
        var list = new List<ScheduleItemType>();
        if (ids == null || ids.Length == 0)
            return new List<ScheduleItemType>();

        list.Add(new ScheduleItemType() { Id = "manual", Name = Resources.ControlManual });

        if (ids.Contains("time"))
        {
            list.Add(new ScheduleItemType() { Id = "time", Name = Resources.ControlScheduleTime });
        }
        if (ids.Contains("temperature"))
        {
            list.Add(new ScheduleItemType() { Id = "temperature", Name = Resources.ControlScheduleTemperature });
        }
        if (ids.Contains("humidity"))
        {
            list.Add(new ScheduleItemType() { Id = "humidity", Name = Resources.Humidity });
        }
        return list;
    }
}