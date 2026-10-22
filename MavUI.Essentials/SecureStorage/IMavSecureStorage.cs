namespace Mav.Essentials;

/// <summary>
/// Encrypted key/value storage for secrets such as tokens.
/// Implementations must not fall back to plain-text storage: if the platform cannot protect the data, they throw.
/// </summary>
public interface IMavSecureStorage
{
    /// <summary>Returns the stored secret, or <c>null</c> if the key does not exist.</summary>
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);

    /// <summary>Removes the key. Returns <c>true</c> if the key existed.</summary>
    bool Remove(string key);

    void RemoveAll();
}
