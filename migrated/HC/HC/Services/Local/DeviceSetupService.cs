namespace Hc.Services.Local;

/// <summary>Why the device setup stopped. The UI maps each reason to a localized message.</summary>
public enum DeviceSetupError
{
    /// <summary>The device did not answer on its access point.</summary>
    Unreachable,

    /// <summary>The device answered but did not accept a parameter.</summary>
    Rejected,
}

public sealed class DeviceSetupException(DeviceSetupError reason, string? detail = null)
    : Exception(detail ?? reason.ToString())
{
    public DeviceSetupError Reason { get; } = reason;
}

/// <summary>Identity of a device read from its access point.</summary>
public sealed record LocalDeviceInfo(string Id, string Model);

/// <summary>
/// Steps of the device setup wizard that talk to the device: read it, scan networks, then hand it the
/// account and the home Wi-Fi. Waiting for the device to appear in the cloud is a separate step (stage 6).
/// </summary>
public sealed class DeviceSetupService(LocalDeviceClient device)
{
    /// <summary>Reads the device id and model. Throws <see cref="DeviceSetupError.Unreachable"/> if the device does not answer.</summary>
    public async Task<LocalDeviceInfo> ReadDeviceAsync(CancellationToken ct = default)
    {
        var id = await device.GetDeviceIdAsync(ct).ConfigureAwait(false);
        if (!id.IsSuccessStatusCode || id.Content is null)
            throw new DeviceSetupException(DeviceSetupError.Unreachable, id.ErrorMessage);

        var model = await device.GetDeviceModelAsync(ct).ConfigureAwait(false);
        if (!model.IsSuccessStatusCode || model.Content is null)
            throw new DeviceSetupException(DeviceSetupError.Unreachable, model.ErrorMessage);

        return new LocalDeviceInfo(id.Content, model.Content);
    }

    /// <summary>Names of the Wi-Fi networks the device can see.</summary>
    public async Task<IReadOnlyList<string>> ScanNetworksAsync(CancellationToken ct = default)
    {
        var scan = await device.ScanNetworksAsync(ct).ConfigureAwait(false);
        if (!scan.IsSuccessStatusCode || scan.Content is null)
            throw new DeviceSetupException(DeviceSetupError.Unreachable, scan.ErrorMessage);

        return LocalDeviceClient.ParseNetworks(scan.Content);
    }

    /// <summary>
    /// Sends the account id, then the Wi-Fi credentials. The second step runs only if the first was accepted,
    /// so a rejected account never gets the password.
    /// </summary>
    public async Task AttachToAccountAsync(string userId, string ssid, string password, CancellationToken ct = default)
    {
        var account = await device.SetUserIdAsync(userId, ct).ConfigureAwait(false);
        if (!LocalDeviceClient.IsAccepted(account))
            throw new DeviceSetupException(DeviceSetupError.Rejected, account.ErrorMessage);

        var wifi = await device.SetWifiCredentialsAsync(ssid, password, ct).ConfigureAwait(false);
        if (!LocalDeviceClient.IsAccepted(wifi))
            throw new DeviceSetupException(DeviceSetupError.Rejected, wifi.ErrorMessage);
    }
}
