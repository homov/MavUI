using Hc.Pages.Devices;
using Hc.Pages.Groups;
using Hc.Pages.Settings;
using Hc.Pages.Shell;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Home;

/// <summary>
/// Signed-in start page: two tabs, groups and devices. Each list refreshes by pulling down. The header opens the
/// settings and signs out. Tapping a group opens its page.
/// </summary>
public partial class HomePage : MavContentPage
{
    private readonly GroupListView _groups = new();
    private readonly DeviceListView _devices = new();

    public HomePage()
    {
        InitializeComponent();

        _groups.GroupSelected += OnGroupSelected;
        _devices.DeviceSelected += OnDeviceSelected;
        Tabs.Add(MavLocalization.Get("home.tab.groups", "Groups"), MavIcons.Groups, content: _groups);
        Tabs.Add(MavLocalization.Get("home.tab.devices", "Devices"), MavIcons.Devices, content: _devices);

        AddAction(MavIcons.Settings, OnSettings, MavLocalization.Get("home.settings", "Settings"));
        AddAction(MavIcons.Logout, OnSignOut, MavLocalization.Get("home.signout", "Sign out"));
    }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    private void OnGroupSelected(object? sender, Hc.Models.Cloud.UserGroupInfo group) =>
        Navigator.Push(new GroupPage { Navigator = Navigator, Group = group });

    private void OnDeviceSelected(object? sender, Hc.Models.Cloud.UserDeviceInfo device) =>
        Navigator.Push(new Hc.Pages.Devices.DevicePage { Navigator = Navigator, Device = device });

    private void OnSettings() => Navigator.Push(new SettingsPage { Navigator = Navigator });

    private void OnSignOut()
    {
        AppServices.Default.Session.SignOut();
        Navigator.ShowSignIn();
    }
}
