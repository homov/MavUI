using System.Net;

namespace HeatCeram.Api;

public static class HttpClientFactory
{
    public static HttpClient CreateHttpClient(in string url, int timeOut = ApiConstants.HttpRequestTimeout)
    {
        var handler = new HttpClientHandler { 
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        handler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeOut),
            BaseAddress = new Uri(url)
        };


        return client;
    }
}
