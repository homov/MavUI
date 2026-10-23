using Hc.Pages.Devices;
using Hc.Pages.Groups;
using Hc.Pages.Settings;
using Hc.Pages.Shell;
using Hc.Services;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Home;

/// <summary>Signed-in start page: two tabs, groups and devices. Each list refreshes by pulling down; the header opens account settings and sign-out.</summary>
public partial class HomePage : MavContentPage
{

    public HomePage()
    {
        InitializeComponent();

        Tabs.Add(MavLocalization.Get("home.tab.groups", "Groups"), MavIcons.Groups, content: new GroupListView());
        Tabs.Add(MavLocalization.Get("home.tab.devices", "Devices"), MavIcons.Devices, content: new DeviceListView());

        AddAction(MavIcons.Settings, OnSettings, MavLocalization.Get("home.settings", "Settings"));
        AddAction(MavIcons.Logout, OnSignOut, MavLocalization.Get("home.signout", "Sign out"));
    }

    /// <summary>Navigation of the app. Set before the page is shown.</summary>
    public required AppNavigator Navigator { get; init; }

    private void OnSettings() => Navigator.Push(new SettingsPage { Navigator = Navigator });

    private void OnSignOut()
    {
        AppServices.Default.Session.SignOut();
        Navigator.ShowSignIn();
    }
}
