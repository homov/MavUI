using Hc.Models.Cloud;

namespace Hc.Pages.Groups;

/// <summary>
/// The schedule as the editor works with it: 24 hour slots for each weekday. The server's hour records are read
/// into it and written back from it. Manual and humidity keep one value all week. Time and temperature have
/// intervals for each day.
/// </summary>
public sealed class ScheduleDraft
{
    public const string Manual = "manual";
    public const string Time = "time";
    public const string Temperature = "temperature";
    public const string Humidity = "humidity";

    /// <summary>Types in the order the control offers them.</summary>
    public static readonly string[] Types = [Manual, Time, Temperature, Humidity];

    public const int HoursPerDay = 24;
    public const int DefaultTemperature = 24;
    public const int DefaultHumidity = 55;

    private readonly ScheduleSlot[][] _days;

    private ScheduleDraft(string type, int defaultValue)
    {
        Type = type;
        DefaultValue = defaultValue;
        _days = new ScheduleSlot[WeekDays.All.Length][];
        for (var d = 0; d < _days.Length; d++)
            _days[d] = Enumerable.Repeat(new ScheduleSlot(ValueFor(type, defaultValue), false), HoursPerDay).ToArray();
    }

    /// <summary>The type: one of <see cref="Types"/>.</summary>
    public string Type { get; private set; }

    /// <summary>The value of a constant schedule (manual or humidity).</summary>
    public int DefaultValue { get; private set; }

    /// <summary>Manual and humidity: one value for the whole week, no intervals.</summary>
    public bool IsConstant => Type is Manual or Humidity;

    /// <summary>Time and temperature: intervals for each day.</summary>
    public bool HasIntervals => !IsConstant;

    /// <summary>Whether the type has a value (everything except time).</summary>
    public bool HasValue => Type != Time;

    public int MinimumValue => Type == Manual ? 0 : 8;

    public int MaximumValue => Type == Manual ? 60 : 32;

    /// <summary>Reads a schedule from the server. A null schedule gives the default for the manual type.</summary>
    public static ScheduleDraft From(ScheduleInfo? info)
    {
        var type = info?.ScheduleType is { } name && Types.Contains(name) ? name : Manual;
        var draft = new ScheduleDraft(type, DefaultFor(type));
        var week = info?.Week;
        if (week is null)
            return draft;

        int? constantValue = null;
        for (var d = 0; d < WeekDays.All.Length; d++)
        {
            if (!week.TryGetValue(WeekDays.All[d], out var records) || records.Count == 0)
                continue;

            var ordered = records.OrderBy(r => r.Hour ?? 0).ToList();
            for (var h = 0; h < HoursPerDay; h++)
            {
                // Each hour takes the last record that starts at or before it; hours before the first record take the first.
                var record = ordered.LastOrDefault(r => (r.Hour ?? 0) <= h) ?? ordered[0];
                var value = ValueOf(type, record);
                draft._days[d][h] = new ScheduleSlot(value, record.State == 1);
                constantValue ??= value;
            }
        }

        if (draft.IsConstant && constantValue is { } fromServer)
            draft.DefaultValue = fromServer;
        return draft;
    }

    /// <summary>Hour intervals of one day (0 = Monday), with equal neighbouring hours merged.</summary>
    public IReadOnlyList<ScheduleInterval> IntervalsOf(int day)
    {
        var slots = _days[day];
        var result = new List<ScheduleInterval>();
        var start = 0;
        for (var h = 1; h <= HoursPerDay; h++)
        {
            if (h == HoursPerDay || slots[h] != slots[start])
            {
                result.Add(new ScheduleInterval(start, h, slots[start].Value, slots[start].IsDeviceOn));
                start = h;
            }
        }
        return result;
    }

    /// <summary>
    /// Sets the hours from <paramref name="start"/> (inclusive) to <paramref name="end"/> (exclusive) of a day.
    /// The other hours keep their values, so the neighbouring intervals are split. Returns false for an empty
    /// or out-of-range interval.
    /// </summary>
    public bool Paint(int day, int start, int end, int? value, bool isDeviceOn)
    {
        if (day < 0 || day >= _days.Length || start < 0 || end > HoursPerDay || start >= end)
            return false;

        for (var h = start; h < end; h++)
            _days[day][h] = new ScheduleSlot(Type == Time ? null : value, isDeviceOn);
        return true;
    }

    /// <summary>Sets the value of a constant schedule for the whole week.</summary>
    public void SetDefaultValue(int value)
    {
        if (!IsConstant)
            return;

        DefaultValue = value;
        FillAll(new ScheduleSlot(value, false));
    }

    /// <summary>Changes the type. Values that the new type does not have are reset; hours are kept.</summary>
    public void SetType(string type)
    {
        if (type == Type || !Types.Contains(type))
            return;

        Type = type;
        DefaultValue = DefaultFor(type);
        if (IsConstant)
        {
            FillAll(new ScheduleSlot(DefaultValue, false));
            return;
        }

        for (var d = 0; d < _days.Length; d++)
            for (var h = 0; h < HoursPerDay; h++)
            {
                var slot = _days[d][h];
                _days[d][h] = Type == Time ? slot with { Value = null } : slot with { Value = slot.Value ?? DefaultValue };
            }
    }

    /// <summary>Builds the schedule to send to the server, in the same form the old app used.</summary>
    public ScheduleInfo ToInfo(string? scheduleId)
    {
        var week = new Dictionary<string, List<HourRecord>>();
        for (var d = 0; d < WeekDays.All.Length; d++)
        {
            var records = new List<HourRecord>();
            if (IsConstant)
            {
                records.Add(Record(0, DefaultValue, false));
            }
            else
            {
                foreach (var interval in IntervalsOf(d))
                    records.Add(Record(interval.StartHour, interval.Value, interval.IsDeviceOn));
            }
            week[WeekDays.All[d]] = records;
        }
        return new ScheduleInfo { ScheduleId = scheduleId, ScheduleType = Type, Week = week };
    }

    private HourRecord Record(int hour, int? value, bool isOn) => Type switch
    {
        // Constant types send only their value. Manual has no state field, as in the old app.
        Humidity => new HourRecord { Hour = hour, Humidity = value },
        Manual => new HourRecord { Hour = hour, Temperature = value },
        Temperature => new HourRecord { Hour = hour, Temperature = value, State = isOn ? 1 : 0 },
        _ => new HourRecord { Hour = hour, State = isOn ? 1 : 0 },
    };

    private void FillAll(ScheduleSlot slot)
    {
        for (var d = 0; d < _days.Length; d++)
            Array.Fill(_days[d], slot);
    }

    private static int? ValueOf(string type, HourRecord record) => type switch
    {
        Time => null,
        Humidity => (int?)record.Humidity,
        _ => (int?)record.Temperature,
    };

    private static int? ValueFor(string type, int value) => type == Time ? (int?)null : value;

    private static int DefaultFor(string type) => type == Humidity ? DefaultHumidity : DefaultTemperature;
}
