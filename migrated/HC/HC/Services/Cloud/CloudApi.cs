using Refit;

namespace Hc.Services.Cloud;

/// <summary>The app-wide cloud API, built on <see cref="CloudHttpClient.Instance"/>.</summary>
public static class CloudApi
{
    public static ICloudApi Instance { get; } = RestService.For<ICloudApi>(CloudHttpClient.Instance);
}
