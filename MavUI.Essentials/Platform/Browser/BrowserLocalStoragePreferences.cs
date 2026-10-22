using System.Globalization;
using System.Text.Json;
using System.Runtime.Versioning;

namespace Mav.Essentials.Browser;

/// <summary>
/// Preferences backed by <c>window.localStorage</c>. Values are stored as plain text, per origin, and
/// survive reloads. Each value is stored with a one-character type tag, so <c>Get&lt;int&gt;</c> never
/// reads a string back as a number. Key names are tracked in an index entry because localStorage cannot
/// be enumerated through the interop surface used here.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BrowserLocalStoragePreferences : IMavPreferences
{
    private const string Prefix = "mav.essentials.";
    private const string IndexKey = "mav.essentials.__index";
    // Unit separator between the container name and the key; it does not occur in normal names.
    private const char Separator = '\u001f';

    private readonly object _gate = new();

    public T Get<T>(string key, T defaultValue, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        MavPreferenceValues.ThrowIfUnsupported<T>();

        var raw = BrowserStorage.GetItem(StorageKey(key, sharedName));
        if (raw is null)
            return defaultValue;

        return Decode(raw, defaultValue);
    }

    public void Set<T>(string key, T value, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        MavPreferenceValues.ThrowIfUnsupported<T>();

        if (value is null)
        {
            Remove(key, sharedName);
            return;
        }

        var storageKey = StorageKey(key, sharedName);
        lock (_gate)
        {
            BrowserStorage.SetItem(storageKey, Encode(value));
            var index = ReadIndex();
            if (!index.Contains(storageKey))
            {
                index.Add(storageKey);
                WriteIndex(index);
            }
        }
    }

    public bool ContainsKey(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return BrowserStorage.GetItem(StorageKey(key, sharedName)) is not null;
    }

    public void Remove(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var storageKey = StorageKey(key, sharedName);
        lock (_gate)
        {
            BrowserStorage.RemoveItem(storageKey);
            var index = ReadIndex();
            if (index.Remove(storageKey))
                WriteIndex(index);
        }
    }

    public void Clear(string? sharedName = null)
    {
        var containerPrefix = Prefix + (sharedName ?? string.Empty) + Separator;
        lock (_gate)
        {
            var index = ReadIndex();
            var kept = new List<string>();
            foreach (var storageKey in index)
            {
                if (storageKey.StartsWith(containerPrefix, StringComparison.Ordinal))
                    BrowserStorage.RemoveItem(storageKey);
                else
                    kept.Add(storageKey);
            }

            WriteIndex(kept);
        }
    }

    private static string StorageKey(string key, string? sharedName) =>
        Prefix + (sharedName ?? string.Empty) + Separator + key;

    private static List<string> ReadIndex()
    {
        var raw = BrowserStorage.GetItem(IndexKey);
        if (string.IsNullOrEmpty(raw))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? [];
        }
        catch (JsonException)
        {
            // A damaged index only loses the bookkeeping for Clear; values themselves stay readable.
            return [];
        }
    }

    private static void WriteIndex(List<string> index) =>
        BrowserStorage.SetItem(IndexKey, JsonSerializer.Serialize(index));

    private static char TagOf<T>() => typeof(T) switch
    {
        var t when t == typeof(string) => 's',
        var t when t == typeof(bool) => 'b',
        var t when t == typeof(int) => 'i',
        var t when t == typeof(long) => 'l',
        var t when t == typeof(float) => 'f',
        var t when t == typeof(double) => 'd',
        _ => throw new NotSupportedException($"Type {typeof(T).Name} is not supported."),
    };

    private static string Encode<T>(T value)
    {
        object boxed = value!;
        var text = boxed switch
        {
            bool b => b ? "true" : "false",
            _ => Convert.ToString(boxed, CultureInfo.InvariantCulture) ?? string.Empty,
        };
        return TagOf<T>() + ":" + text;
    }

    private static T Decode<T>(string raw, T defaultValue)
    {
        if (raw.Length < 2 || raw[0] != TagOf<T>() || raw[1] != ':')
            return defaultValue;

        var payload = raw[2..];
        object? parsed = typeof(T) switch
        {
            var t when t == typeof(string) => payload,
            var t when t == typeof(bool) => bool.TryParse(payload, out var b) ? (object)b : null,
            var t when t == typeof(int) => int.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? (object)i : null,
            var t when t == typeof(long) => long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? (object)l : null,
            var t when t == typeof(float) => float.TryParse(payload, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? (object)f : null,
            var t when t == typeof(double) => double.TryParse(payload, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? (object)d : null,
            _ => null,
        };

        return parsed is null ? defaultValue : (T)parsed;
    }
}
