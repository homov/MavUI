namespace Mav.Essentials;

/// <summary>
/// Entry point for secure storage. Until a platform backend is registered, every call fails with
/// <see cref="PlatformNotSupportedException"/>; secrets are never written to an unprotected location.
/// </summary>
public static class MavSecureStorage
{
    private static readonly object Gate = new();
    private static IMavSecureStorage? _default;

    public static IMavSecureStorage Default
    {
        get
        {
            lock (Gate)
            {
                return _default ?? UnsupportedSecureStorage.Instance;
            }
        }
    }

    /// <summary>Registers a platform backend. Pass <c>null</c> to return to the unsupported default.</summary>
    public static void SetDefault(IMavSecureStorage? storage)
    {
        lock (Gate)
        {
            _default = storage;
        }
    }
}

/// <summary>Default backend: refuses every operation.</summary>
public sealed class UnsupportedSecureStorage : IMavSecureStorage
{
    public static UnsupportedSecureStorage Instance { get; } = new();

    private static PlatformNotSupportedException NotSupported() =>
        new("No secure storage backend is registered for this platform. Call MavSecureStorage.SetDefault with a platform backend.");

    public Task<string?> GetAsync(string key) => Task.FromException<string?>(NotSupported());

    public Task SetAsync(string key, string value) => Task.FromException(NotSupported());

    public bool Remove(string key) => throw NotSupported();

    public void RemoveAll() => throw NotSupported();
}
