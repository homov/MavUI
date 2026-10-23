namespace HC.Api.Models;

public class LoginResponse : BaseResponse
{
    [JsonPropertyName("uuid")]
    public string UserId { get; set; } = "";
}
