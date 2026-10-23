namespace HC.Api.Models;

public class TimezoneData : List<Dictionary<string, string>>
{
    public List<string> GetNames()
    {
        var names = new List<string>();
        foreach (var item in this)
            foreach (var value in item.Values)
                names.Add(value);
        return names;
    }
}

public class GetTimezonesResponse : BaseResponse
{
    [JsonPropertyName("timezones")]
    public TimezoneData Timezones { get; set; } = new();
}
