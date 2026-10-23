using System.Text.Json.Serialization;

namespace Hc.Models.Cloud;

/// <summary>Values of the <c>state</c> field returned by every cloud method.</summary>
public static class CloudStates
{
    public const string Ok = "ok";
    public const string NoGroups = "no_groups";

    /// <summary>Description the server returns when the session is not authorized.</summary>
    public const string AuthorizationError = "authorization error";
}

/// <summary>Common part of every cloud response.</summary>
public class BaseResponse
{
    [JsonPropertyName("state")]
    public string State { get; set; } = CloudStates.Ok;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>True when the server reports success. "no_groups" is also a success: the user simply has no groups.</summary>
    [JsonIgnore]
    public bool IsSuccess => State is CloudStates.Ok or CloudStates.NoGroups;

    [JsonIgnore]
    public bool IsAuthorizationError => Description == CloudStates.AuthorizationError;
}

/// <summary>A user who has access to a device or group.</summary>
public class SharingInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
