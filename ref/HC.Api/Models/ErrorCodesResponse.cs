namespace HC.Api.Models;

public class ErrorCodesResponse : BaseResponse
{
    [JsonPropertyName("errors")]
    public Dictionary<string, string> Errors { get; set; } = new();
}
