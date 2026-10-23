using System.Text.Json.Serialization;
using Hc.Models.Cloud;

namespace Hc.Services.Cloud;

/// <summary>
/// JSON metadata for every type the cloud API sends or receives. Source generation replaces
/// reflection, which keeps the app AOT-compatible.
/// </summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(BaseResponse))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(GetUserSettingsResponse))]
[JsonSerializable(typeof(GetTimezonesResponse))]
[JsonSerializable(typeof(DevicesResponse))]
[JsonSerializable(typeof(UserDeviceInfo))]
[JsonSerializable(typeof(GetUserGroupsResponse))]
[JsonSerializable(typeof(GetScheduleResponse))]
[JsonSerializable(typeof(ScheduleInfo))]
[JsonSerializable(typeof(GetDeviceTypesResponse))]
[JsonSerializable(typeof(ErrorCodesResponse))]
[JsonSerializable(typeof(AllErrorCodesResponse))]
internal sealed partial class CloudJsonContext : JsonSerializerContext
{
}
