using Refit;

namespace HeatCeram.Api;

public class CloudClient
{
    private Lazy<ICloudClient> cloudClient = new Lazy<ICloudClient>(CreateCloudClient);

    public ICloudClient Api { get => cloudClient.Value; }

    public static ICloudClient CreateCloudClient()
    {
        return RestService.For<ICloudClient>(HttpClientFactory.CreateHttpClient(ApiConstants.HeatCeramBaseAddress)  );
    }

    public static ICloudClient CreateCloudClient(string baseAddress)
    {
        return RestService.For<ICloudClient>(HttpClientFactory.CreateHttpClient(baseAddress));
    }
}
