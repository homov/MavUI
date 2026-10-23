using System.Net;
using System.Text;
using Hc.Services.Local;
using Xunit;

namespace Hc.Tests;

/// <summary>
/// Local device API (access point of the heater), without a device: a fake handler answers each request
/// from a queue and records what was sent.
/// </summary>
public class LocalDeviceTests
{
    private sealed class QueueHandler(params (HttpStatusCode Status, string Body)[] answers) : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _answers = new(answers);

        public List<(string Route, string? Body)> Sent { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Sent.Add((request.RequestUri!.AbsolutePath, body));

            var (status, text) = _answers.Dequeue();
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "text/plain") };
        }
    }

    private static (DeviceSetupService Setup, QueueHandler Handler) Create(params (HttpStatusCode, string)[] answers)
    {
        var handler = new QueueHandler(answers);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://10.10.10.1:10893/") };
        return (new DeviceSetupService(new LocalDeviceClient(http)), handler);
    }

    [Fact]
    public void ParseNetworks_SplitsByComma_AndDropsEmptyEntries()
    {
        var names = LocalDeviceClient.ParseNetworks("Home,,Cafe Wifi,");

        Assert.Equal(new[] { "Home", "Cafe Wifi" }, names);
    }

    [Fact]
    public async Task ReadDevice_ReturnsIdAndModel()
    {
        var (setup, _) = Create((HttpStatusCode.OK, "SN-42"), (HttpStatusCode.OK, "HC-Pro"));

        var info = await setup.ReadDeviceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new LocalDeviceInfo("SN-42", "HC-Pro"), info);
    }

    [Fact]
    public async Task ReadDevice_WhenNoAnswer_IsUnreachable()
    {
        var (setup, _) = Create((HttpStatusCode.InternalServerError, "down"));

        var ex = await Assert.ThrowsAsync<DeviceSetupException>(() => setup.ReadDeviceAsync(TestContext.Current.CancellationToken));

        Assert.Equal(DeviceSetupError.Unreachable, ex.Reason);
    }

    [Fact]
    public async Task ScanNetworks_ParsesDeviceAnswer()
    {
        var (setup, handler) = Create((HttpStatusCode.OK, "Home,Office"));

        var networks = await setup.ScanNetworksAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "Home", "Office" }, networks);
        Assert.Equal("/scan", handler.Sent.Single().Route);
    }

    [Fact]
    public async Task AttachToAccount_SendsAccountThenCredentials()
    {
        var (setup, handler) = Create((HttpStatusCode.OK, "accepted"), (HttpStatusCode.OK, "accepted"));

        await setup.AttachToAccountAsync("user-1", "Home", "pa ss", TestContext.Current.CancellationToken);

        Assert.Collection(handler.Sent,
            first =>
            {
                Assert.Equal("/user_id", first.Route);
                Assert.Equal("id=user-1", first.Body);
            },
            second =>
            {
                Assert.Equal("/data", second.Route);
                Assert.Contains("ssid=Home", second.Body);
                Assert.Contains("password=pa+ss", second.Body);
            });
    }

    [Fact]
    public async Task AttachToAccount_RejectedAccount_NeverSendsPassword()
    {
        var (setup, handler) = Create((HttpStatusCode.OK, "denied"));

        var ex = await Assert.ThrowsAsync<DeviceSetupException>(() => setup.AttachToAccountAsync("user-1", "Home", "secret", TestContext.Current.CancellationToken));

        Assert.Equal(DeviceSetupError.Rejected, ex.Reason);
        Assert.Single(handler.Sent);
        Assert.DoesNotContain(handler.Sent, sent => sent.Route == "/data");
    }

    [Fact]
    public async Task AttachToAccount_RejectedCredentials_IsReported()
    {
        var (setup, _) = Create((HttpStatusCode.OK, "accepted"), (HttpStatusCode.OK, "wrong network"));

        var ex = await Assert.ThrowsAsync<DeviceSetupException>(() => setup.AttachToAccountAsync("user-1", "Home", "bad", TestContext.Current.CancellationToken));

        Assert.Equal(DeviceSetupError.Rejected, ex.Reason);
    }
}
