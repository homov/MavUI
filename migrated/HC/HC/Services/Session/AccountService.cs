using Hc.Models.Cloud;
using Hc.Services.Cloud;
using Hc.Services.Settings;

namespace Hc.Services.Session;

/// <summary>
/// Account operations that do not sign in: registration, email confirmation with a code, and password reset.
/// Passwords are encrypted with <see cref="PasswordEncrypter"/> before sending, as the server expects.
/// </summary>
public sealed class AccountService(CloudClient cloud, SettingsService settings, LanguageService language)
{
    /// <summary>Creates the account. The server then sends a confirmation code to the email.</summary>
    public async Task RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        var request = new RegisterRequest(email, PasswordEncrypter.Encrypt(password), settings.PreferredTimezone, language.Current);
        CloudResults.Require(await cloud.Register(request, ct).ConfigureAwait(false));
    }

    /// <summary>Confirms the email with the code from the message.</summary>
    public async Task ConfirmEmailAsync(string email, string code, CancellationToken ct = default)
    {
        CloudResults.Require(await cloud.ValidateEmail(new ValidateEmailRequest(email, code), ct).ConfigureAwait(false));
    }

    /// <summary>Changes the password of a signed-in account. The server checks the current password.</summary>
    public async Task ChangePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var request = new ChangePasswordRequest(email, PasswordEncrypter.Encrypt(currentPassword), PasswordEncrypter.Encrypt(newPassword));
        CloudResults.Require(await cloud.ChangePassword(request, ct).ConfigureAwait(false));
    }

    /// <summary>Asks the server to send a password reset code to the email.</summary>
    public async Task RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        CloudResults.Require(await cloud.QueryRestorePassword(new QueryRestorePasswordRequest(email), ct).ConfigureAwait(false));
    }

    /// <summary>Sets a new password with the code from the message.</summary>
    public async Task ResetPasswordAsync(string email, string newPassword, string code, CancellationToken ct = default)
    {
        var request = new RestorePasswordRequest(email, PasswordEncrypter.Encrypt(newPassword), code);
        CloudResults.Require(await cloud.RestorePassword(request, ct).ConfigureAwait(false));
    }
}
