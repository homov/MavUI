using Hc.Config;

namespace Hc.Services.Local;

/// <summary>
/// Client for the heating device's own access point (plain HTTP, <see cref="ApiEndpoints.LocalDeviceBaseUrl"/>).
/// The device answers with plain text. Requests run one at a time, like the cloud client.
/// Use this only while the phone is connected to the device's access point.
/// </summary>
public sealed class LocalDeviceClient(HttpClient http)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Shared client. Its base address is the device access point, so it is only useful during setup.</summary>
    public static LocalDeviceClient Instance { get; } = new(CreateHttpClient());

    private static HttpClient CreateHttpClient() => new()
    {
        // The trailing slash keeps relative routes ("dev_id") under the base address.
        BaseAddress = new Uri(ApiEndpoints.LocalDeviceBaseUrl.TrimEnd('/') + "/"),
        Timeout = ApiEndpoints.LocalDeviceTimeout,
    };

    /// <summary>Device serial number (route "dev_id"). Also used as a reachability check.</summary>
    public Task<ApiResponse<string>> GetDeviceIdAsync(CancellationToken ct = default) =>
        PostAsync("dev_id", null, ct);

    /// <summary>Device model, used to pick its picture (route "dev_ver").</summary>
    public Task<ApiResponse<string>> GetDeviceModelAsync(CancellationToken ct = default) =>
        PostAsync("dev_ver", null, ct);

    /// <summary>Raw scan result: network names separated by commas. Parse with <see cref="ParseNetworks"/>.</summary>
    public Task<ApiResponse<string>> ScanNetworksAsync(CancellationToken ct = default) =>
        PostAsync("scan", null, ct);

    /// <summary>Tells the device which account it belongs to (route "user_id").</summary>
    public Task<ApiResponse<string>> SetUserIdAsync(string userId, CancellationToken ct = default) =>
        PostAsync("user_id", Form(("id", userId)), ct);

    /// <summary>Gives the device the home Wi-Fi name and password (route "data"). The password is sent as is.</summary>
    public Task<ApiResponse<string>> SetWifiCredentialsAsync(string ssid, string password, CancellationToken ct = default) =>
        PostAsync("data", Form(("ssid", ssid), ("password", password)), ct);

    /// <summary>
    /// Splits the scan result into network names. Empty entries are dropped. Names are not trimmed,
    /// because an SSID may contain spaces.
    /// </summary>
    public static IReadOnlyList<string> ParseNetworks(string scanResult) =>
        scanResult.Split(',', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The device confirms a parameter with the word "accepted" in the body.</summary>
    public static bool IsAccepted(ApiResponse<string> response) =>
        response.IsSuccessStatusCode && response.Content is not null && response.Content.Contains("accepted", StringComparison.Ordinal);

    private async Task<ApiResponse<string>> PostAsync(string route, HttpContent? content, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var response = await http.PostAsync(route, content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return new ApiResponse<string>
            {
                StatusCode = response.StatusCode,
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                Content = body,
                ErrorMessage = response.IsSuccessStatusCode ? null : body,
            };
        }
        // Same rule as the cloud client: only the caller's cancellation is thrown; timeouts become failed responses.
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return new ApiResponse<string> { StatusCode = 0, IsSuccessStatusCode = false, ErrorMessage = ex.Message };
        }
        finally
        {
            _gate.Release();
        }
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Key, f.Value)));
}
