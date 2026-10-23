namespace Hc.Models.Cloud;

// Request bodies are sent as form data. The server's field names (snake_case) are written in CloudClient.

public record LoginRequest(
    string Email,
    string Password);

public record RegisterRequest(
    string Email,
    string Password,
    string Timezone,
    string Language);

public record ValidateEmailRequest(
    string Email,
    string Code);

public record QueryRestorePasswordRequest(
    string Email);

public record RestorePasswordRequest(
    string Email,
    string Password,
    string Code);

public record ChangePasswordRequest(
    string Email,
    string Password,
    string NewPassword);

public record GetUserSettingsRequest(
    string Email);

public record SetTimezoneRequest(
    string Timezone);

public record ErrorCodesRequest(
    string Language);

public record GroupRequest(
    string GroupId);

public record CreateGroupRequest(
    string GroupName);

public record SetGroupNameRequest(
    string GroupId,
    string GroupName);

public record SetGroupSensorRequest(
    string GroupId,
    string? SensorId);

public record ScheduleRequest(
    string ScheduleId);

public record SetScheduleTypeRequest(
    string ScheduleId,
    string ScheduleType);

public record DeviceRequest(
    string DeviceId);

public record SetDeviceHeatStateRequest(
    string DeviceId,
    string State);

public record SetDeviceNameRequest(
    string DeviceId,
    string Name);

public record SetDeviceGroupRequest(
    string DeviceId,
    string GroupId);

public record SetDeviceSensorRequest(
    string DeviceId,
    string SensorId);

public record SetAntifreezeModeRequest(
    string DeviceId,
    int State);

public record SetHumidityModeRequest(
    string DeviceId,
    int State);

public record SetCoefficientRequest(
    string DeviceId,
    int Value);

public record SetTemperatureRequest(
    string DeviceId,
    int Temperature);
