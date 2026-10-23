using System.Net;
using System.Text;
using Hc.Services.Cloud;
using Mav.Essentials;

namespace Hc.Tests;

/// <summary>Answers each cloud request with a script and records what was sent. No network is used.</summary>
internal sealed class ScriptedHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> script) : HttpMessageHandler
{
    public List<(string Route, string? Body)> Sent { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Sent.Add((request.RequestUri!.AbsolutePath, body));
        return script(request, body);
    }

    /// <summary>Always answers 200 with the given JSON.</summary>
    public static ScriptedHandler Json(string json) => new((_, _) => Ok(json));

    public static HttpResponseMessage Ok(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

/// <summary>Cloud client over a scripted handler, using the same base address as production routes.</summary>
internal static class TestCloud
{
    public static CloudClient Create(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/stinexapiv1/") });
}

/// <summary>In-memory secure storage for tests.</summary>
internal sealed class FakeSecureStorage : IMavSecureStorage
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public bool Remove(string key) => _values.Remove(key);

    public void RemoveAll() => _values.Clear();
}

/// <summary>Preferences in a temporary folder, deleted with the test.</summary>
internal sealed class TempPreferences : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "hc-tests-" + Guid.NewGuid().ToString("N"));

    public TempPreferences() => Preferences = new JsonFilePreferences(_directory);

    public IMavPreferences Preferences { get; }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
