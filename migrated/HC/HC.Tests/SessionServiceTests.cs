using System.Net;
using Hc.Services;
using Hc.Services.Cloud;
using Hc.Services.Session;
using Xunit;

namespace Hc.Tests;

public class SessionServiceTests : IDisposable
{
    private readonly TempPreferences _prefs = new();
    private readonly FakeSecureStorage _secrets = new();

    public void Dispose() => _prefs.Dispose();

    private SessionService Session(ScriptedHandler handler) =>
        new(TestCloud.Create(handler), _prefs.Preferences, _secrets);

    private static async Task<string> FormField(string name, string value) =>
        await new FormUrlEncodedContent([new KeyValuePair<string, string>(name, value)]).ReadAsStringAsync();

    [Fact]
    public async Task Login_SendsEncryptedPassword_NotThePlainOne()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok","uuid":"u-1"}""");
        var session = Session(handler);

        await session.LoginAsync("user@example.com", "my-secret", rememberMe: false);

        var body = handler.Sent.Single().Body!;
        Assert.DoesNotContain("my-secret", body);
        Assert.Contains(await FormField("password", PasswordEncrypter.Encrypt("my-secret")), body);
        Assert.Equal("u-1", session.UserId);
        Assert.True(session.IsSignedIn);
    }

    [Fact]
    public async Task Login_RememberMe_StoresEncryptedPasswordAndTurnsAutologinOn()
    {
        var session = Session(ScriptedHandler.Json("""{"state":"ok","uuid":"u-1"}"""));

        await session.LoginAsync("user@example.com", "pw", rememberMe: true);

        Assert.Equal(PasswordEncrypter.Encrypt("pw"), await _secrets.GetAsync(SessionService.PasswordKey));
        Assert.True(session.AutologinEnabled);
        Assert.Equal("user@example.com", session.Email);
    }

    [Fact]
    public async Task Login_WithoutRememberMe_RemovesSavedPassword()
    {
        await _secrets.SetAsync(SessionService.PasswordKey, "old");
        var session = Session(ScriptedHandler.Json("""{"state":"ok","uuid":"u-1"}"""));

        await session.LoginAsync("user@example.com", "pw", rememberMe: false);

        Assert.Null(await _secrets.GetAsync(SessionService.PasswordKey));
        Assert.False(session.AutologinEnabled);
    }

    [Fact]
    public async Task Login_Rejected_ThrowsServerErrorAndKeepsEmailUnchanged()
    {
        var session = Session(ScriptedHandler.Json("""{"state":"error","description":"wrong password"}"""));

        var ex = await Assert.ThrowsAsync<CloudRequestException>(() => session.LoginAsync("user@example.com", "bad", rememberMe: true));

        Assert.Equal(CloudErrorKind.Server, ex.Kind);
        Assert.Equal("wrong password", ex.Message);
        Assert.False(session.IsSignedIn);
        Assert.Equal(string.Empty, session.Email);
        Assert.Null(await _secrets.GetAsync(SessionService.PasswordKey));
    }

    [Fact]
    public async Task TryAutoLogin_UsesStoredValueAsIs()
    {
        _prefs.Preferences.Set(SessionService.AutologinKey, true);
        _prefs.Preferences.Set(SessionService.EmailKey, "user@example.com");
        await _secrets.SetAsync(SessionService.PasswordKey, "ENC-VALUE");
        var handler = ScriptedHandler.Json("""{"state":"ok","uuid":"u-2"}""");
        var session = Session(handler);

        var ok = await session.TryAutoLoginAsync();

        Assert.True(ok);
        Assert.Equal("u-2", session.UserId);
        // The stored value is already encrypted, so it must be sent without encrypting it again.
        Assert.Contains(await FormField("password", "ENC-VALUE"), handler.Sent.Single().Body);
    }

    [Fact]
    public async Task TryAutoLogin_RejectedPassword_ForgetsItAndReturnsFalse()
    {
        _prefs.Preferences.Set(SessionService.AutologinKey, true);
        _prefs.Preferences.Set(SessionService.EmailKey, "user@example.com");
        await _secrets.SetAsync(SessionService.PasswordKey, "stale");
        var session = Session(ScriptedHandler.Json("""{"state":"error","description":"authorization error"}"""));

        var ok = await session.TryAutoLoginAsync();

        Assert.False(ok);
        Assert.Null(await _secrets.GetAsync(SessionService.PasswordKey));
        Assert.False(session.AutologinEnabled);
    }

    [Fact]
    public async Task TryAutoLogin_NoConnection_KeepsPasswordForNextStart()
    {
        _prefs.Preferences.Set(SessionService.AutologinKey, true);
        _prefs.Preferences.Set(SessionService.EmailKey, "user@example.com");
        await _secrets.SetAsync(SessionService.PasswordKey, "kept");
        var session = Session(new ScriptedHandler((_, _) => throw new HttpRequestException("offline")));

        var ok = await session.TryAutoLoginAsync();

        Assert.False(ok);
        Assert.Equal("kept", await _secrets.GetAsync(SessionService.PasswordKey));
        Assert.True(session.AutologinEnabled);
    }

    [Fact]
    public async Task TryAutoLogin_WithoutAutologin_DoesNotCallServer()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok","uuid":"u-1"}""");
        var session = Session(handler);

        Assert.False(await session.TryAutoLoginAsync());
        Assert.Empty(handler.Sent);
    }

    [Fact]
    public async Task SignOut_ClearsSessionAndSavedPassword()
    {
        var session = Session(ScriptedHandler.Json("""{"state":"ok","uuid":"u-1"}"""));
        await session.LoginAsync("user@example.com", "pw", rememberMe: true);

        session.SignOut();

        Assert.False(session.IsSignedIn);
        Assert.False(session.AutologinEnabled);
        Assert.Null(await _secrets.GetAsync(SessionService.PasswordKey));
        Assert.Equal("user@example.com", session.Email);
    }
}
