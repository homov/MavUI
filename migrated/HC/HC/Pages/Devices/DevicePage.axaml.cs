using Avalonia.Interactivity;
using Hc.Models.Cloud;
using Hc.Pages.Groups;
using Hc.Pages.Models;
using Hc.Pages.Shell;
using Hc.Services;
using Mav.Controls;

namespace Hc.Pages.Devices;

/// <summary>
/// A device's page: its schedule, edited with <see cref="ScheduleEditorView"/>. As in the old app, a sensor has no
/// schedule of its own, and a device in a group takes the group's schedule, so the editor is hidden for both.
/// </summary>
public partial class DevicePage : MavContentPage
{
    private string? _scheduleId;
    private string _savedType = ScheduleDraft.Manual;

    public DevicePage()
    {
        InitializeComponent();

        SaveButton.Clicked += OnSave;
        Loaded += OnLoaded;
    }

    /// <summary>The device from the list. Set before the page is shown; the page reloads it for fresh data.</summary>
    public required UserDeviceInfo Device { get; init; }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    /// <summary>
    /// Whether the device's own schedule can be changed: it is not a sensor, and it is not controlled by a group.
    /// The rule is the old app's <c>CanSetControl</c>: a device counts as in a group only when the group work
    /// flag is on and the device has a group id.
    /// </summary>
    public static bool CanControl(UserDeviceInfo device) =>
        !device.IsSensor && (!device.GroupWork || string.IsNullOrEmpty(device.GroupId));

    /// <summary>
    /// The schedule types the device offers: manual always, and time, temperature or humidity when the device's
    /// control types include them (as the old app did).
    /// </summary>
    public static string[] AllowedTypes(UserDeviceInfo device) =>
        ScheduleDraft.Types.Where(t => t == ScheduleDraft.Manual || device.ControlTypes.Contains(t)).ToArray();

    private async void OnLoaded(object? sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        Progress.IsVisible = true;
        ErrorBanner.Dismiss();
        try
        {
            var services = AppServices.Default;
            var device = await services.Devices.GetDeviceAsync(Device.DeviceId);
            Title = device.DeviceName;

            var types = await services.Devices.GetDeviceTypesAsync();
            var deviceType = types.FirstOrDefault(t => t.Name == device.DeviceType);

            if (!CanControl(device))
            {
                ShowInfo(MavLocalization.Get("device.group.controlled", "The device is controlled by the group schedule"));
                return;
            }

            if (string.IsNullOrEmpty(device.ScheduleId))
            {
                ShowInfo(MavLocalization.Get("device.schedule.none", "This device has no schedule."));
                return;
            }

            _scheduleId = device.ScheduleId;
            var info = await services.Schedules.GetAsync(_scheduleId);
            var draft = ScheduleDraft.From(info);
            draft.SetLimits(deviceType);
            _savedType = draft.Type;

            Editor.Show(draft, AllowedTypes(device));
            Editor.IsVisible = true;
            SaveButton.IsVisible = true;
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

    private void ShowInfo(string text)
    {
        InfoBanner.Message = text;
        InfoBanner.Show();
        Editor.IsVisible = false;
        SaveButton.IsVisible = false;
    }

    /// <summary>Saves the type first when it changed (its own request, as in the old app), then the whole week.</summary>
    private async void OnSave(object? sender, EventArgs e) =>
        await PageRunner.RunAsync(SaveButton, ErrorBanner, async () =>
        {
            if (_scheduleId is null)
                return;

            var services = AppServices.Default;
            var draft = Editor.Draft;
            if (draft.Type != _savedType)
            {
                await services.Schedules.SetTypeAsync(_scheduleId, draft.Type);
                _savedType = draft.Type;
            }
            await services.Schedules.SaveAsync(draft.ToInfo(_scheduleId));
        });
}
