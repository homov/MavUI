using Avalonia.Interactivity;
using Hc.Models.Cloud;
using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>
/// A group's schedule, as in the old app's group page: the control type, the week by day, and the intervals of the
/// day (time and temperature) or the one value (manual and humidity). The whole schedule is saved at once.
/// </summary>
public partial class GroupPage : MavContentPage
{
    private static readonly (string Key, string Fallback)[] DayNames =
    [
        ("schedule.day.mon", "Mon"), ("schedule.day.tue", "Tue"), ("schedule.day.wed", "Wed"),
        ("schedule.day.thu", "Thu"), ("schedule.day.fri", "Fri"), ("schedule.day.sat", "Sat"),
        ("schedule.day.sun", "Sun"),
    ];

    private ScheduleDraft _draft = ScheduleDraft.From(null);
    private int _day;

    /// <summary>While the page fills its controls, their change events do not change the schedule.</summary>
    private bool _rendering;

    public GroupPage()
    {
        InitializeComponent();

        foreach (var (key, fallback) in DayNames)
            DaySegments.AddSegment(MavLocalization.Get(key, fallback));

        DaySegments.SelectedIndexChanged += OnDayChanged;
        TypeSelect.SelectedIndexChanged += OnTypeChanged;
        DefaultValueBox.ValueChanged += OnDefaultValueChanged;
        AddIntervalButton.Clicked += (_, _) => OpenEditor(null);
        SaveButton.Clicked += OnSave;
        Loaded += OnLoaded;
    }

    /// <summary>The group whose schedule is shown. Set before the page is shown.</summary>
    public required Hc.Models.Cloud.UserGroupInfo Group { get; init; }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Title = Group.Name;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Progress.IsVisible = true;
        ErrorBanner.Dismiss();
        try
        {
            var scheduleId = Group.Schedule;
            var info = string.IsNullOrEmpty(scheduleId) ? null : await AppServices.Default.Schedules.GetAsync(scheduleId);
            _draft = ScheduleDraft.From(info);
            Render();
        }
        catch (OperationCanceledException)
        {
            // The page was closed during the request.
        }
        catch (Exception ex)
        {
            ErrorBanner.Message = ErrorText.From(ex);
            ErrorBanner.Show();
        }
        finally
        {
            Progress.IsVisible = false;
        }
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

    private string Describe(ScheduleInterval interval)
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

    private async void OnSave(object? sender, EventArgs e) =>
        await PageRunner.RunAsync(SaveButton, ErrorBanner, async () =>
        {
            await AppServices.Default.Schedules.SaveAsync(_draft.ToInfo(Group.Schedule));
        });
}
