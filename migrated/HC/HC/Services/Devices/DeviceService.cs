using Hc.Models.Cloud;
using Hc.Services.Cloud;

namespace Hc.Services.Devices;

/// <summary>Arguments of <see cref="DeviceService.HeatStateChanged"/>.</summary>
public sealed record HeatStateChangedArgs(string DeviceId, bool HeatOn);

/// <summary>
/// Device operations of the cloud API. Only one heat-state change per device runs at a time: a second request
/// for the same device fails at once with <see cref="InvalidOperationException"/>, so the UI can block that
/// device's switch while <see cref="IsHeatStateBusy"/> is true (the old app blocked the whole screen).
/// </summary>
public sealed class DeviceService(CloudClient cloud)
{
    private readonly object _gate = new();
    private readonly HashSet<string> _heatStateBusy = new(StringComparer.Ordinal);

    /// <summary>Raised after the server accepts a heat-state change.</summary>
    public event EventHandler<HeatStateChangedArgs>? HeatStateChanged;

    public bool IsHeatStateBusy(string deviceId)
    {
        lock (_gate)
            return _heatStateBusy.Contains(deviceId);
    }

    public async Task<IReadOnlyList<UserDeviceInfo>> GetDevicesAsync(CancellationToken ct = default)
    {
        var response = await cloud.GetUserDevices(ct).ConfigureAwait(false);
        return CloudResults.Require(response).Devices;
    }

    /// <summary>Device types with their limits (the old app's get_device_types).</summary>
    public async Task<IReadOnlyList<DeviceTypeInfo>> GetDeviceTypesAsync(CancellationToken ct = default)
    {
        var response = await cloud.GetDeviceTypes(ct).ConfigureAwait(false);
        return CloudResults.Require(response).DeviceTypes;
    }

    public async Task<UserDeviceInfo> GetDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        var response = await cloud.GetDeviceInfo(new DeviceRequest(deviceId), ct).ConfigureAwait(false);
        return CloudResults.RequireHttp(response);
    }

    /// <exception cref="InvalidOperationException">Another heat-state change for this device is still running.</exception>
    public async Task SetHeatStateAsync(string deviceId, bool heatOn, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (!_heatStateBusy.Add(deviceId))
                throw new InvalidOperationException($"Heat state of device {deviceId} is already being changed.");
        }

        try
        {
            var response = await cloud.SetDeviceHeatState(new SetDeviceHeatStateRequest(deviceId, heatOn ? "true" : "false"), ct)
                .ConfigureAwait(false);
            CloudResults.Require(response);
            HeatStateChanged?.Invoke(this, new HeatStateChangedArgs(deviceId, heatOn));
        }
        finally
        {
            lock (_gate)
                _heatStateBusy.Remove(deviceId);
        }
    }

    public async Task RenameAsync(string deviceId, string name, CancellationToken ct = default)
    {
        var response = await cloud.SetDeviceName(new SetDeviceNameRequest(deviceId, name), ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }

    public async Task MoveToGroupAsync(string deviceId, string groupId, CancellationToken ct = default)
    {
        var response = await cloud.SetDeviceGroup(new SetDeviceGroupRequest(deviceId, groupId), ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }

    public async Task DeleteAsync(string deviceId, CancellationToken ct = default)
    {
        var response = await cloud.DeleteDevice(new DeviceRequest(deviceId), ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }
}
