using Hc.Services.Wifi;

namespace Hc.Platforms.Desktop.Services;

/// <summary>
/// Device Wi-Fi setup is not available on Desktop (decision in the plan): the wizard is hidden.
/// </summary>
public sealed class DesktopWifiProvisioning : IWifiProvisioning
{
    public bool IsSupported => false;
}
