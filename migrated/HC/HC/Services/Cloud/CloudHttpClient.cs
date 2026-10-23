using System.Net;
using Hc.Config;

namespace Hc.Services.Cloud;

/// <summary>
/// The single HTTP client for the cloud API. The old app created one client per request and
/// disposed it in the middle of calls, which caused "Cannot access a disposed object".
/// One instance lives for the whole app, so cookies and connections are shared.
/// </summary>
public static class CloudHttpClient
{
    private static readonly Lazy<HttpClient> Shared = new(() => Create(ApiEndpoints.CloudBaseUrl));

    /// <summary>The app-wide client. Never dispose it.</summary>
    public static HttpClient Instance => Shared.Value;

    /// <summary>Creates a client for <paramref name="baseUrl"/>. Used by <see cref="Instance"/> and by tests.</summary>
    public static HttpClient Create(string baseUrl)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            // Kept from the old app: the server certificate is not validated.
            // This is a known risk (man-in-the-middle). Do not reuse this handler for other hosts.
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = ApiEndpoints.CloudTimeout,
        };
    }
}
