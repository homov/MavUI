using Hc.Pages.Devices;
using Hc.Pages.Groups;
using Hc.Pages.Shell;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Home;

/// <summary>Signed-in start page: two tabs, groups and devices. Refresh reloads both; sign-out is in the header.</summary>
public partial class HomePage : MavContentPage
{
    private readonly GroupListView _groups = new();
    private readonly DeviceListView _devices = new();

    public HomePage()
    {
        InitializeComponent();

        Tabs.Add(MavLocalization.Get("home.tab.groups", "Groups"), MavIcons.Groups, content: _groups);
        Tabs.Add(MavLocalization.Get("home.tab.devices", "Devices"), MavIcons.Devices, content: _devices);

        AddAction(MavIcons.Refresh, OnRefresh, MavLocalization.Get("home.refresh", "Refresh"));
        AddAction(MavIcons.Logout, OnSignOut, MavLocalization.Get("home.signout", "Sign out"));
    }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    private async void OnRefresh()
    {
        await _groups.ReloadAsync();
        await _devices.ReloadAsync();
    }

    private void OnSignOut()
    {
        AppServices.Default.Session.SignOut();
        Navigator.ShowSignIn();
    }
}
