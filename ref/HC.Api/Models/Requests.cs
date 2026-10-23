namespace HC.Api.Models;

public record LoginRequest(string email, string password);

public record RegisterRequest(string email, string password, string timezone, string language);

public record ValidateEmailRequest(string email, string code);

public record QueryRestorePasswordRequest(string email);

public record RestorePasswordRequest(string email, string password, string code);

public record ChangePasswordRequest(string email, string password, string new_password);

public record GetUserSettingsRequest(string email);

public record SetTimezoneRequest(string timezone);

public record ScheduleRequest(string schedule_id);

public record GroupRequest(string group_id);

public record CreateGroupRequest(string group_name);

public record SetGroupNameRequest(string group_id, string group_name);

public record SetGroupSensorRequest(string group_id, string sensor_id);

public record SetScheduleTypeRequest(string schedule_id, string schedule_type);

public record SetDeviceHeatStateRequest(string device_id, string state);

public record SetDeviceNameRequest(string device_id, string name);

public record SetDeviceGroupRequest(string device_id, string group_id);

public record SetDeviceSensorRequest(string device_id, string sensor_id);

public record SetAntifreezeModeRequest(string device_id, int state);

public record SetHumiModeRequest(string device_id, int state);

public record SetCoeffRequest(string device_id, int value);

public record SetTempRequest(string device_id, int temp);

public record DeviceRequest(string device_id);

public record ErrorCodesRequest(string language);
