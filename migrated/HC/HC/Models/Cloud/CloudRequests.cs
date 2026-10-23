using Refit;

namespace Hc.Models.Cloud;

// Request bodies are sent as form data (UrlEncoded). AliasAs keeps the server's field names,
// which are snake_case and must not change.

public record LoginRequest(
    [property: AliasAs("email")] string Email,
    [property: AliasAs("password")] string Password);

public record RegisterRequest(
    [property: AliasAs("email")] string Email,
    [property: AliasAs("password")] string Password,
    [property: AliasAs("timezone")] string Timezone,
    [property: AliasAs("language")] string Language);

public record ValidateEmailRequest(
    [property: AliasAs("email")] string Email,
    [property: AliasAs("code")] string Code);

public record QueryRestorePasswordRequest(
    [property: AliasAs("email")] string Email);

public record RestorePasswordRequest(
    [property: AliasAs("email")] string Email,
    [property: AliasAs("password")] string Password,
    [property: AliasAs("code")] string Code);

public record ChangePasswordRequest(
    [property: AliasAs("email")] string Email,
    [property: AliasAs("password")] string Password,
    [property: AliasAs("new_password")] string NewPassword);

public record GetUserSettingsRequest(
    [property: AliasAs("email")] string Email);

public record SetTimezoneRequest(
    [property: AliasAs("timezone")] string Timezone);

public record ErrorCodesRequest(
    [property: AliasAs("language")] string Language);

public record GroupRequest(
    [property: AliasAs("group_id")] string GroupId);

public record CreateGroupRequest(
    [property: AliasAs("group_name")] string GroupName);

public record SetGroupNameRequest(
    [property: AliasAs("group_id")] string GroupId,
    [property: AliasAs("group_name")] string GroupName);

public record SetGroupSensorRequest(
    [property: AliasAs("group_id")] string GroupId,
    [property: AliasAs("sensor_id")] string SensorId);

public record ScheduleRequest(
    [property: AliasAs("schedule_id")] string ScheduleId);

public record SetScheduleTypeRequest(
    [property: AliasAs("schedule_id")] string ScheduleId,
    [property: AliasAs("schedule_type")] string ScheduleType);

public record DeviceRequest(
    [property: AliasAs("device_id")] string DeviceId);

public record SetDeviceHeatStateRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("state")] string State);

public record SetDeviceNameRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("name")] string Name);

public record SetDeviceGroupRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("group_id")] string GroupId);

public record SetDeviceSensorRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("sensor_id")] string SensorId);

public record SetAntifreezeModeRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("state")] int State);

public record SetHumidityModeRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("state")] int State);

public record SetCoefficientRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("value")] int Value);

public record SetTemperatureRequest(
    [property: AliasAs("device_id")] string DeviceId,
    [property: AliasAs("temp")] int Temperature);
