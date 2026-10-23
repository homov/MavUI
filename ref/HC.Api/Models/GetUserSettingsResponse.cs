namespace HC.Api.Models;

public class GetUserSettingsResponse : BaseResponse
{
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}
