namespace HC.Api.Models;

public class AllErrorCodesResponse : BaseResponse
{
    [JsonPropertyName("errors")]
    public Dictionary<string, Dictionary<string, string>> Errors { get; set; } = new();
}
