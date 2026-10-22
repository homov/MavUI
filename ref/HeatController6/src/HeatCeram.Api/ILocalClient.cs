using Refit;
using System;
using System.Collections.Generic;
using System.Text;

namespace HeatCeram.Api;

[Headers("User-Agent: HeatController", "Accept-Encoding: gzip", "Accept: application/json, text/html")]
public interface ILocalClient
{
    [Post("/dev_id")]
    Task<ApiResponse<string>> GetDeviceId();

    [Post("/dev_ver")]
    Task<ApiResponse<string>> GetDeviceModel();

    [Post("/scan")]
    Task<ApiResponse<string>> GetWifiList();

    [Post("/data")]
    Task<ApiResponse<string>> SetDeviceParameters(string ssid, string password);

    [Post("/user_id")]
    Task<ApiResponse<string>> SetUserId(string id);
}
