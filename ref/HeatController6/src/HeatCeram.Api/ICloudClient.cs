using Refit;

namespace HeatCeram.Api;

[Headers("User-Agent: HeatController", "Accept-Encoding: gzip", "Accept: application/json, text/html")]
public interface ICloudClient
{
    #region User
    [Post("/user_authorization/")]
    Task<ApiResponse<LoginResponse>> Login([Body(BodySerializationMethod.UrlEncoded)] LoginRequest request);
    
    [Post("/user_authorization/")]
    Task<string> LoginString([Body(BodySerializationMethod.UrlEncoded)] LoginRequest request);

    [Post("/user_registration/")]
    Task<ApiResponse<BaseResponse>> Register([Body(BodySerializationMethod.UrlEncoded)] RegisterRequest request);

    [Post("/user_delete/")]
    Task<ApiResponse<BaseResponse>> UserDelete();


    [Post("/user_validation/")]
    Task<ApiResponse<BaseResponse>> ValidateEmail([Body(BodySerializationMethod.UrlEncoded)] ValidateEmailRequest request);

    [Post("/user_password_restoration/")]
    Task<ApiResponse<BaseResponse>> QueryRestorePassword([Body(BodySerializationMethod.UrlEncoded)] QueryRestorePasswordRequest request);

    [Post("/password_restoration_validate/")]
    Task<ApiResponse<BaseResponse>> RestorePassword([Body(BodySerializationMethod.UrlEncoded)] RestorePasswordRequest request);

    [Post("/password_change/")]
    Task<ApiResponse<BaseResponse>> ChangePassword([Body(BodySerializationMethod.UrlEncoded)] ChangePasswordRequest request);

    [Post("/get_user_settings/")]
    Task<ApiResponse<GetUserSettingsResponse>> GetUserSettings([Body(BodySerializationMethod.UrlEncoded)] GetUserSettingsRequest request);

    [Post("/change_timezone/")]
    Task<ApiResponse<BaseResponse>> SetTimezone([Body(BodySerializationMethod.UrlEncoded)] SetTimezoneRequest request);

    [Post("/get_timezones/")]
    Task<ApiResponse<GetTimezonesResponse>> GetTimezones();

    [Post("/get_user_devices/")]
    Task<ApiResponse<string>> GetUserDevicesAsString();

    [Post("/get_user_devices/")]
    Task<ApiResponse<DevicesResponse>> GetUserDevices();

    #endregion

    #region Groups

    [Post("/get_user_groups/")]
    Task<ApiResponse<string>> GetUserGroupsAsString();

    [Post("/get_user_groups/")]
    Task<ApiResponse<GetUserGroupsResponse>> GetUserGroups();

    [Post("/get_group_devices/")]
    Task<ApiResponse<string>> GetGroupDevicesString([Body(BodySerializationMethod.UrlEncoded)] GroupRequest request);

    [Post("/get_group_devices/")]
    Task<ApiResponse<DevicesResponse>> GetGroupDevices([Body(BodySerializationMethod.UrlEncoded)] GroupRequest request);

    [Post("/remove_group/")]
    Task<ApiResponse<DevicesResponse>> RemoveGroup([Body(BodySerializationMethod.UrlEncoded)] GroupRequest request);


    [Post("/create_new_group/")]
    Task<ApiResponse<BaseResponse>> CreateGroup([Body(BodySerializationMethod.UrlEncoded)] CretaeGroupRequest request);


    [Post("/set_group_name/")]
    Task<ApiResponse<BaseResponse>> SetGroupName([Body(BodySerializationMethod.UrlEncoded)] SetGroupNameRequest request);

    [Post("/set_group_sensor/")]
    Task<ApiResponse<BaseResponse>> SetGroupSensor([Body(BodySerializationMethod.UrlEncoded)] SetGroupSensorRequest request);

    #endregion

    #region Schedules

    [Post("/get_schedule_by_id/")]
    Task<ApiResponse<string>> GetScheduleString([Body(BodySerializationMethod.UrlEncoded)] ScheduleRequest request);

    [Post("/get_schedule_by_id/")]
    Task<ApiResponse<GetScheduleResponse>> GetSchedule([Body(BodySerializationMethod.UrlEncoded)] ScheduleRequest request);

    [Post("/update_schedule_type/")]
    Task<ApiResponse<BaseResponse>> SetScheduleType([Body(BodySerializationMethod.UrlEncoded)] SetScheduleTypeRequest request);


    [Post("/update_schedule/")]
    Task<ApiResponse<BaseResponse>> UpdateSchedule([Body(BodySerializationMethod.Json)] Schedule request);


    #endregion

    #region Devices

    [Post("/set_heat_state/")]
    Task<ApiResponse<BaseResponse>> SetDeviceHeatState([Body(BodySerializationMethod.UrlEncoded)] SetDeviceHeatStateRequest request);

    [Post("/set_device_name/")]
    Task<ApiResponse<BaseResponse>> SetDeviceName([Body(BodySerializationMethod.UrlEncoded)] SetDeviceNameRequest request);

    [Post("/set_device_group/")]
    Task<ApiResponse<BaseResponse>> SetDeviceGroup([Body(BodySerializationMethod.UrlEncoded)] SetDeviceGroupRequest request);

    [Post("/set_device_sensor/")]
    Task<ApiResponse<BaseResponse>> SetDeviceSensor([Body(BodySerializationMethod.UrlEncoded)] SetDeviceSensorRequest request);

    [Post("/change_antifreeze_mode/")]
    Task<ApiResponse<BaseResponse>> SetAntifreezeMode([Body(BodySerializationMethod.UrlEncoded)] SetAntifreezeModeRequest request);

    [Post("/change_humidity_mode/")]
    Task<ApiResponse<BaseResponse>> SetHumiMode([Body(BodySerializationMethod.UrlEncoded)] SetHumiModeRequest request);

    [Post("/change_panel_coef/")]
    Task<ApiResponse<BaseResponse>> SetPanelCoeff([Body(BodySerializationMethod.UrlEncoded)] SetCoeffRequest request);

    [Post("/change_room_coef/")]
    Task<ApiResponse<BaseResponse>> SetRoomCoeff([Body(BodySerializationMethod.UrlEncoded)] SetCoeffRequest request);
    
    [Post("/set_panel_temp/")]
    Task<ApiResponse<BaseResponse>> SetPanelTemp([Body(BodySerializationMethod.UrlEncoded)] SetTempRequest request);

    [Post("/change_gisteresis/")]
    Task<ApiResponse<BaseResponse>> SetHisteresis([Body(BodySerializationMethod.UrlEncoded)] SetTempRequest request);

    [Post("/remove_device/")]
    Task<ApiResponse<BaseResponse>> DeleteDevice([Body(BodySerializationMethod.UrlEncoded)] DeviceRequest request);

    [Post("/get_device_types/")]
    Task<ApiResponse<string>> GetDeviceTypesString();

    [Post("/get_device_types/")]
    Task<ApiResponse<GetDeviceTypesResponse>> GetDeviceTypes();

    [Post("/get_device_info_by_id/")]
    Task<ApiResponse<string>> GetDeviceInfoString([Body(BodySerializationMethod.UrlEncoded)] DeviceRequest request);

    [Post("/error_codes/")]
    Task<ApiResponse<ErrorCodesResponse>> GetErrorCodes([Body(BodySerializationMethod.UrlEncoded)] ErrorCodesRequest request);

    #endregion

}
