namespace HC.Api.Models;

public class GetDeviceTypesResponse : BaseResponse
{
    [JsonPropertyName("device_types")]
    public List<DeviceTypeInfo> DeviceTypes { get; set; } = new();
}

public class DeviceTypeInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("picture")]
    public string Picture { get; set; } = "";

    [JsonPropertyName("temp_min")]
    public int? MinTemperature { get; set; }

    [JsonPropertyName("temp_max")]
    public int? MaxTemperature { get; set; }

    [JsonPropertyName("humi_min")]
    public int? MinHumidity { get; set; }

    [JsonPropertyName("humi_max")]
    public int? MaxHumidity { get; set; }

    [JsonPropertyName("gis_min")]
    public int? MinHysteresis { get; set; }

    [JsonPropertyName("gis_max")]
    public int? MaxHysteresis { get; set; }

    [JsonPropertyName("control_type")]
    public string[]? ControlType { get; set; }

    [JsonIgnore]
    public string ImageUrl => string.IsNullOrEmpty(Picture) ? "" : $"{ApiConstants.ImageBaseUrl}/{Picture}";
}
