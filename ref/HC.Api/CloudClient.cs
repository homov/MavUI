using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace HC.Api;

public sealed class CloudHttpClient : ICloudClient
{
    private readonly HttpClient _http;
    private static readonly AppJsonSerializerContext JsonContext = AppJsonSerializerContext.Default;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CloudHttpClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    #region User

    public Task<ApiResponse<LoginResponse>> Login(LoginRequest request, CancellationToken ct = default)
        => SendFormAsync<LoginResponse, LoginRequest>(request, "/user_authorization/", ct);

    public async Task<string> LoginString(LoginRequest request, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var response = await _http.PostAsync("user_authorization/", ToFormContent(request), ct).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task<ApiResponse<BaseResponse>> Register(RegisterRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, RegisterRequest>(request, "/user_registration/", ct);

    public Task<ApiResponse<BaseResponse>> UserDelete(CancellationToken ct = default)
        => SendFormAsync<BaseResponse>("/user_delete/", ct);

    public Task<ApiResponse<BaseResponse>> ValidateEmail(ValidateEmailRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, ValidateEmailRequest>(request, "/user_validation/", ct);

    public Task<ApiResponse<BaseResponse>> QueryRestorePassword(QueryRestorePasswordRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, QueryRestorePasswordRequest>(request, "/user_password_restoration/", ct);

    public Task<ApiResponse<BaseResponse>> RestorePassword(RestorePasswordRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, RestorePasswordRequest>(request, "/password_restoration_validate/", ct);

    public Task<ApiResponse<BaseResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, ChangePasswordRequest>(request, "/password_change/", ct);

    public Task<ApiResponse<GetUserSettingsResponse>> GetUserSettings(GetUserSettingsRequest request, CancellationToken ct = default)
        => SendFormAsync<GetUserSettingsResponse, GetUserSettingsRequest>(request, "/get_user_settings/", ct);

    public Task<ApiResponse<BaseResponse>> SetTimezone(SetTimezoneRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetTimezoneRequest>(request, "/change_timezone/", ct);

    public Task<ApiResponse<GetTimezonesResponse>> GetTimezones(CancellationToken ct = default)
        => SendFormAsync<GetTimezonesResponse>("/get_timezones/", ct);

    public Task<ApiResponse<string>> GetUserDevicesAsString(CancellationToken ct = default)
        => SendStringAsync("/get_user_devices/", ct);


    public Task<ApiResponse<DevicesResponse>> GetUserDevices(CancellationToken ct = default)
        => SendFormAsync<DevicesResponse>("/get_user_devices/", ct);

    #endregion

    #region Groups

    public Task<ApiResponse<string>> GetUserGroupsAsString(CancellationToken ct = default)
        => SendStringAsync("/get_user_groups/", ct);

    public Task<ApiResponse<GetUserGroupsResponse>> GetUserGroups(CancellationToken ct = default)
        => SendFormAsync<GetUserGroupsResponse>("/get_user_groups/", ct);

    public Task<ApiResponse<string>> GetGroupDevicesString(GroupRequest request, CancellationToken ct = default)
        => SendStringAsync<GroupRequest>(request, "/get_group_devices/", ct);

    public Task<ApiResponse<DevicesResponse>> GetGroupDevices(GroupRequest request, CancellationToken ct = default)
        => SendFormAsync<DevicesResponse, GroupRequest>(request, "/get_group_devices/", ct);

    public Task<ApiResponse<DevicesResponse>> RemoveGroup(GroupRequest request, CancellationToken ct = default)
        => SendFormAsync<DevicesResponse, GroupRequest>(request, "/remove_group/", ct);

    public Task<ApiResponse<BaseResponse>> CreateGroup(CreateGroupRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, CreateGroupRequest>(request, "/create_new_group/", ct);

    public Task<ApiResponse<BaseResponse>> SetGroupName(SetGroupNameRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetGroupNameRequest>(request, "/set_group_name/", ct);

    public Task<ApiResponse<BaseResponse>> SetGroupSensor(SetGroupSensorRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetGroupSensorRequest>(request, "/set_group_sensor/", ct);

    #endregion

    #region Schedules

    public Task<ApiResponse<string>> GetScheduleString(ScheduleRequest request, CancellationToken ct = default)
        => SendStringAsync<ScheduleRequest>(request, "/get_schedule_by_id/", ct);

    public Task<ApiResponse<GetScheduleResponse>> GetSchedule(ScheduleRequest request, CancellationToken ct = default)
        => SendFormAsync<GetScheduleResponse, ScheduleRequest>(request, "/get_schedule_by_id/", ct);

    public Task<ApiResponse<BaseResponse>> SetScheduleType(SetScheduleTypeRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetScheduleTypeRequest>(request, "/update_schedule_type/", ct);

    public Task<ApiResponse<BaseResponse>> UpdateSchedule(Schedule request, CancellationToken ct = default)
        => SendJsonAsync<BaseResponse, Schedule>(request, "/update_schedule/", ct);

    #endregion

    #region Devices

    public Task<ApiResponse<BaseResponse>> SetDeviceHeatState(SetDeviceHeatStateRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetDeviceHeatStateRequest>(request, "/set_heat_state/", ct);

    public Task<ApiResponse<BaseResponse>> SetDeviceName(SetDeviceNameRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetDeviceNameRequest>(request, "/set_device_name/", ct);

    public Task<ApiResponse<BaseResponse>> SetDeviceGroup(SetDeviceGroupRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetDeviceGroupRequest>(request, "/set_device_group/", ct);

    public Task<ApiResponse<BaseResponse>> SetDeviceSensor(SetDeviceSensorRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetDeviceSensorRequest>(request, "/set_device_sensor/", ct);

    public Task<ApiResponse<BaseResponse>> SetAntifreezeMode(SetAntifreezeModeRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetAntifreezeModeRequest>(request, "/change_antifreeze_mode/", ct);

    public Task<ApiResponse<BaseResponse>> SetHumiMode(SetHumiModeRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetHumiModeRequest>(request, "/change_humidity_mode/", ct);

    public Task<ApiResponse<BaseResponse>> SetPanelCoeff(SetCoeffRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetCoeffRequest>(request, "/change_panel_coef/", ct);

    public Task<ApiResponse<BaseResponse>> SetRoomCoeff(SetCoeffRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetCoeffRequest>(request, "/change_room_coef/", ct);

    public Task<ApiResponse<BaseResponse>> SetPanelTemp(SetTempRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetTempRequest>(request, "/set_panel_temp/", ct);

    public Task<ApiResponse<BaseResponse>> SetHysteresis(SetTempRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, SetTempRequest>(request, "/change_gisteresis/", ct);

    public Task<ApiResponse<BaseResponse>> DeleteDevice(DeviceRequest request, CancellationToken ct = default)
        => SendFormAsync<BaseResponse, DeviceRequest>(request, "/remove_device/", ct);

    public Task<ApiResponse<string>> GetDeviceTypesString(CancellationToken ct = default)
        => SendStringAsync("/get_device_types/", ct);

    public Task<ApiResponse<GetDeviceTypesResponse>> GetDeviceTypes(CancellationToken ct = default)
        => SendFormAsync<GetDeviceTypesResponse>("/get_device_types/", ct);

    public Task<ApiResponse<string>> GetDeviceInfoString(DeviceRequest request, CancellationToken ct = default)
        => SendStringAsync<DeviceRequest>(request, "/get_device_info_by_id/", ct);

    public Task<ApiResponse<UserDeviceInfo>> GetDeviceInfo(DeviceRequest request, CancellationToken ct = default)
        => SendFormAsync<UserDeviceInfo,DeviceRequest>(request, "/get_device_info_by_id/", ct);

    public Task<ApiResponse<ErrorCodesResponse>> GetErrorCodes(ErrorCodesRequest request, CancellationToken ct = default)
        => SendFormAsync<ErrorCodesResponse, ErrorCodesRequest>(request, "/error_codes/", ct);

    public Task<ApiResponse<AllErrorCodesResponse>> GetAllErrorCodes(CancellationToken ct = default)
        => SendFormAsync<AllErrorCodesResponse>("/get_all_error_codes/", ct);

    #endregion

    #region Implementation

    private Task<ApiResponse<TResponse>> SendFormAsync<TResponse>(string route, CancellationToken ct)
        => SendAsync<TResponse>(route, null, ct);

    private Task<ApiResponse<TResponse>> SendFormAsync<TResponse, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TRequest>(TRequest request, string route, CancellationToken ct)
        => SendAsync<TResponse>(route, ToFormContent(request), ct);

    private Task<ApiResponse<string>> SendStringAsync(string route, CancellationToken ct)
        => SendStringCore(route, null, ct);

    private Task<ApiResponse<string>> SendStringAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TRequest>(TRequest request, string route, CancellationToken ct)
        => SendStringCore(route, ToFormContent(request), ct);

    private async Task<ApiResponse<TResponse>> SendJsonAsync<TResponse, TBody>(TBody body, string route, CancellationToken ct)
    {
        var typeInfo = JsonContext.GetTypeInfo(typeof(TBody))!;
        var json = JsonSerializer.Serialize(body, typeInfo);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await SendAsync<TResponse>(route, content, ct).ConfigureAwait(false);
    }

    private async Task<ApiResponse<TResponse>> SendAsync<TResponse>(string route, HttpContent? content, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var response = await _http.PostAsync(route.TrimStart('/'), content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return new ApiResponse<TResponse> { StatusCode = response.StatusCode, IsSuccessStatusCode = false, ErrorMessage = body };

            var typeInfo = JsonContext.GetTypeInfo(typeof(TResponse))!;
            var result = (TResponse?)JsonSerializer.Deserialize(body, typeInfo);
            return new ApiResponse<TResponse> { StatusCode = response.StatusCode, IsSuccessStatusCode = true, Content = result };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ApiResponse<TResponse> { StatusCode = (HttpStatusCode)0, IsSuccessStatusCode = false, ErrorMessage = ex.Message };
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ApiResponse<string>> SendStringCore(string route, HttpContent? content, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var response = await _http.PostAsync(route.TrimStart('/'), content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return new ApiResponse<string> { StatusCode = response.StatusCode, IsSuccessStatusCode = response.IsSuccessStatusCode, Content = body };
        }
        catch (Exception ex)
        {
            return new ApiResponse<string> { StatusCode = (HttpStatusCode)0, IsSuccessStatusCode = false, ErrorMessage = ex.Message };
        }
        finally
        {
            _gate.Release();
        }
    }

    private static HttpContent ToFormContent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TRequest>(TRequest request)
    {
        var fields = typeof(TRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetValue(request) is not null)
            .Select(p => new KeyValuePair<string, string>(p.Name, p.GetValue(request)!.ToString()!));

        return new FormUrlEncodedContent(fields);
    }

    #endregion
}

public class CloudClient
{
    private static readonly Lazy<ICloudClient> DefaultClient = new(() =>
        new CloudHttpClient(HttpClientFactory.GetClient(
            ApiConstants.HeatCeramBaseAddress,
            ApiConstants.HttpRequestTimeout,
            ignoreSslErrors: true)));

    public ICloudClient Api => DefaultClient.Value;

    public static ICloudClient CreateCloudClient() => DefaultClient.Value;

    public static ICloudClient CreateCloudClient(string baseAddress) =>
        new CloudHttpClient(HttpClientFactory.GetClient(
            baseAddress,
            ApiConstants.HttpRequestTimeout,
            ignoreSslErrors: true));
}
