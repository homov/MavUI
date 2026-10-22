using System.Text.Json;
using System.Text.Json.Nodes;

namespace Mav.Essentials;

/// <summary>
/// Preferences stored as one JSON file per container in a directory.
/// Writes go to a temporary file first, then replace the target, so a crash cannot leave a half-written file.
/// </summary>
public sealed class JsonFilePreferences : IMavPreferences
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly string _directory;
    private readonly object _gate = new();
    private readonly Dictionary<string, JsonObject> _containers = new(StringComparer.Ordinal);

    public JsonFilePreferences(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
    }

    public T Get<T>(string key, T defaultValue, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        MavPreferenceValues.ThrowIfUnsupported<T>();

        lock (_gate)
        {
            var container = Load(MavPreferenceValues.ResolveContainer(sharedName));
            if (container[key] is JsonValue value && value.TryGetValue<T>(out var result))
            {
                return result;
            }

            return defaultValue;
        }
    }

    public void Set<T>(string key, T value, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        MavPreferenceValues.ThrowIfUnsupported<T>();

        lock (_gate)
        {
            var name = MavPreferenceValues.ResolveContainer(sharedName);
            var container = Load(name);
            if (value is null)
            {
                container.Remove(key);
            }
            else
            {
                container[key] = JsonValue.Create(value);
            }

            Save(name, container);
        }
    }

    public bool ContainsKey(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_gate)
        {
            return Load(MavPreferenceValues.ResolveContainer(sharedName)).ContainsKey(key);
        }
    }

    public void Remove(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_gate)
        {
            var name = MavPreferenceValues.ResolveContainer(sharedName);
            var container = Load(name);
            if (container.Remove(key))
            {
                Save(name, container);
            }
        }
    }

    public void Clear(string? sharedName = null)
    {
        lock (_gate)
        {
            var name = MavPreferenceValues.ResolveContainer(sharedName);
            var container = new JsonObject();
            _containers[name] = container;
            Save(name, container);
        }
    }

    private JsonObject Load(string name)
    {
        if (_containers.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var path = PathFor(name);
        JsonObject container;
        if (File.Exists(path))
        {
            try
            {
                container = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
            }
            catch (JsonException)
            {
                // Keep the damaged file for inspection instead of silently overwriting it.
                File.Move(path, $"{path}.corrupt-{DateTime.UtcNow.Ticks}", overwrite: true);
                container = new JsonObject();
            }
        }
        else
        {
            container = new JsonObject();
        }

        _containers[name] = container;
        return container;
    }

    private void Save(string name, JsonObject container)
    {
        Directory.CreateDirectory(_directory);
        var path = PathFor(name);
        var temp = path + ".tmp";
        File.WriteAllText(temp, container.ToJsonString(WriteOptions));
        File.Move(temp, path, overwrite: true);
    }

    private string PathFor(string name) => Path.Combine(_directory, name + ".json");
}
