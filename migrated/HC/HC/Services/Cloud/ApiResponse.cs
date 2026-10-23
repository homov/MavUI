using System.Net;

namespace Hc.Services.Cloud;

/// <summary>
/// Result of a cloud call. HTTP errors and network failures are returned here instead of thrown,
/// so callers check <see cref="IsSuccessStatusCode"/> and show <see cref="ErrorMessage"/>.
/// </summary>
/// <typeparam name="T">Type of the parsed body.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>Parsed body. Null when the request failed.</summary>
    public T? Content { get; init; }

    /// <summary>HTTP status, or 0 when the request did not reach the server (network error, timeout).</summary>
    public HttpStatusCode StatusCode { get; init; }

    public bool IsSuccessStatusCode { get; init; }

    /// <summary>Response body for HTTP errors, or the exception message for transport errors.</summary>
    public string? ErrorMessage { get; init; }
}
