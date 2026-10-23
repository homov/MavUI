namespace HC.Api;

public class ApiConstants
{
    public const string HeatCeramHost = "api.heatceram.com.ua";
    public const string LocalDeviceHost = "10.10.10.1";
    public const int LocalDevicePort = 10893;

    public static string HeatCeramBaseAddress = $"https://{HeatCeramHost}:443/stinexapiv1/";
    public static string LocalDeviceBaseAddress = $"http://{LocalDeviceHost}:{LocalDevicePort}";

    public static string ImageBaseUrl = $"https://{HeatCeramHost}/images";

    public const int HttpRequestTimeout = 15;
    public const int LocalRequestTimeout = 5;

    public const string StateOk = "ok";
    public const string StateNoGroups = "no_groups";

    public const string DescriptionAuthorizationError = "authorization error";
    public const string DescriptionUserNotBound = "user not bound";

    public const string OnlineStatusOnline = "online";
    public const string OnlineStatusOffline = "offline";

    public const string Monday = "monday";
    public const string Tuesday = "tuesday";
    public const string Wednesday = "wednesday";
    public const string Thursday = "thursday";
    public const string Friday = "friday";
    public const string Saturday = "saturday";
    public const string Sunday = "sunday";
}
