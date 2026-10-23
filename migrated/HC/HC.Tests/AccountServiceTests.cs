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

    [Fact]
    public async Task ChangePassword_SendsEmailAndEncryptedPasswordsOnly()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok"}""");
        var settings = new SettingsService(_prefs.Preferences);
        var service = new AccountService(TestCloud.Create(handler), settings, new LanguageService(settings));

        await service.ChangePasswordAsync("user@example.com", "secret-old", "secret-new", TestContext.Current.CancellationToken);

        var body = handler.Sent.Single().Body!;
        Assert.Contains("email=user%40example.com", body);
        Assert.DoesNotContain("secret-old", body);
        Assert.DoesNotContain("secret-new", body);
    }

    [Fact]
    public async Task SetTimezone_SendsToServerAndSavesLocally()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok"}""");
        var settings = new SettingsService(_prefs.Preferences);
        var service = new AccountService(TestCloud.Create(handler), settings, new LanguageService(settings));

        await service.SetTimezoneAsync("Europe/Kyiv", TestContext.Current.CancellationToken);

        Assert.Contains("timezone=Europe%2FKyiv", handler.Sent.Single().Body!);
        Assert.Equal("Europe/Kyiv", settings.PreferredTimezone);
    }
}
