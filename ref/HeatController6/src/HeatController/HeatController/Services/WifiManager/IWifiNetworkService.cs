namespace HeatController.Services.WifiManager;

public interface IWifiNetworkService : IDisposable
{
    Task<WifiManagerResponse<NetworkData>> ConnectWifi(string ssid, string password);
    Task<WifiManagerResponse<NetworkData>> GetNetworkInfo();
    void DisconnectWifi(string ssid);
    Task<bool> OpenWifiSetting();
    Task<WifiManagerResponse<List<NetworkData>>> ScanWifiNetworks();
    Task<bool> OpenWirelessSetting();
}

