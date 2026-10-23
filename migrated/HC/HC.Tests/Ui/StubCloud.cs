using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Hc.Tests.Ui;

/// <summary>
/// A local stand-in for the cloud API, started once per test run. It answers the routes the UI tests use and
/// records what the app sent. The app reaches it through HC_API_BASE, which the test environment sets.
/// Only <see cref="KnownEmail"/> can sign in; any other account gets a server error.
/// </summary>
internal sealed class StubCloud
{
    public const string KnownEmail = "user@example.test";

    private readonly HttpListener _listener;
    private readonly object _gate = new();
    private readonly List<(string Route, string Body)> _requests = [];

    private StubCloud(int port)
    {
        BaseUrl = $"http://127.0.0.1:{port}/stinexapiv1";
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Start();
        _ = ServeAsync();
    }

    /// <summary>The address to put in HC_API_BASE (no trailing slash, like the default).</summary>
    public string BaseUrl { get; }

    /// <summary>The stub for this test run.</summary>
    public static StubCloud Instance { get; private set; } = null!;

    /// <summary>Starts the stub on a free loopback port. Call once, before the app reads its cloud address.</summary>
    public static void StartForRun()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        Instance = new StubCloud(port);
    }

    /// <summary>Every request received so far: the route (last path segment) and the form body.</summary>
    public IReadOnlyList<(string Route, string Body)> Requests
    {
        get
        {
            lock (_gate)
                return _requests.ToList();
        }
    }

    /// <summary>True when the app sent a sign-in request with this email.</summary>
    public bool SignInWasSent(string email) =>
        Requests.Any(r => r.Route == "user_authorization" && FormValue(r.Body, "email") == email);

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                return; // the listener was stopped: the test run is over
            }

            _ = Task.Run(() => Handle(context));
        }
    }

    private void Handle(HttpListenerContext context)
    {
        try
        {
            var route = context.Request.Url?.AbsolutePath.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty;
            string body;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
                body = reader.ReadToEnd();

            lock (_gate)
                _requests.Add((route, body));

            var bytes = Encoding.UTF8.GetBytes(Answer(route, body));
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private static string Answer(string route, string body) => route switch
    {
        "user_authorization" when FormValue(body, "email") == KnownEmail =>
            """{"state":"ok","uuid":"user-1"}""",
        "user_authorization" =>
            """{"state":"error","status":"error","description":"Wrong email or password","code":"auth"}""",
        "get_user_groups" => """{"state":"no_groups","groups":[]}""",
        "get_user_devices" => """{"state":"ok","devices":[]}""",
        _ => """{"state":"ok"}""",
    };

    private static string? FormValue(string body, string key)
    {
        foreach (var pair in body.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && Uri.UnescapeDataString(parts[0].Replace('+', ' ')) == key)
                return Uri.UnescapeDataString(parts[1].Replace('+', ' '));
        }
        return null;
    }
}
