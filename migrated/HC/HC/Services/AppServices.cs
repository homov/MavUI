using Hc.Services.Cloud;
using Hc.Services.Devices;
using Hc.Services.Local;
using Hc.Services.Locations;
using Hc.Services.Schedules;
using Hc.Services.Session;
using Hc.Services.Settings;
using Mav.Essentials;

namespace Hc.Services;

/// <summary>
/// Manual composition root (replaces the old Autofac container). Each service gets its dependencies in its
/// constructor, so tests can build the graph with fakes through <see cref="Create"/>.
/// </summary>
public sealed class AppServices
{
    private AppServices(
        CloudClient cloud,
        DeviceSetupService deviceSetup,
        SessionService session,
        AccountService account,
        DeviceService devices,
        LocationService locations,
        ScheduleService schedules,
        SettingsService settings,
        LanguageService language,
        ThemeService theme)
    {
        Cloud = cloud;
        DeviceSetup = deviceSetup;
        Session = session;
        Account = account;
        Devices = devices;
        Locations = locations;
        Schedules = schedules;
        Settings = settings;
        Language = language;
        Theme = theme;
    }

    public CloudClient Cloud { get; }
    public DeviceSetupService DeviceSetup { get; }
    public SessionService Session { get; }
    public AccountService Account { get; }
    public DeviceService Devices { get; }
    public LocationService Locations { get; }
    public ScheduleService Schedules { get; }
    public SettingsService Settings { get; }
    public LanguageService Language { get; }
    public ThemeService Theme { get; }

    /// <summary>The app-wide graph, built on the shared clients and the MavUI storage defaults.</summary>
    public static AppServices Default { get; } = Create(
        CloudClient.Instance,
        LocalDeviceClient.Instance,
        MavPreferences.Default,
        MavSecureStorage.Default);

    /// <summary>Builds the graph from the given clients and storage.</summary>
    public static AppServices Create(
        CloudClient cloud,
        LocalDeviceClient localDevice,
        IMavPreferences preferences,
        IMavSecureStorage secrets)
    {
        var settings = new SettingsService(preferences);
        var language = new LanguageService(settings);
        return new AppServices(
            cloud: cloud,
            deviceSetup: new DeviceSetupService(localDevice),
            session: new SessionService(cloud, preferences, secrets),
            account: new AccountService(cloud, settings, language),
            devices: new DeviceService(cloud),
            locations: new LocationService(cloud),
            schedules: new ScheduleService(cloud),
            settings: settings,
            language: language,
            theme: new ThemeService(settings));
    }
}
