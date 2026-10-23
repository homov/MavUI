using System.Net;
using Hc.Services;
using Hc.Services.Cloud;
using Hc.Services.Devices;
using Hc.Services.Settings;
using Xunit;

namespace Hc.Tests;

public class DeviceServiceTests
{
    [Fact]
    public async Task SetHeatState_SendsStateAndRaisesEvent()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok"}""");
        var service = new DeviceService(TestCloud.Create(handler));
        HeatStateChangedArgs? raised = null;
        service.HeatStateChanged += (_, args) => raised = args;

        await service.SetHeatStateAsync("dev-1", heatOn: true);

        Assert.Contains("state=true", handler.Sent.Single().Body);
        Assert.Equal(new HeatStateChangedArgs("dev-1", true), raised);
        Assert.False(service.IsHeatStateBusy("dev-1"));
    }

    [Fact]
    public async Task SetHeatState_SecondRequestForSameDevice_FailsWhileFirstIsRunning()
    {
        var firstSent = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var slow = new SlowHandler(firstSent, release);
        var service = new DeviceService(TestCloud.Create(slow));

        var first = service.SetHeatStateAsync("dev-1", heatOn: true);
        await firstSent.Task;

        Assert.True(service.IsHeatStateBusy("dev-1"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetHeatStateAsync("dev-1", heatOn: false));
        Assert.False(service.IsHeatStateBusy("dev-2"));

        release.SetResult();
        await first;
        Assert.False(service.IsHeatStateBusy("dev-1"));
    }

    [Fact]
    public async Task SetHeatState_ServerError_ClearsBusyFlag()
    {
        var service = new DeviceService(TestCloud.Create(ScriptedHandler.Json("""{"state":"error","description":"offline device"}""")));

        var ex = await Assert.ThrowsAsync<CloudRequestException>(() => service.SetHeatStateAsync("dev-1", heatOn: true));

        Assert.Equal(CloudErrorKind.Server, ex.Kind);
        Assert.False(service.IsHeatStateBusy("dev-1"));
    }

    /// <summary>Holds the first request until the test releases it, so the device stays busy.</summary>
    private sealed class SlowHandler(TaskCompletionSource sent, TaskCompletionSource release) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            sent.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            return ScriptedHandler.Ok("""{"state":"ok"}""");
        }
    }
}

public class SettingsServiceTests : IDisposable
{
    private readonly TempPreferences _prefs = new();

    public void Dispose() => _prefs.Dispose();

    [Fact]
    public void PreferredTimezone_DefaultsToDeviceZone_AndKeepsSavedValue()
    {
        var settings = new SettingsService(_prefs.Preferences);
        Assert.Equal(TimeZoneInfo.Local.Id, settings.PreferredTimezone);

        settings.PreferredTimezone = "Europe/Kyiv";

        Assert.Equal("Europe/Kyiv", settings.PreferredTimezone);
    }

    [Fact]
    public void Language_ResolvesSavedChoiceOrSystemLanguage()
    {
        var settings = new SettingsService(_prefs.Preferences);

        settings.Language = LanguageService.Ukrainian;
        Assert.Equal("uk", LanguageService.Resolve(settings.Language));

        Assert.Contains(LanguageService.Resolve(string.Empty), LanguageService.SupportedLanguages);
        Assert.Equal("en", LanguageService.Resolve("fr"));
    }

    [Fact]
    public void Theme_UnknownNameFallsBackToSystem()
    {
        var settings = new SettingsService(_prefs.Preferences);
        var theme = new ThemeService(settings);
        Assert.Equal(ThemeService.SystemName, theme.Current);

        settings.Theme = ThemeService.DarkName;
        Assert.Equal(ThemeService.DarkName, theme.Current);

        settings.Theme = "neon";
        Assert.Equal(ThemeService.SystemName, theme.Current);
    }
}
