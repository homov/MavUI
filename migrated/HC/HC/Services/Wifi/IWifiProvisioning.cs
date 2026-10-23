namespace Hc.Services.Wifi;

/// <summary>
/// Device Wi-Fi setup, implemented per platform in Platforms/. Methods are added in stage 3.
/// </summary>
public interface IWifiProvisioning
{
    /// <summary>False on platforms where the wizard is hidden (Desktop).</summary>
    bool IsSupported { get; }
}
