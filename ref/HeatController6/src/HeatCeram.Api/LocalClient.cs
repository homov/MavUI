using Refit;

namespace HeatCeram.Api;

public class LocalClient
{
    private Lazy<ILocalClient> cloudClient = new Lazy<ILocalClient>(CreateCloudClient);

    public ILocalClient Api { get => cloudClient.Value; }

    private static ILocalClient CreateCloudClient()
    {
        var httpClient = HttpClientFactory.CreateHttpClient(ApiConstants.LocalDeviceBaseAddress);
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        return RestService.For<ILocalClient>(httpClient);
    }
}
