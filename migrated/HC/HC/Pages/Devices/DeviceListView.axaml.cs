using Avalonia.Controls;
using Avalonia.Interactivity;
using Hc.Models.Cloud;
using Hc.Pages.Models;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Devices;

/// <summary>
/// The user's devices as a list: name, group name (when the device has a group), and an online icon.
/// The group name is looked up in the group list, so both lists are loaded together.
/// </summary>
public partial class DeviceListView : UserControl
{
    /// <summary>Value of <c>OnlineStatus</c> that means the device is connected (as in the cloud API).</summary>
    public const string OnlineValue = "online";

    /// <summary>Raised when a device is tapped. The page that owns the list opens the device page.</summary>
    public event EventHandler<UserDeviceInfo>? DeviceSelected;

    public DeviceListView()
    {
        InitializeComponent();
        Refresh.RefreshHandler = ReloadAsync;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e) => await ReloadAsync();

    /// <summary>Loads the devices and their group names again. A failure is shown in the banner.</summary>
    public async Task ReloadAsync()
    {
        Progress.IsVisible = true;
        ErrorBanner.Dismiss();
        try
        {
            var groups = await AppServices.Default.Locations.GetGroupsAsync();
            var devices = await AppServices.Default.Devices.GetDevicesAsync();
            Render(devices, groups);
        }
        catch (OperationCanceledException)
        {
            // The view was closed during the request.
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

    private void Render(IReadOnlyList<UserDeviceInfo> devices, IReadOnlyList<UserGroupInfo> groups)
    {
        ListHost.Children.Clear();
        EmptyState.IsVisible = devices.Count == 0;
        if (devices.Count == 0)
            return;

        var list = new MavList { ShowDividers = true };
        foreach (var device in devices)
        {
            // A device without a group has a null GroupId; it must not match a group that has none either.
            var groupName = device.GroupId is { Length: > 0 } groupId
                ? groups.FirstOrDefault(g => g.GroupId == groupId)?.Name
                : null;
            var icon = device.OnlineStatus == OnlineValue ? MavIcons.Wifi : MavIcons.WifiOff;
            var item = new MavListItem(device.DeviceName, groupName, icon);
            item.Clicked += (_, _) => DeviceSelected?.Invoke(this, device);
            list.AddItem(item);
        }

        ListHost.Children.Add(new MavCard(MavCardVariant.Outlined, list));
    }
}
