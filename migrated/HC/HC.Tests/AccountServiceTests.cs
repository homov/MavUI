using Hc.Services.Cloud;
using Hc.Services.Session;
using Hc.Services.Settings;
using Xunit;

namespace Hc.Tests;

public class AccountServiceTests : IDisposable
{
    private readonly TempPreferences _prefs = new();

    public void Dispose() => _prefs.Dispose();

    [Fact]
    public async Task Register_SendsEncryptedPasswordTimezoneAndLanguage()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok"}""");
        var settings = new SettingsService(_prefs.Preferences);
        settings.PreferredTimezone = "Europe/Kyiv";
        var service = new AccountService(TestCloud.Create(handler), settings, new LanguageService(settings));

        await service.RegisterAsync("new@example.com", "pw", TestContext.Current.CancellationToken);

        var body = handler.Sent.Single().Body!;
        Assert.DoesNotContain("password=pw&", body);
        Assert.Contains("Europe%2FKyiv", body);
        Assert.Contains("language=", body);
    }

    [Fact]
    public async Task ResetPassword_SendsEncryptedNewPasswordAndCode()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok"}""");
        var settings = new SettingsService(_prefs.Preferences);
        var service = new AccountService(TestCloud.Create(handler), settings, new LanguageService(settings));

        await service.ResetPasswordAsync("user@example.com", "fresh", "1234", TestContext.Current.CancellationToken);

        var body = handler.Sent.Single().Body!;
        Assert.Contains("code=1234", body);
        Assert.DoesNotContain("fresh", body);
    }
}
