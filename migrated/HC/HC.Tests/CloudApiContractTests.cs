using System.Net;
using System.Text;
using Hc.Models.Cloud;
using Hc.Services.Cloud;
using Xunit;

namespace Hc.Tests;

/// <summary>
/// Checks the wire contract of <see cref="CloudClient"/> without a network: a fake handler records the
/// request and returns canned JSON. Field names must match what the server expects.
/// </summary>
public class CloudApiContractTests
{
    private sealed class FakeHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            if (request.Content is not null)
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (CloudClient Client, FakeHandler Handler) Create(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHandler(status, json);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/stinexapiv1/") };
        return (new CloudClient(http), handler);
    }

    [Fact]
    public async Task Login_SendsServerFieldNames_AndReadsUserId()
    {
        var (client, handler) = Create("""{"state":"ok","uuid":"user-123"}""");

        var response = await client.Login(new LoginRequest("user@example.com", "secret"), TestContext.Current.CancellationToken);

        Assert.Equal("/stinexapiv1/user_authorization/", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Contains("email=user%40example.com", handler.RequestBody);
        Assert.Contains("password=secret", handler.RequestBody);
        Assert.DoesNotContain("language", handler.RequestBody);
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("user-123", response.Content!.UserId);
        Assert.True(response.Content.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_UsesNewPasswordField()
    {
        var (client, handler) = Create("""{"state":"ok"}""");

        await client.ChangePassword(new ChangePasswordRequest("a@b.c", "old", "new"), TestContext.Current.CancellationToken);

        Assert.Contains("new_password=new", handler.RequestBody);
    }

    [Fact]
    public async Task GetUserDevices_ParsesDeviceFields()
    {
        var json = """
        {"state":"ok","devices":[{"device_id":"d1","device_name":"Kitchen","heat_state":true,
          "temp_panel":21.5,"control_type":["time","temperature"],"t1_coef":0.5}]}
        """;
        var (client, _) = Create(json);

        var response = await client.GetUserDevices(TestContext.Current.CancellationToken);

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
        var (client, _) = Create("""{"state":"no_groups","groups":[]}""");

        var response = await client.GetUserGroups(TestContext.Current.CancellationToken);

        Assert.True(response.Content!.IsSuccess);
        Assert.Empty(response.Content.Groups);
    }

    [Fact]
    public async Task UpdateSchedule_SendsJson_WithoutEmptyReceiverFields()
    {
        var (client, handler) = Create("""{"state":"ok"}""");
        var schedule = new ScheduleInfo
        {
            ScheduleId = "s1",
            ScheduleType = "time",
            Week = new Dictionary<string, List<HourRecord>>
            {
                [WeekDays.Monday] = [new HourRecord { Hour = 6, Temperature = 22 }],
            },
        };

        await client.UpdateSchedule(schedule, TestContext.Current.CancellationToken);

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
        var (client, _) = Create("""{"state":"ok","timezones":[{"Europe/Kyiv":"Europe/Kyiv"},{"UTC":"UTC"}]}""");

        var response = await client.GetTimezones(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "Europe/Kyiv", "UTC" }, response.Content!.Names);
    }

    [Fact]
    public async Task GetDeviceInfo_ParsesSingleDevice()
    {
        var (client, handler) = Create("""{"state":"ok","device_id":"d7","device_name":"Bath","temp_panel":19}""");

        var response = await client.GetDeviceInfo(new DeviceRequest("d7"), TestContext.Current.CancellationToken);

        Assert.Contains("device_id=d7", handler.RequestBody);
        Assert.Equal("Bath", response.Content!.DeviceName);
        Assert.Equal(19f, response.Content.PanelTemperature);
    }

    [Fact]
    public async Task GetAllErrorCodes_ParsesNestedDictionary()
    {
        var (client, _) = Create("""{"state":"ok","errors":{"en":{"E1":"Sensor fault"},"uk":{"E1":"Збій датчика"}}}""");

        var response = await client.GetAllErrorCodes(TestContext.Current.CancellationToken);

        Assert.Equal("Збій датчика", response.Content!.Errors["uk"]["E1"]);
    }

    [Fact]
    public async Task HttpError_IsReturnedNotThrown()
    {
        var (client, _) = Create("server down", HttpStatusCode.InternalServerError);

        var response = await client.GetUserDevices(TestContext.Current.CancellationToken);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("server down", response.ErrorMessage);
        Assert.Null(response.Content);
    }

    [Fact]
    public async Task Timeout_IsReturnedAsFailure()
    {
        var handler = new TimeoutHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/stinexapiv1/") };
        var client = new CloudClient(http);

        var response = await client.GetUserDevices(TestContext.Current.CancellationToken);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal((HttpStatusCode)0, response.StatusCode);
    }

    [Fact]
    public async Task CallerCancellation_IsThrown()
    {
        var (client, _) = Create("""{"state":"ok"}""");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetUserDevices(cts.Token));
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("timeout"));
    }

    [Fact]
    public async Task MalformedJson_IsReportedAsHttpFormatError_NotAsLostConnection()
    {
        var (client, _) = Create("this is not json");

        var response = await client.Login(new LoginRequest("user@example.com", "secret"), TestContext.Current.CancellationToken);

        Assert.False(response.IsSuccessStatusCode);
        Assert.NotEqual(0, (int)response.StatusCode);
        Assert.Equal("Unexpected response format", response.ErrorMessage);
    }
}
