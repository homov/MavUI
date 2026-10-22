using System.Text.Json.Serialization;

namespace HeatCeram.Api.Models;

public class SharingInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;
    [JsonPropertyName("email")]
    public string Email { get; set; } = String.Empty;
}