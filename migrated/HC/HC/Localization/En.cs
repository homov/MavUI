namespace Hc.Localization;

/// <summary>English strings (default language).</summary>
internal static class En
{
    public static readonly IReadOnlyDictionary<string, string> Strings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["app.name"] = "HC",

        ["startup.checking"] = "Signing you in...",

        ["auth.signin.title"] = "Sign in",
        ["auth.signin.button"] = "Sign in",
        ["auth.register.link"] = "Create account",
        ["auth.restore.link"] = "Forgot password?",
        ["auth.field.email"] = "Email",
        ["auth.field.password"] = "Password",
        ["auth.field.password2"] = "Repeat password",
        ["auth.field.newpassword"] = "New password",
        ["auth.field.remember"] = "Remember me on this device",
        ["auth.register.title"] = "Create account",
        ["auth.register.button"] = "Create account",
        ["auth.restore.title"] = "Restore password",
        ["auth.restore.button"] = "Send code",
        ["auth.code.title"] = "Enter code",
        ["auth.code.hint"] = "Enter the 4-digit code sent to {0}",
        ["auth.code.confirm"] = "Confirm",
        ["auth.code.reset"] = "Set new password",

        ["auth.error.email"] = "Enter a valid email",
        ["auth.error.password"] = "Enter the password",
        ["auth.error.mismatch"] = "Passwords do not match",
        ["auth.error.code"] = "Enter the 4-digit code",
        ["auth.error.transport"] = "No connection to the server. Check the network and try again.",
        ["auth.error.http"] = "The server returned an error. Try again later.",
        ["auth.error.auth"] = "Your session has expired. Sign in again.",
        ["auth.error.storage"] = "Saving the password is not available on this system. Sign in without Remember me.",
        ["auth.error.generic"] = "Something went wrong. Try again.",

        ["home.title"] = "HC",
        ["home.placeholder"] = "Groups and devices will appear here.",
        ["home.signout"] = "Sign out",
    };
}
