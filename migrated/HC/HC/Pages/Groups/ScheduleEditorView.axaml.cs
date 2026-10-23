using Avalonia.Controls;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>
/// The schedule editor of the old app's group and device pages: the control type, the week by day, and the
/// intervals of the day (time and temperature) or the one value (manual and humidity). The page saves
/// <see cref="Draft"/> with its own button.
/// </summary>
public partial class ScheduleEditorView : UserControl
{
    private static readonly (string Key, string Fallback)[] DayNames =
    [
        ("schedule.day.mon", "Mon"), ("schedule.day.tue", "Tue"), ("schedule.day.wed", "Wed"),
        ("schedule.day.thu", "Thu"), ("schedule.day.fri", "Fri"), ("schedule.day.sat", "Sat"),
        ("schedule.day.sun", "Sun"),
    ];

    private ScheduleDraft _draft = ScheduleDraft.From(null);
    private int _day;

    /// <summary>While the controls are filled, their change events do not change the schedule.</summary>
    private bool _rendering;

    public ScheduleEditorView()
    {
        InitializeComponent();

        foreach (var (key, fallback) in DayNames)
            DaySegments.AddSegment(MavLocalization.Get(key, fallback));

        DaySegments.SelectedIndexChanged += OnDayChanged;
        TypeSelect.SelectedIndexChanged += OnTypeChanged;
        DefaultValueBox.ValueChanged += OnDefaultValueChanged;
        AddIntervalButton.Clicked += (_, _) => OpenEditor(null);
    }

    /// <summary>The schedule being edited. The page reads it to save.</summary>
    public ScheduleDraft Draft => _draft;

    /// <summary>Shows a schedule in the editor.</summary>
    public void Show(ScheduleDraft draft)
    {
        _draft = draft;
        Render();
    }

    private void Render()
    {
        _rendering = true;
        try
        {
            TypeSelect.Options.Clear();
            foreach (var type in ScheduleDraft.Types)
                TypeSelect.AddOption(ScheduleTypeNames.Localize(type));
            TypeSelect.SelectedIndex = Array.IndexOf(ScheduleDraft.Types, _draft.Type);

            DaySegments.IsVisible = _draft.HasIntervals;
            DaySegments.SelectedIndex = _day;

            DefaultValueBox.IsVisible = _draft.IsConstant;
            DefaultValueBox.Minimum = _draft.MinimumValue;
            DefaultValueBox.Maximum = _draft.MaximumValue;
            DefaultValueBox.Value = _draft.DefaultValue;

            AddIntervalButton.IsVisible = _draft.HasIntervals;
            RenderDay();
        }
        finally
        {
            _rendering = false;
        }
    }

    private void RenderDay()
    {
        IntervalHost.Children.Clear();
        if (!_draft.HasIntervals)
            return;

        var list = new MavList { ShowDividers = true };
        foreach (var interval in _draft.IntervalsOf(_day))
        {
            var item = new MavListItem(interval.TimeRange, Describe(interval), MavIcons.Schedule);
            item.Clicked += (_, _) => OpenEditor(interval);
            list.AddItem(item);
        }
        IntervalHost.Children.Add(new MavCard(MavCardVariant.Outlined, list));
    }

    private static string Describe(ScheduleInterval interval)
    {
        var state = interval.IsDeviceOn
            ? MavLocalization.Get("schedule.on", "On")
            : MavLocalization.Get("schedule.off", "Off");
        return interval.Value is { } value ? $"{value}°C · {state}" : state;
    }

    private void OnDayChanged(object? sender, int index)
    {
        if (_rendering || index < 0)
            return;

        _day = index;
        RenderDay();
    }

    private void OnTypeChanged(object? sender, int index)
    {
        if (_rendering || index < 0 || index >= ScheduleDraft.Types.Length)
            return;

        _draft.SetType(ScheduleDraft.Types[index]);
        Render();
    }

    private void OnDefaultValueChanged(object? sender, double value)
    {
        if (_rendering || !_draft.IsConstant)
            return;

        _draft.SetDefaultValue((int)value);
    }

    /// <summary>Opens the editor for an interval, or for a new interval when <paramref name="interval"/> is null.</summary>
    private void OpenEditor(ScheduleInterval? interval)
    {
        var editor = new IntervalEditorView();
        editor.Configure(_draft, interval);

        MavBottomSheet? sheet = null;
        editor.Applied += (_, _) =>
        {
            var (start, end, value, isOn) = editor.Read();
            if (end <= start)
            {
                editor.ShowError(MavLocalization.Get("schedule.range.invalid", "The end must be after the start."));
                return;
            }

            _draft.Paint(_day, start, end, value, isOn);
            sheet?.Close();
            RenderDay();
        };

        sheet = MavBottomSheet.ShowCurrent(MavLocalization.Get("schedule.interval.title", "Interval"), editor);
    }
}
