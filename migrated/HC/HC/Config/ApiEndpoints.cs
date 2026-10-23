namespace Hc.Config;

/// <summary>
/// API addresses. The cloud base URL can be overridden with the HC_API_BASE environment variable
/// (for example to point at the test server). The default is the production server.
/// </summary>
public static class ApiEndpoints
{
    private const string DefaultCloudBaseUrl = "https://api.heatceram.com.ua/stinexapiv1";

    /// <summary>Plain-HTTP access point of the heating device (used only during setup).</summary>
    public const string LocalDeviceBaseUrl = "http://10.10.10.1:10893";

    public static string CloudBaseUrl =>
        Environment.GetEnvironmentVariable("HC_API_BASE") is { Length: > 0 } custom
            ? custom
            : DefaultCloudBaseUrl;

    public static TimeSpan CloudTimeout => TimeSpan.FromSeconds(15);

    public static TimeSpan LocalDeviceTimeout => TimeSpan.FromSeconds(5);
}
