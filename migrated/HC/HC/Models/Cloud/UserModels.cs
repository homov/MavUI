using System.Text.Json.Serialization;

namespace Hc.Models.Cloud;

public class LoginResponse : BaseResponse
{
    /// <summary>Server user id (field "uuid").</summary>
    [JsonPropertyName("uuid")]
    public string UserId { get; set; } = string.Empty;
}

public class GetUserSettingsResponse : BaseResponse
{
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
}

public class GetTimezonesResponse : BaseResponse
{
    /// <summary>
    /// The server sends a list of single-entry objects, for example [{"Europe/Kyiv": "Europe/Kyiv"}].
    /// Use <see cref="Names"/> to get the flat list of zone names.
    /// </summary>
    [JsonPropertyName("timezones")]
    public List<Dictionary<string, string>> Timezones { get; set; } = [];

    /// <summary>All zone names, in the order received.</summary>
    [JsonIgnore]
    public IEnumerable<string> Names => Timezones.SelectMany(item => item.Values);
}
