using Hc.Services.Wifi;

namespace Hc.Platforms.Windows.Services;

/// <summary>
/// Device Wi-Fi setup is not available on Windows (decision in the plan): the wizard is hidden.
/// </summary>
public sealed class WindowsWifiProvisioning : IWifiProvisioning
{
    public bool IsSupported => false;
}
