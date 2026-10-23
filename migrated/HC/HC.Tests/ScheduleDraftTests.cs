using Hc.Models.Cloud;
using Hc.Pages.Groups;
using Xunit;

namespace Hc.Tests;

public class ScheduleDraftTests
{
    [Fact]
    public void Temperature_ReadsIntervalsOfTheDay()
    {
        var info = new ScheduleInfo
        {
            ScheduleType = ScheduleDraft.Temperature,
            Week = new() { [WeekDays.Monday] = [new HourRecord { Hour = 0, Temperature = 18, State = 1 }, new HourRecord { Hour = 6, Temperature = 22, State = 0 }] },
        };

        var intervals = ScheduleDraft.From(info).IntervalsOf(0);

        Assert.Equal(2, intervals.Count);
        Assert.Equal(new ScheduleInterval(0, 6, 18, true), intervals[0]);
        Assert.Equal(new ScheduleInterval(6, 24, 22, false), intervals[1]);
    }

    [Fact]
    public void Paint_SplitsTheIntervalAroundTheNewRange()
    {
        var draft = ScheduleDraft.From(new ScheduleInfo
        {
            ScheduleType = ScheduleDraft.Temperature,
            Week = new() { [WeekDays.Monday] = [new HourRecord { Hour = 0, Temperature = 18, State = 0 }] },
        });

        Assert.True(draft.Paint(0, 3, 8, 25, true));

        var intervals = draft.IntervalsOf(0);
        Assert.Equal(3, intervals.Count);
        Assert.Equal(new ScheduleInterval(0, 3, 18, false), intervals[0]);
        Assert.Equal(new ScheduleInterval(3, 8, 25, true), intervals[1]);
        Assert.Equal(new ScheduleInterval(8, 24, 18, false), intervals[2]);
    }

    [Fact]
    public void Paint_RejectsEmptyAndOutOfRangeIntervals()
    {
        var draft = ScheduleDraft.From(null);

        Assert.False(draft.Paint(0, 5, 5, 20, false));
        Assert.False(draft.Paint(0, 20, 25, 20, false));
        Assert.False(draft.Paint(7, 0, 1, 20, false));
    }

    [Fact]
    public void Manual_IsOneValueForTheWeek_AndOneRecordPerDay()
    {
        var draft = ScheduleDraft.From(new ScheduleInfo
        {
            ScheduleType = ScheduleDraft.Manual,
            Week = new() { [WeekDays.Monday] = [new HourRecord { Hour = 0, Temperature = 20 }] },
        });

        Assert.True(draft.IsConstant);
        var info = draft.ToInfo("schedule-1");

        Assert.Equal("schedule-1", info.ScheduleId);
        Assert.Equal(7, info.Week!.Count);
        Assert.All(info.Week.Values, records => Assert.Single(records));
        Assert.Equal(20, info.Week[WeekDays.Friday][0].Temperature);
    }

    [Fact]
    public void Time_WritesOnlyTheStateNotATemperature()
    {
        var draft = ScheduleDraft.From(new ScheduleInfo { ScheduleType = ScheduleDraft.Time });
        draft.Paint(0, 0, 12, null, true);
        draft.Paint(0, 12, 24, null, false);

        var records = draft.ToInfo(null).Week![WeekDays.Monday];

        Assert.Equal(2, records.Count);
        Assert.All(records, record => Assert.Null(record.Temperature));
        Assert.Equal(1, records[0].State);
        Assert.Equal(0, records[1].State);
    }
}
