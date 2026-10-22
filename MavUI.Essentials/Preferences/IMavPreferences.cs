namespace Mav.Essentials;

/// <summary>
/// Key/value storage for non-secret settings. Values are stored in plain text.
/// Use <see cref="IMavSecureStorage"/> for anything that must stay confidential.
/// </summary>
/// <remarks>
/// Supported value types: <c>string</c>, <c>bool</c>, <c>int</c>, <c>long</c>, <c>float</c>, <c>double</c>.
/// <paramref name="sharedName"/> selects a separate container; <c>null</c> is the app's default container.
/// </remarks>
public interface IMavPreferences
{
    /// <summary>Returns the stored value, or <paramref name="defaultValue"/> when the key is missing or holds another type.</summary>
    T Get<T>(string key, T defaultValue, string? sharedName = null);

    /// <summary>Stores the value. Passing <c>null</c> for a reference type removes the key.</summary>
    void Set<T>(string key, T value, string? sharedName = null);

    bool ContainsKey(string key, string? sharedName = null);

    void Remove(string key, string? sharedName = null);

    void Clear(string? sharedName = null);
}
