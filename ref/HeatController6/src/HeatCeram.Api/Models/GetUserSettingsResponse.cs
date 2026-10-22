using System.Text.Json.Serialization;

namespace HeatCeram.Api.Models;

public class GetUserSettingsResponse : BaseResponse
{
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}
