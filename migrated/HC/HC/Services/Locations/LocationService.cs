using Hc.Models.Cloud;
using Hc.Services.Cloud;

namespace Hc.Services.Locations;

/// <summary>Groups ("locations") of the user: list, create, rename, remove, and their devices.</summary>
public sealed class LocationService(CloudClient cloud)
{
    public async Task<IReadOnlyList<UserGroupInfo>> GetGroupsAsync(CancellationToken ct = default)
    {
        var response = await cloud.GetUserGroups(ct).ConfigureAwait(false);
        return CloudResults.Require(response).Groups;
    }

    public async Task<IReadOnlyList<UserDeviceInfo>> GetGroupDevicesAsync(string groupId, CancellationToken ct = default)
    {
        var response = await cloud.GetGroupDevices(new GroupRequest(groupId), ct).ConfigureAwait(false);
        return CloudResults.Require(response).Devices;
    }

    public async Task CreateAsync(string name, CancellationToken ct = default)
    {
        var response = await cloud.CreateGroup(new CreateGroupRequest(name), ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }

    public async Task RenameAsync(string groupId, string name, CancellationToken ct = default)
    {
        var response = await cloud.SetGroupName(new SetGroupNameRequest(groupId, name), ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }

    /// <summary>Removes a group.</summary>
    public async Task RemoveAsync(string groupId, CancellationToken ct = default)
    {
        var response = await cloud.RemoveGroup(new GroupRequest(groupId), ct).ConfigureAwait(false);
        CloudResults.Require(response);
    }
}
