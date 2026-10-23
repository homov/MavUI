using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Hc.Models.Cloud;
using Hc.Services;

namespace Hc.Services.Cloud;

/// <summary>
/// Cloud API client, written by hand (not Refit) so it stays AOT-compatible: JSON uses source generation
/// and form bodies are built explicitly. Requests run one at a time per instance (a queue, kept on purpose).
/// Errors come back in <see cref="ApiResponse{T}"/>; only cancellation requested by the caller is thrown.
/// Routes are relative to the base address, which must end with "/".
/// </summary>
public sealed class CloudClient(HttpClient http)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>The app-wide client over the shared HTTP client.</summary>
    public static CloudClient Instance { get; } = new(CloudHttpClient.Instance);

    #region Account

    public Task<ApiResponse<LoginResponse>> Login(LoginRequest r, CancellationToken ct = default) =>
        PostAsync("user_authorization/", Form(("email", r.Email), ("password", r.Password)), CloudJsonContext.Default.LoginResponse, ct);

    public Task<ApiResponse<BaseResponse>> Register(RegisterRequest r, CancellationToken ct = default) =>
        PostAsync("user_registration/", Form(("email", r.Email), ("password", r.Password), ("timezone", r.Timezone), ("language", r.Language)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> UserDelete(CancellationToken ct = default) =>
        PostAsync("user_delete/", null, CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> ValidateEmail(ValidateEmailRequest r, CancellationToken ct = default) =>
        PostAsync("user_validation/", Form(("email", r.Email), ("code", r.Code)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> QueryRestorePassword(QueryRestorePasswordRequest r, CancellationToken ct = default) =>
        PostAsync("user_password_restoration/", Form(("email", r.Email)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> RestorePassword(RestorePasswordRequest r, CancellationToken ct = default) =>
        PostAsync("password_restoration_validate/", Form(("email", r.Email), ("password", r.Password), ("code", r.Code)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> ChangePassword(ChangePasswordRequest r, CancellationToken ct = default) =>
        PostAsync("password_change/", Form(("email", r.Email), ("password", r.Password), ("new_password", r.NewPassword)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<GetUserSettingsResponse>> GetUserSettings(GetUserSettingsRequest r, CancellationToken ct = default) =>
        PostAsync("get_user_settings/", Form(("email", r.Email)), CloudJsonContext.Default.GetUserSettingsResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetTimezone(SetTimezoneRequest r, CancellationToken ct = default) =>
        PostAsync("change_timezone/", Form(("timezone", r.Timezone)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<GetTimezonesResponse>> GetTimezones(CancellationToken ct = default) =>
        PostAsync("get_timezones/", null, CloudJsonContext.Default.GetTimezonesResponse, ct);

    public Task<ApiResponse<DevicesResponse>> GetUserDevices(CancellationToken ct = default) =>
        PostAsync("get_user_devices/", null, CloudJsonContext.Default.DevicesResponse, ct);

    #endregion

    #region Groups

    public Task<ApiResponse<GetUserGroupsResponse>> GetUserGroups(CancellationToken ct = default) =>
        PostAsync("get_user_groups/", null, CloudJsonContext.Default.GetUserGroupsResponse, ct);

    public Task<ApiResponse<DevicesResponse>> GetGroupDevices(GroupRequest r, CancellationToken ct = default) =>
        PostAsync("get_group_devices/", Form(("group_id", r.GroupId)), CloudJsonContext.Default.DevicesResponse, ct);

    /// <summary>The server answers with the devices of the removed group (same shape as get_group_devices).</summary>
    public Task<ApiResponse<DevicesResponse>> RemoveGroup(GroupRequest r, CancellationToken ct = default) =>
        PostAsync("remove_group/", Form(("group_id", r.GroupId)), CloudJsonContext.Default.DevicesResponse, ct);

    public Task<ApiResponse<BaseResponse>> CreateGroup(CreateGroupRequest r, CancellationToken ct = default) =>
        PostAsync("create_new_group/", Form(("group_name", r.GroupName)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetGroupName(SetGroupNameRequest r, CancellationToken ct = default) =>
        PostAsync("set_group_name/", Form(("group_id", r.GroupId), ("group_name", r.GroupName)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetGroupSensor(SetGroupSensorRequest r, CancellationToken ct = default) =>
        PostAsync("set_group_sensor/", Form(("group_id", r.GroupId), ("sensor_id", r.SensorId)), CloudJsonContext.Default.BaseResponse, ct);

    #endregion

    #region Schedules

    public Task<ApiResponse<GetScheduleResponse>> GetSchedule(ScheduleRequest r, CancellationToken ct = default) =>
        PostAsync("get_schedule_by_id/", Form(("schedule_id", r.ScheduleId)), CloudJsonContext.Default.GetScheduleResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetScheduleType(SetScheduleTypeRequest r, CancellationToken ct = default) =>
        PostAsync("update_schedule_type/", Form(("schedule_id", r.ScheduleId), ("schedule_type", r.ScheduleType)), CloudJsonContext.Default.BaseResponse, ct);

    /// <summary>Sends the whole weekly schedule as JSON.</summary>
    public Task<ApiResponse<BaseResponse>> UpdateSchedule(ScheduleInfo schedule, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(schedule, CloudJsonContext.Default.ScheduleInfo);
        return PostAsync("update_schedule/", new StringContent(json, Encoding.UTF8, "application/json"), CloudJsonContext.Default.BaseResponse, ct);
    }

    #endregion

    #region Devices

    public Task<ApiResponse<BaseResponse>> SetDeviceHeatState(SetDeviceHeatStateRequest r, CancellationToken ct = default) =>
        PostAsync("set_heat_state/", Form(("device_id", r.DeviceId), ("state", r.State)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetDeviceName(SetDeviceNameRequest r, CancellationToken ct = default) =>
        PostAsync("set_device_name/", Form(("device_id", r.DeviceId), ("name", r.Name)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetDeviceGroup(SetDeviceGroupRequest r, CancellationToken ct = default) =>
        PostAsync("set_device_group/", Form(("device_id", r.DeviceId), ("group_id", r.GroupId)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetDeviceSensor(SetDeviceSensorRequest r, CancellationToken ct = default) =>
        PostAsync("set_device_sensor/", Form(("device_id", r.DeviceId), ("sensor_id", r.SensorId)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetAntifreezeMode(SetAntifreezeModeRequest r, CancellationToken ct = default) =>
        PostAsync("change_antifreeze_mode/", Form(("device_id", r.DeviceId), ("state", Int(r.State))), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetHumiMode(SetHumidityModeRequest r, CancellationToken ct = default) =>
        PostAsync("change_humidity_mode/", Form(("device_id", r.DeviceId), ("state", Int(r.State))), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetPanelCoeff(SetCoefficientRequest r, CancellationToken ct = default) =>
        PostAsync("change_panel_coef/", Form(("device_id", r.DeviceId), ("value", Int(r.Value))), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetRoomCoeff(SetCoefficientRequest r, CancellationToken ct = default) =>
        PostAsync("change_room_coef/", Form(("device_id", r.DeviceId), ("value", Int(r.Value))), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> SetPanelTemp(SetTemperatureRequest r, CancellationToken ct = default) =>
        PostAsync("set_panel_temp/", Form(("device_id", r.DeviceId), ("temp", Int(r.Temperature))), CloudJsonContext.Default.BaseResponse, ct);

    /// <summary>The route keeps the server's spelling ("gisteresis").</summary>
    public Task<ApiResponse<BaseResponse>> SetHysteresis(SetTemperatureRequest r, CancellationToken ct = default) =>
        PostAsync("change_gisteresis/", Form(("device_id", r.DeviceId), ("temp", Int(r.Temperature))), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<BaseResponse>> DeleteDevice(DeviceRequest r, CancellationToken ct = default) =>
        PostAsync("remove_device/", Form(("device_id", r.DeviceId)), CloudJsonContext.Default.BaseResponse, ct);

    public Task<ApiResponse<GetDeviceTypesResponse>> GetDeviceTypes(CancellationToken ct = default) =>
        PostAsync("get_device_types/", null, CloudJsonContext.Default.GetDeviceTypesResponse, ct);

    public Task<ApiResponse<UserDeviceInfo>> GetDeviceInfo(DeviceRequest r, CancellationToken ct = default) =>
        PostAsync("get_device_info_by_id/", Form(("device_id", r.DeviceId)), CloudJsonContext.Default.UserDeviceInfo, ct);

    #endregion

    #region Error codes

    public Task<ApiResponse<ErrorCodesResponse>> GetErrorCodes(ErrorCodesRequest r, CancellationToken ct = default) =>
        PostAsync("error_codes/", Form(("language", r.Language)), CloudJsonContext.Default.ErrorCodesResponse, ct);

    public Task<ApiResponse<AllErrorCodesResponse>> GetAllErrorCodes(CancellationToken ct = default) =>
        PostAsync("get_all_error_codes/", null, CloudJsonContext.Default.AllErrorCodesResponse, ct);

    #endregion

    #region Transport

    private async Task<ApiResponse<T>> PostAsync<T>(string route, HttpContent? content, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var response = await http.PostAsync(route, content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new ApiResponse<T> { StatusCode = response.StatusCode, IsSuccessStatusCode = false, ErrorMessage = body };

            return new ApiResponse<T>
            {
                StatusCode = response.StatusCode,
                IsSuccessStatusCode = true,
                Content = JsonSerializer.Deserialize(body, typeInfo),
            };
        }
        // Cancellation requested by the caller propagates. A timeout also surfaces as an OperationCanceledException,
        // but the caller did not cancel, so it becomes an ordinary failed response.
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return new ApiResponse<T> { StatusCode = 0, IsSuccessStatusCode = false, ErrorMessage = ex.Message };
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Builds a form body. Fields with a null value are left out.</summary>
    private static FormUrlEncodedContent Form(params (string Key, string? Value)[] fields) =>
        new(fields.Where(f => f.Value is not null).Select(f => new KeyValuePair<string, string>(f.Key, f.Value!)));

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    #endregion
}
