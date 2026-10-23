namespace HC.Api;

public interface ILocalClient
{
    Task<ApiResponse<string>> GetDeviceId(CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> GetDeviceModel(CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> GetWifiList(CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> SetDeviceParameters(string ssid, string password, CancellationToken cancellationToken = default);

    Task<ApiResponse<string>> SetUserId(string id, CancellationToken cancellationToken = default);
}

public sealed class LocalHttpClient : ILocalClient
{
    private readonly HttpClient _http;

    public LocalHttpClient(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public Task<ApiResponse<string>> GetDeviceId(CancellationToken ct = default)
        => SendAsync("/dev_id", null, ct);

    public Task<ApiResponse<string>> GetDeviceModel(CancellationToken ct = default)
        => SendAsync("/dev_ver", null, ct);

    public Task<ApiResponse<string>> GetWifiList(CancellationToken ct = default)
        => SendAsync("/scan", null, ct);

    public Task<ApiResponse<string>> SetDeviceParameters(string ssid, string password, CancellationToken ct = default)
        => SendAsync("/data", new[] { new KeyValuePair<string, string>("ssid", ssid), new KeyValuePair<string, string>("password", password) }, ct);

    public Task<ApiResponse<string>> SetUserId(string id, CancellationToken ct = default)
        => SendAsync("/user_id", new[] { new KeyValuePair<string, string>("id", id) }, ct);

    private async Task<ApiResponse<string>> SendAsync(string route, IEnumerable<KeyValuePair<string, string>>? fields, CancellationToken ct)
    {
        HttpContent? content = fields is null ? null : new FormUrlEncodedContent(fields);
        try
        {
            using var response = await _http.PostAsync(route, content, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return new ApiResponse<string> { StatusCode = response.StatusCode, IsSuccessStatusCode = response.IsSuccessStatusCode, Content = body };
        }
        catch (Exception ex)
        {
            return new ApiResponse<string> { StatusCode = (HttpStatusCode)0, IsSuccessStatusCode = false, ErrorMessage = ex.Message };
        }
    }
}

public class LocalClient
{
    private static readonly Lazy<ILocalClient> DefaultClient = new(() =>
        new LocalHttpClient(HttpClientFactory.GetClient(
            ApiConstants.LocalDeviceBaseAddress,
            ApiConstants.LocalRequestTimeout)));

    public ILocalClient Api => DefaultClient.Value;
}
