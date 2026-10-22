using Autofac.Core.Lifetime;
using HeatCeram.Api.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.CommunityToolkit.Helpers;

namespace HeatController.Models;

public partial class ScheduleValueModel : ObservableObject
{
    [ObservableProperty]
    bool isDeviceOn = false;

    [ObservableProperty]
    int? temperature = 0;

    [ObservableProperty]
    int? humidity = 0;
}

public partial class ScheduleHourModel : ScheduleValueModel
{
    [ObservableProperty]
    int startHour = 0;

    [ObservableProperty]
    int endHour;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHourEditEnabled))]
    ScheduleModel scheduleModel;

    public bool IsHourEditEnabled => ScheduleModel.ScheduleType != ScheduleTypes.Humidity;

    public int? Value
    {
        get
        {
            if (scheduleModel.ScheduleType == ScheduleTypes.Humidity)
                return this.Humidity;
            return this.Temperature;
        }

        set
        {
            if (scheduleModel.ScheduleType == ScheduleTypes.Humidity)
                this.Humidity = value;
            else
                this.Temperature = value;   
            this.OnPropertyChanged(nameof(Value));
            this.OnPropertyChanged(nameof(DisplayValue));
        }
    }

    public string StringFormat => scheduleModel.ControlValueFormat;
    
    public string DisplayValue
    {
        get => String.Format(StringFormat, Value);
    }
}


public partial class ScheduleWeekDayModel : ObservableObject
{
    public string WeekDay { get; set; }
    public string WeekDayName => LocalizationResourceManager.Current[WeekDay];
    public ObservableCollection<ScheduleHourModel> Hours { get; } = new ObservableCollection<ScheduleHourModel>();
    public ScheduleModel ScheduleModel { get; }

    public bool IsHourEditEnabled => ScheduleModel.ScheduleType != ScheduleTypes.Humidity;

    public ScheduleWeekDayModel(ScheduleModel scheduleModel)
    {
        ScheduleModel = scheduleModel;
    }
    public IEnumerable<ScheduleHourModel> GetListByHour()
    {
        foreach( var item in Hours)
        {
            for( int i=item.StartHour; i<item.EndHour; i++)
                yield return new ScheduleHourModel() 
                { 
                    StartHour=i, 
                    EndHour=i+1, 
                    Temperature=item.Temperature, 
                    Humidity =item.Humidity,
                    IsDeviceOn=item.IsDeviceOn,
                    ScheduleModel = item.ScheduleModel
                };
        }
    }

    public void UpdateHours(int start, int end, ScheduleHourModel model)
    {
        var forRemove = Hours.Where(it => it != model && it.StartHour >= start && it.EndHour <= end).ToList();
        forRemove.ForEach(it => Hours.Remove(it));
        
        var thisIndex = Hours.IndexOf(model);
        ScheduleHourModel prev = thisIndex > 0 ? Hours[thisIndex - 1] : null;
        ScheduleHourModel next = thisIndex < Hours.Count - 1 ? Hours[thisIndex + 1] : null;

        if (prev == null)
        {
            if (start > 0)
            {
                Hours.Insert(0, prev = new ScheduleHourModel()
                {
                    StartHour = 0,
                    EndHour = start,
                    Temperature = model.Temperature,
                    Humidity = model.Humidity,
                    IsDeviceOn = model.IsDeviceOn,
                    ScheduleModel = this.ScheduleModel
                });
            }
        }
        else
            prev.EndHour = start;

        if (next == null)
        {
            if (end < 24)
            {
                Hours.Add( prev = new ScheduleHourModel()
                {
                    StartHour = end,
                    EndHour = 24,
                    Temperature = model.Temperature,
                    Humidity = model.Humidity,
                    IsDeviceOn = model.IsDeviceOn,
                    ScheduleModel = this.ScheduleModel
                });
            }
        }
        else
            next.StartHour = end;
        model.StartHour = start;
        model.EndHour = end;
    }
}

public static class ScheduleTypes
{
    public const string Manual = "manual";
    public const string Temperature = "temperature";
    public const string Humidity = "humidity";
}

public partial class ScheduleModel : ObservableObject
{
    [ObservableProperty]
    string scheduleId;

    [ObservableProperty]
    string scheduleType;

    [ObservableProperty]
    int? defaultValue=24;

    [ObservableProperty]
    int? minValue=8;

    [ObservableProperty]
    int? maxValue=32;

    [ObservableProperty]
    string controlValueName;

    [ObservableProperty]
    string controlValueFormat;

    public ObservableCollection<ScheduleWeekDayModel> Days { get; } = new ObservableCollection<ScheduleWeekDayModel>();

    public ScheduleWeekDayModel Mon { get; }
    public ScheduleWeekDayModel Tue { get; }
    public ScheduleWeekDayModel Wed { get; }
    public ScheduleWeekDayModel Thu { get; }
    public ScheduleWeekDayModel Fri { get; }
    public ScheduleWeekDayModel Sat { get; }
    public ScheduleWeekDayModel Sun { get; }

    public bool IsManual => ScheduleType == ScheduleTypes.Manual || ScheduleType == ScheduleTypes.Humidity;
    public bool IsTemperature => ScheduleType == ScheduleTypes.Temperature;
    public bool IsHumidity => ScheduleType == ScheduleTypes.Humidity;
    public bool IsNotManual => !IsManual;

    public bool IsValueControl => IsTemperature || IsHumidity;


    public ScheduleModel()
    {
        Days.Add(Mon=new ScheduleWeekDayModel(this) { WeekDay = Weeks.Monday });
        Days.Add(Tue=new ScheduleWeekDayModel(this) { WeekDay = Weeks.Tuesday });
        Days.Add(Wed=new ScheduleWeekDayModel(this) { WeekDay = Weeks.Wednesday });
        Days.Add(Thu=new ScheduleWeekDayModel(this) { WeekDay = Weeks.Thursday });
        Days.Add(Fri=new ScheduleWeekDayModel(this) { WeekDay = Weeks.Friday });
        Days.Add(Sat=new ScheduleWeekDayModel(this) { WeekDay = Weeks.Saturday });
        Days.Add(Sun=new ScheduleWeekDayModel(this) { WeekDay = Weeks.Sunday });
    }

    partial void OnScheduleTypeChanged(string newType)
    {
        if (newType == "humidity")
        {
            this.DefaultValue = 55;
            this.ControlValueName = Resources.Humidity;
            this.ControlValueFormat = "{0}%";
        }
        else
        {
            this.DefaultValue = 24;
            this.ControlValueName = Resources.Temperature;
            this.ControlValueFormat = "{0}°C";
        }
        this.OnPropertyChanged(nameof(IsHumidity));
        this.OnPropertyChanged(nameof(IsTemperature));
        this.OnPropertyChanged(nameof(IsManual));
        this.OnPropertyChanged(nameof(IsNotManual));
        this.OnPropertyChanged(nameof(IsValueControl));
    }

    public ScheduleWeekDayModel WeekDay(string name)
    {
        return this.Days.First(it => it.WeekDay == name);
    }

    public void ClearHours()
    {
        foreach (var day in Days)
            day.Hours.Clear();
    }

    public Schedule ToSchedule()
    {
        bool isHumidity = this.IsHumidity;

        var schedule = new Schedule()
        {
            ScheduleId = this.ScheduleId,
            ScheduleType = this.ScheduleType,
            WeekSchedule = new WeekSchedule()
        };
        if (this.IsManual)
        {
            foreach( var day in Days)
            {
                schedule.WeekSchedule[day.WeekDay] = new List<HourRecord>()
                {
                    isHumidity ?
                        new HourRecord() { Hour = 0, Humidity = (int?)this.DefaultValue }
                        :
                        new HourRecord() { Hour = 0, Temperature = (int?)this.DefaultValue}
                };
            }
        }
        else
        {
            foreach (var day in Days)
            {
                var hours = new List<HourRecord>();
                foreach( var hour in day.Hours)
                {
                    if (!isHumidity)
                    {
                        hours.Add(new HourRecord()
                        {
                            Hour = hour.StartHour,
                            Temperature = hour.Temperature,
                            State = hour.IsDeviceOn ? 1 : 0
                        });
                    }
                    else
                    {
                        hours.Add(new HourRecord()
                        {
                            Hour = hour.StartHour,
                            Humidity = hour.Humidity,
                        });
                    }
                }
                schedule.WeekSchedule[day.WeekDay] = hours;
            }
        }
        return schedule;
    }


    public static ScheduleModel FromSchedule(Schedule userSchedule)
    {
        var newSchedule = new ScheduleModel();

        newSchedule.ScheduleId = userSchedule.ScheduleId;
        newSchedule.ScheduleType = userSchedule.ScheduleType;

        bool isHumidity = newSchedule.IsHumidity;
        foreach (var item in userSchedule.WeekSchedule)
        {
            var day = newSchedule.WeekDay(item.Key);
            ScheduleHourModel last = null;
            foreach (var hourItem in item.Value.OrderBy(it => it.Hour))
            {
                var hour = new ScheduleHourModel()
                {
                    Temperature = (int?)hourItem.Temperature,
                    Humidity = (int?)hourItem.Humidity,
                    IsDeviceOn = hourItem.State == 1,
                    StartHour = hourItem.Hour.Value,
                    EndHour = 24,
                    ScheduleModel = newSchedule

                };
                day.Hours.Add(hour);
                if (last != null)
                    last.EndHour = hour.StartHour;
                last = hour;
            }
        }
        if (newSchedule.IsManual)
        {
            var day = newSchedule.Days.FirstOrDefault();
            if (day != null && day.Hours!=null && day.Hours.Any())
            {
                var hour = day.Hours.FirstOrDefault();
                if (hour != null) 
                {
                    if (newSchedule.IsHumidity)
                        newSchedule.DefaultValue = hour.Humidity;
                    else
                        newSchedule.DefaultValue = hour.Temperature;
                }
            }
        }
        return newSchedule;
    }

    public static int GetMinimumValue(string scheduleType)
    {
        if (scheduleType == ScheduleTypes.Manual)
            return 0;
        return 8;
    }

    public static int GetMaximumValue(string scheduleType)
    {
        if (scheduleType == ScheduleTypes.Manual)
            return 60;
        return 32;
    }

}
