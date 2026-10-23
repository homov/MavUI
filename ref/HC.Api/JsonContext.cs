using System.Text.Json.Serialization;

namespace HC.Api;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(BaseResponse))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(GetUserSettingsResponse))]
[JsonSerializable(typeof(GetTimezonesResponse))]
[JsonSerializable(typeof(DevicesResponse))]
[JsonSerializable(typeof(GetUserGroupsResponse))]
[JsonSerializable(typeof(GetScheduleResponse))]
[JsonSerializable(typeof(ErrorCodesResponse))]
[JsonSerializable(typeof(AllErrorCodesResponse))]
[JsonSerializable(typeof(GetDeviceTypesResponse))]
[JsonSerializable(typeof(Schedule))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
}
