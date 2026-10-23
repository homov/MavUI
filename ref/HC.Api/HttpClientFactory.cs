using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace HC.Api;

public static class HttpClientFactory
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, HttpClient> Clients = new();

    public static HttpClient GetClient(
        string baseAddress,
        int timeoutSeconds = ApiConstants.HttpRequestTimeout,
        bool ignoreSslErrors = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseAddress);

        lock (SyncRoot)
        {
            if (Clients.TryGetValue(baseAddress, out var existing))
                return existing;

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = true,
                CookieContainer = new CookieContainer(),
            };

            if (ignoreSslErrors)
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            Clients[baseAddress] = client;
            return client;
        }
    }
}
