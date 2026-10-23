using System.Net;
using System.Net.Http;
using System.Text;
using Hc.Models.Cloud;
using Hc.Services.Cloud;
using Refit;
using Xunit;

namespace Hc.Tests;

/// <summary>
/// Checks the wire contract of <see cref="ICloudApi"/> without a network: a fake handler records the
/// request and returns canned JSON. Field names must match what the server expects.
/// </summary>
public class CloudApiContractTests
{
    private sealed class FakeHandler(string json) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            if (request.Content is not null)
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (ICloudApi Api, FakeHandler Handler) Create(string json)
    {
        var handler = new FakeHandler(json);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/stinexapiv1") };
        return (RestService.For<ICloudApi>(client), handler);
    }

    [Fact]
    public async Task Login_SendsServerFieldNames_AndReadsUserId()
    {
        var (api, handler) = Create("""{"state":"ok","uuid":"user-123"}""");

        var response = await api.Login(new LoginRequest("user@example.com", "secret", "uk"));

        Assert.Equal("/stinexapiv1/user_authorization/", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Contains("email=user%40example.com", handler.RequestBody);
        Assert.Contains("password=secret", handler.RequestBody);
        Assert.Contains("language=uk", handler.RequestBody);
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("user-123", response.Content!.UserId);
        Assert.True(response.Content.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_UsesNewPasswordField()
    {
        var (api, handler) = Create("""{"state":"ok"}""");

        await api.ChangePassword(new ChangePasswordRequest("a@b.c", "old", "new"));

        Assert.Contains("new_password=new", handler.RequestBody);
    }

    [Fact]
    public async Task GetUserDevices_ParsesDeviceFields()
    {
        var json = """
        {"state":"ok","devices":[{"device_id":"d1","device_name":"Kitchen","heat_state":true,
          "temp_panel":21.5,"control_type":["time","temperature"],"t1_coef":0.5}]}
        """;
        var (api, _) = Create(json);

        var response = await api.GetUserDevices();

        var device = Assert.Single(response.Content!.Devices);
        Assert.Equal("d1", device.DeviceId);
        Assert.Equal("Kitchen", device.DeviceName);
        Assert.True(device.HeatState);
        Assert.Equal(21.5f, device.PanelTemperature);
        Assert.Equal(new[] { "time", "temperature" }, device.ControlTypes);
        Assert.Equal(0.5f, device.RoomCoefficient);
    }

    [Fact]
    public async Task GetUserGroups_NoGroupsStateIsSuccess()
    {
        var (api, _) = Create("""{"state":"no_groups","groups":[]}""");

        var response = await api.GetUserGroups();

        Assert.True(response.Content!.IsSuccess);
        Assert.Empty(response.Content.Groups);
    }

    [Fact]
    public async Task UpdateSchedule_SendsJson_WithoutEmptyReceiverFields()
    {
        var (api, handler) = Create("""{"state":"ok"}""");
        var schedule = new ScheduleInfo
        {
            ScheduleId = "s1",
            ScheduleType = "time",
            Week = new Dictionary<string, List<HourRecord>>
            {
                [WeekDays.Monday] = [new HourRecord { Hour = 6, Temperature = 22 }],
            },
        };

        await api.UpdateSchedule(schedule);

        Assert.Contains("\"schedule_id\":\"s1\"", handler.RequestBody);
        Assert.Contains("\"monday\"", handler.RequestBody);
        Assert.Contains("\"t\":22", handler.RequestBody);
        Assert.DoesNotContain("reciver", handler.RequestBody);
        Assert.DoesNotContain("\"p\"", handler.RequestBody);
        Assert.DoesNotContain("\"s\"", handler.RequestBody);
    }

    [Fact]
    public async Task GetTimezones_FlattensNames()
    {
        var (api, _) = Create("""{"state":"ok","timezones":[{"Europe/Kyiv":"Europe/Kyiv"},{"UTC":"UTC"}]}""");

        var response = await api.GetTimezones();

        Assert.Equal(new[] { "Europe/Kyiv", "UTC" }, response.Content!.Names);
    }
}
