using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mav.Essentials;
using Mav.Essentials.Linux;
using Xunit;

namespace Mav.Tests;

public sealed class JsonFilePreferencesTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "mav-essentials-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Round_trips_every_supported_type()
    {
        var prefs = new JsonFilePreferences(_directory);
        prefs.Set("s", "text");
        prefs.Set("b", true);
        prefs.Set("i", 42);
        prefs.Set("l", 1L << 40);
        prefs.Set("f", 1.5f);
        prefs.Set("d", 2.25);

        Assert.Equal("text", prefs.Get("s", "x"));
        Assert.True(prefs.Get("b", false));
        Assert.Equal(42, prefs.Get("i", 0));
        Assert.Equal(1L << 40, prefs.Get("l", 0L));
        Assert.Equal(1.5f, prefs.Get("f", 0f));
        Assert.Equal(2.25, prefs.Get("d", 0d));
    }

    [Fact]
    public void Values_persist_across_instances()
    {
        new JsonFilePreferences(_directory).Set("theme", "dark");

        Assert.Equal("dark", new JsonFilePreferences(_directory).Get("theme", "system"));
    }

    [Fact]
    public void Missing_or_mismatched_keys_return_the_default()
    {
        var prefs = new JsonFilePreferences(_directory);
        prefs.Set("name", "text");

        Assert.Equal("fallback", prefs.Get("missing", "fallback"));
        Assert.Equal(7, prefs.Get("name", 7));
    }

    [Fact]
    public void Shared_containers_are_isolated()
    {
        var prefs = new JsonFilePreferences(_directory);
        prefs.Set("k", "default-value");
        prefs.Set("k", "shared-value", "widget");

        Assert.Equal("default-value", prefs.Get("k", ""));
        Assert.Equal("shared-value", prefs.Get("k", "", "widget"));
    }

    [Fact]
    public void Remove_and_Clear_delete_values()
    {
        var prefs = new JsonFilePreferences(_directory);
        prefs.Set("a", "1");
        prefs.Set("b", "2");

        prefs.Remove("a");
        Assert.False(prefs.ContainsKey("a"));
        Assert.True(prefs.ContainsKey("b"));

        prefs.Clear();
        Assert.False(prefs.ContainsKey("b"));
    }

    [Fact]
    public void Setting_null_removes_the_key()
    {
        var prefs = new JsonFilePreferences(_directory);
        prefs.Set<string?>("name", "value");
        prefs.Set<string?>("name", null);

        Assert.False(prefs.ContainsKey("name"));
    }

    [Fact]
    public void Unsupported_value_types_are_rejected()
    {
        var prefs = new JsonFilePreferences(_directory);

        Assert.Throws<NotSupportedException>(() => prefs.Set<object>("k", new object()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("..")]
    [InlineData("-leading-dash")]
    public void Invalid_shared_names_are_rejected(string name)
    {
        var prefs = new JsonFilePreferences(_directory);

        Assert.ThrowsAny<ArgumentException>(() => prefs.Set("k", "v", name));
    }
}

public sealed class SecureStorageFacadeTests
{
    [Fact]
    public async Task Without_a_platform_backend_every_call_fails()
    {
        MavSecureStorage.SetDefault(null);

        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => MavSecureStorage.Default.GetAsync("token"));
    }

    [Fact]
    public async Task Facade_delegates_to_the_registered_backend()
    {
        MavSecureStorage.SetDefault(new InMemorySecureStorage());
        try
        {
            await MavSecureStorage.Default.SetAsync("token", "abc");

            Assert.Equal("abc", await MavSecureStorage.Default.GetAsync("token"));
            Assert.Null(await MavSecureStorage.Default.GetAsync("missing"));
        }
        finally
        {
            MavSecureStorage.SetDefault(null);
        }
    }

    private sealed class InMemorySecureStorage : IMavSecureStorage
    {
        private readonly Dictionary<string, string> _values = new();

        public Task<string?> GetAsync(string key) =>
            Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

        public Task SetAsync(string key, string value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public bool Remove(string key) => _values.Remove(key);

        public void RemoveAll() => _values.Clear();
    }
}

public sealed class LibsecretOutputTests
{
    [Fact]
    public void Parses_account_names_from_search_output()
    {
        const string output =
            "[/org/freedesktop/secrets/collection/login/1]\n" +
            "label = MavUI.Essentials secret\n" +
            "secret = \n" +
            "created = 2026-10-22 10:00:00\n" +
            "attribute.service = MavUI.Essentials\n" +
            "attribute.account = refresh-token\n" +
            "\n" +
            "[/org/freedesktop/secrets/collection/login/2]\n" +
            "attribute.service = MavUI.Essentials\r\n" +
            "attribute.account = api-key\r\n";

        var accounts = LibsecretOutput.ParseAccounts(output);

        Assert.Equal(new[] { "refresh-token", "api-key" }, accounts);
    }

    [Fact]
    public void Empty_output_yields_no_accounts()
    {
        Assert.Empty(LibsecretOutput.ParseAccounts(""));
    }
}
