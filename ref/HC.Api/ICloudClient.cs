namespace HC.Api;

public interface ICloudClient
{
    #region User

    Task<ApiResponse<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken = default);

    Task<string> LoginString(LoginRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> Register(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> UserDelete(CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> ValidateEmail(ValidateEmailRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> QueryRestorePassword(QueryRestorePasswordRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> RestorePassword(RestorePasswordRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<GetUserSettingsResponse>> GetUserSettings(GetUserSettingsRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetTimezone(SetTimezoneRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<GetTimezonesResponse>> GetTimezones(CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> GetUserDevicesAsString(CancellationToken cancellationToken = default);

    Task<ApiResponse<DevicesResponse>> GetUserDevices(CancellationToken cancellationToken = default);

    #endregion

    #region Groups

    Task<ApiResponse<string>> GetUserGroupsAsString(CancellationToken cancellationToken = default);

    Task<ApiResponse<GetUserGroupsResponse>> GetUserGroups(CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> GetGroupDevicesString(GroupRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<DevicesResponse>> GetGroupDevices(GroupRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<DevicesResponse>> RemoveGroup(GroupRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> CreateGroup(CreateGroupRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetGroupName(SetGroupNameRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetGroupSensor(SetGroupSensorRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Schedules

    Task<ApiResponse<string>> GetScheduleString(ScheduleRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<GetScheduleResponse>> GetSchedule(ScheduleRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetScheduleType(SetScheduleTypeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> UpdateSchedule(Schedule request, CancellationToken cancellationToken = default);

    #endregion

    #region Devices

    Task<ApiResponse<BaseResponse>> SetDeviceHeatState(SetDeviceHeatStateRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetDeviceName(SetDeviceNameRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetDeviceGroup(SetDeviceGroupRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetDeviceSensor(SetDeviceSensorRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetAntifreezeMode(SetAntifreezeModeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetHumiMode(SetHumiModeRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetPanelCoeff(SetCoeffRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetRoomCoeff(SetCoeffRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetPanelTemp(SetTempRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> SetHysteresis(SetTempRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<BaseResponse>> DeleteDevice(DeviceRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> GetDeviceTypesString(CancellationToken cancellationToken = default);

    Task<ApiResponse<GetDeviceTypesResponse>> GetDeviceTypes(CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> GetDeviceInfoString(DeviceRequest request, CancellationToken cancellationToken = default);
    
    Task<ApiResponse<ErrorCodesResponse>> GetErrorCodes(ErrorCodesRequest request, CancellationToken cancellationToken = default);

    Task<ApiResponse<AllErrorCodesResponse>> GetAllErrorCodes(CancellationToken cancellationToken = default);

    #endregion
}
