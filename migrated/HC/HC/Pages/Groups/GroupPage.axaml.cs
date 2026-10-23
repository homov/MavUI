using Avalonia.Interactivity;
using Hc.Models.Cloud;
using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>
/// A group's page with two tabs: the schedule (<see cref="GroupScheduleTab"/>) and the properties
/// (<see cref="GroupPropertiesTab"/>: name, sensor, delete). Each tab saves with its own button.
/// </summary>
public partial class GroupPage : MavContentPage
{
    private readonly GroupScheduleTab _schedule = new();
    private readonly GroupPropertiesTab _properties = new();

    public GroupPage()
    {
        InitializeComponent();

        Tabs.Add(MavLocalization.Get("group.tab.schedule", "Schedule"), MavIcons.Schedule, content: _schedule);
        Tabs.Add(MavLocalization.Get("group.tab.properties", "Properties"), MavIcons.Settings, content: _properties);

        _schedule.SaveButton.Clicked += OnSaveSchedule;
        _properties.SaveButton.Clicked += OnSaveProperties;
        _properties.DeleteButton.Clicked += OnDelete;
        Loaded += OnLoaded;
    }

    /// <summary>The group shown. Set before the page is shown.</summary>
    public required UserGroupInfo Group { get; init; }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    /// <summary>Raised after the name, the sensor or the existence of the group changes.</summary>
    public event EventHandler? GroupChanged;

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
            var services = AppServices.Default;
            var scheduleId = Group.Schedule;
            var info = string.IsNullOrEmpty(scheduleId) ? null : await services.Schedules.GetAsync(scheduleId);
            var devices = await services.Devices.GetDevicesAsync();

            _schedule.Show(ScheduleDraft.From(info));
            _properties.Show(Group, devices.Where(d => d.IsSensor).ToList());
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

    private async void OnSaveSchedule(object? sender, EventArgs e) =>
        await PageRunner.RunAsync(_schedule.SaveButton, ErrorBanner, async () =>
        {
            await AppServices.Default.Schedules.SaveAsync(_schedule.Draft.ToInfo(Group.Schedule));
        });

    /// <summary>Saves only what changed: the name (its own request), then the sensor (its own request).</summary>
    private async void OnSaveProperties(object? sender, EventArgs e)
    {
        if (!_properties.Validate())
            return;

        await PageRunner.RunAsync(_properties.SaveButton, ErrorBanner, async () =>
        {
            var groupId = Group.GroupId;
            if (groupId is null)
                return;

            var services = AppServices.Default;
            var changed = false;
            try
            {
                var name = _properties.GroupName;
                if (name != Group.Name)
                {
                    await services.Locations.RenameAsync(groupId, name);
                    Group.Name = name;
                    Title = name;
                    changed = true;
                }

                var sensorId = _properties.SensorId;
                var current = string.IsNullOrEmpty(Group.ExternalSensorId) ? null : Group.ExternalSensorId;
                if (sensorId != current)
                {
                    await services.Locations.SetSensorAsync(groupId, sensorId);
                    Group.ExternalSensorId = sensorId ?? string.Empty;
                    changed = true;
                }
            }
            finally
            {
                // A change that went through is shown in the list even if a later request failed.
                if (changed)
                    GroupChanged?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private async void OnDelete(object? sender, EventArgs e)
    {
        var confirmed = await MavDialogService.Instance.ShowConfirmAsync(
            MavLocalization.Get("common.confirmation", "Confirmation"),
            MavLocalization.Get("group.delete.confirm", "Delete group. Proceed?"),
            MavLocalization.Get("group.delete", "Delete group"),
            MavLocalization.Get("common.cancel", "Cancel"));
        if (!confirmed)
            return;

        await PageRunner.RunAsync(_properties.DeleteButton, ErrorBanner, async () =>
        {
            var groupId = Group.GroupId;
            if (groupId is null)
                return;

            await AppServices.Default.Locations.RemoveAsync(groupId);
            GroupChanged?.Invoke(this, EventArgs.Empty);
            Navigator.Pop();
        });
    }
}
