using System.Text.Json.Serialization;

namespace HeatCeram.Api.Models;

public class ErrorCodesResponse: BaseResponse
{
    [JsonPropertyName("errors")]
    public Dictionary<string, string> Errors { get; set; }
}
