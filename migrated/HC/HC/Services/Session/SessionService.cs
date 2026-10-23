using Hc.Models.Cloud;
using Hc.Services.Cloud;
using Mav.Essentials;

namespace Hc.Services.Session;

/// <summary>
/// Sign-in, autologin and sign-out. The password is encrypted with <see cref="PasswordEncrypter"/> before it is
/// sent (the server expects the encrypted form), and only that encrypted form is stored, in secure storage.
/// Email and the autologin flag go to plain preferences.
/// </summary>
public sealed class SessionService(CloudClient cloud, IMavPreferences preferences, IMavSecureStorage secrets)
{
    public const string EmailKey = "session.email";
    public const string AutologinKey = "session.autologin";
    public const string PasswordKey = "session.password";

    /// <summary>Server user id of the signed-in user, or null when signed out.</summary>
    public string? UserId { get; private set; }

    public bool IsSignedIn => UserId is not null;

    /// <summary>Email of the last sign-in. Kept after sign-out so the login form can prefill it.</summary>
    public string Email => preferences.Get(EmailKey, string.Empty);

    public bool AutologinEnabled => preferences.Get(AutologinKey, false);

    /// <summary>
    /// Signs in with a plain password. When <paramref name="rememberMe"/> is set, the encrypted password is kept
    /// for autologin. Otherwise any saved password is removed.
    /// </summary>
    /// <exception cref="CloudRequestException">The server rejected the sign-in or could not be reached.</exception>
    public async Task LoginAsync(string email, string password, bool rememberMe, CancellationToken ct = default)
    {
        var encrypted = PasswordEncrypter.Encrypt(password);
        await SignInAsync(email, encrypted, ct).ConfigureAwait(false);

        preferences.Set(EmailKey, email);
        if (rememberMe)
        {
            await secrets.SetAsync(PasswordKey, encrypted).ConfigureAwait(false);
            preferences.Set(AutologinKey, true);
        }
        else
        {
            ForgetPassword();
        }
    }

    /// <summary>
    /// Signs in with the saved password, if autologin is on. Returns false when there is nothing to use or
    /// the server does not accept it, so the caller shows the login form. The saved password is kept on a
    /// network failure, so the next start can try again.
    /// </summary>
    public async Task<bool> TryAutoLoginAsync(CancellationToken ct = default)
    {
        if (!AutologinEnabled || Email.Length == 0)
            return false;

        var saved = await secrets.GetAsync(PasswordKey).ConfigureAwait(false);
        if (string.IsNullOrEmpty(saved))
            return false;

        try
        {
            await SignInAsync(Email, saved, ct).ConfigureAwait(false);
            return true;
        }
        catch (CloudRequestException ex) when (ex.Kind == CloudErrorKind.Transport)
        {
            return false;
        }
        catch (CloudRequestException)
        {
            // The server rejected the saved password: forget it so the user signs in again.
            ForgetPassword();
            return false;
        }
    }

    /// <summary>Signs out locally and removes the saved password. Nothing is sent to the server.</summary>
    public void SignOut()
    {
        UserId = null;
        ForgetPassword();
    }

    /// <summary>
    /// Turns autologin off and removes the saved password. Where secure storage is not available (for example no
    /// secret-tool on Linux), there is nothing to remove, and that must not stop sign-in or sign-out. Autologin is
    /// already off, so a leftover value is never used.
    /// </summary>
    private void ForgetPassword()
    {
        preferences.Set(AutologinKey, false);
        try
        {
            secrets.Remove(PasswordKey);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or InvalidOperationException)
        {
            // Secure storage is unavailable: no password was saved through it.
        }
    }

    private async Task SignInAsync(string email, string encryptedPassword, CancellationToken ct)
    {
        var response = await cloud.Login(new LoginRequest(email, encryptedPassword), ct).ConfigureAwait(false);
        UserId = CloudResults.Require(response).UserId;
    }
}
