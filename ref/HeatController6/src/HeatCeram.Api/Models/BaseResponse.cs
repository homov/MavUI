using System.Text.Json.Serialization;

namespace HeatCeram.Api.Models;

public class BaseResponse
{
    [JsonPropertyName("state")]
    public string State { get; set; } = "ok";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("code")]
    public string ErrorCode { get; set; } = "";

    [JsonIgnore]
    public bool IsSuccess
    {
        get
        {
            return State == ApiConstants.StateOk || State == ApiConstants.StateNoGroups;
        }
    }

    [JsonIgnore]
    public bool IsAuthorizationError
    {
        get
        {
            return !String.IsNullOrEmpty(Description) && Description == ApiConstants.DescriptionAuthorizationError;
        }
    }
}
