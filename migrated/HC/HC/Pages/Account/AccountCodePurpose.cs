namespace Hc.Pages.Account;

/// <summary>Why the 4-digit code was sent: to confirm a new account, or to reset a forgotten password.</summary>
public enum AccountCodePurpose
{
    ConfirmEmail,
    RestorePassword,
}
