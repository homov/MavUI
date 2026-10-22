using System.Text.Json.Serialization;

namespace HeatCeram;

public class LoginResponse : BaseResponse
{

    [JsonPropertyName("uuid")]
    public string UserId { get;set;}
}
