using Hc.Services.Locations;
using Xunit;

namespace Hc.Tests;

public class LocationServiceTests
{
    [Fact]
    public async Task SetSensor_SendsGroupAndSensor()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok"}""");
        var service = new LocationService(TestCloud.Create(handler));

        await service.SetSensorAsync("g-1", "s-1", TestContext.Current.CancellationToken);

        var body = handler.Sent.Single().Body!;
        Assert.Contains("group_id=g-1", body);
        Assert.Contains("sensor_id=s-1", body);
    }

    [Fact]
    public async Task SetSensor_WithoutSensor_OmitsSensorField()
    {
        var handler = ScriptedHandler.Json("""{"state":"ok"}""");
        var service = new LocationService(TestCloud.Create(handler));

        await service.SetSensorAsync("g-1", null, TestContext.Current.CancellationToken);

        var body = handler.Sent.Single().Body!;
        Assert.Contains("group_id=g-1", body);
        Assert.DoesNotContain("sensor_id", body);
    }
}
