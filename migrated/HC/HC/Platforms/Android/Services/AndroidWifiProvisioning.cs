using Hc.Services.Wifi;

namespace Hc.Platforms.Android.Services;

/// <summary>
/// Android implementation of the device Wi-Fi setup (stage 3 of the plan): joins the device access point.
/// </summary>
public sealed class AndroidWifiProvisioning : IWifiProvisioning
{
    public bool IsSupported => true;
}
