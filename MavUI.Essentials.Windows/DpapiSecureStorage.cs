using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Mav.Essentials.Windows;

/// <summary>
/// SecureStorage that protects each value with DPAPI (<see cref="DataProtectionScope.CurrentUser"/>).
/// Values are kept in one JSON file; only the encrypted blobs are written to disk.
/// </summary>
/// <remarks>
/// Only the same Windows user on the same machine can decrypt the values. A damaged file is not overwritten:
/// the reads throw until the file is repaired or removed.
/// </remarks>
public sealed class DpapiSecureStorage : IMavSecureStorage
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MavUI.Essentials.SecureStorage.v1");

    private readonly string _directory;
    private readonly object _gate = new();

    public DpapiSecureStorage(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
    }

    private string FilePath => Path.Combine(_directory, "secure.json");

    public Task<string?> GetAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_gate)
        {
            var stored = Load()[key]?.GetValue<string>();
            if (stored is null)
            {
                return Task.FromResult<string?>(null);
            }

            var plain = ProtectedData.Unprotect(Convert.FromBase64String(stored), Entropy, DataProtectionScope.CurrentUser);
            return Task.FromResult<string?>(Encoding.UTF8.GetString(plain));
        }
    }

    public Task SetAsync(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        lock (_gate)
        {
            var root = Load();
            var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser);
            root[key] = Convert.ToBase64String(protectedBytes);
            Save(root);
        }

        return Task.CompletedTask;
    }

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_gate)
        {
            var root = Load();
            if (!root.Remove(key))
            {
                return false;
            }

            Save(root);
            return true;
        }
    }

    public void RemoveAll()
    {
        lock (_gate)
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }

    private JsonObject Load()
    {
        if (!File.Exists(FilePath))
        {
            return new JsonObject();
        }

        var root = JsonNode.Parse(File.ReadAllText(FilePath)) as JsonObject;
        return root ?? throw new InvalidDataException($"Secure storage file is damaged: {FilePath}");
    }

    private void Save(JsonObject root)
    {
        Directory.CreateDirectory(_directory);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, root.ToJsonString());
        File.Move(temp, FilePath, overwrite: true);
    }
}
