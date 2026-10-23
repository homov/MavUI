namespace Hc.Services;

/// <summary>Kind of a failed cloud call. The UI picks its message by kind.</summary>
public enum CloudErrorKind
{
    /// <summary>No answer from the server (network down, timeout).</summary>
    Transport,

    /// <summary>The server answered with an HTTP error or an unreadable body.</summary>
    Http,

    /// <summary>The server answered with a state other than "ok" or "no_groups". The message is its description.</summary>
    Server,

    /// <summary>The session is not authorized. The user must sign in again.</summary>
    Authorization,
}

/// <summary>A cloud call failed. Services throw this instead of returning raw responses.</summary>
public sealed class CloudRequestException(CloudErrorKind kind, string message) : Exception(message)
{
    public CloudErrorKind Kind { get; } = kind;
}
