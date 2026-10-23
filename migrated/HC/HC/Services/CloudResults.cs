using Hc.Models.Cloud;

namespace Hc.Services;

/// <summary>Turns a raw cloud response into its body or a <see cref="CloudRequestException"/>.</summary>
public static class CloudResults
{
    /// <summary>
    /// Returns the body of a successful call. Checks in order: transport, HTTP status, empty body,
    /// the server state, and finally the authorization error (the server reports it inside a 200 response).
    /// </summary>
    public static T Require<T>(ApiResponse<T> response) where T : BaseResponse
    {
        var body = RequireHttp(response);

        if (body.IsAuthorizationError)
            throw new CloudRequestException(CloudErrorKind.Authorization, body.Description);

        if (!body.IsSuccess)
            throw new CloudRequestException(CloudErrorKind.Server, string.IsNullOrEmpty(body.Description) ? body.ErrorCode : body.Description);

        return body;
    }

    /// <summary>
    /// For bodies without the common state fields (for example <c>UserDeviceInfo</c>). Checks only transport,
    /// HTTP status and an empty body.
    /// </summary>
    public static T RequireHttp<T>(ApiResponse<T> response)
    {
        if ((int)response.StatusCode == 0)  // 0 = the request never reached the server
            throw new CloudRequestException(CloudErrorKind.Transport, response.ErrorMessage ?? "No connection to the server");

        if (!response.IsSuccessStatusCode)
            throw new CloudRequestException(CloudErrorKind.Http, response.ErrorMessage ?? $"HTTP {(int)response.StatusCode}");

        return response.Content ?? throw new CloudRequestException(CloudErrorKind.Http, "Empty response");
    }
}
