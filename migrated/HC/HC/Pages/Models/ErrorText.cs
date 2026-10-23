using Hc.Services;
using Mav;

namespace Hc.Pages.Models;

/// <summary>Turns a failed call into the text shown under a form.</summary>
public static class ErrorText
{
    public static string From(Exception ex) => ex switch
    {
        CloudRequestException { Kind: CloudErrorKind.Transport } => Get("auth.error.transport"),
        CloudRequestException { Kind: CloudErrorKind.Authorization } => Get("auth.error.auth"),
        // The server's own description is already shown in the server's language.
        CloudRequestException { Kind: CloudErrorKind.Server } server when server.Message.Length > 0 => server.Message,
        CloudRequestException => Get("auth.error.http"),
        // Secure storage without a backend (for example no secret-tool on Linux) throws one of these.
        PlatformNotSupportedException or InvalidOperationException => Get("auth.error.storage"),
        _ => Get("auth.error.generic"),
    };

    private static string Get(string key) => MavLocalization.Get(key, key);
}
