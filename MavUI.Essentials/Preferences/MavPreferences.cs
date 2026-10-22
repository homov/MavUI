namespace Mav.Essentials;

/// <summary>
/// Entry point for preferences. <see cref="Default"/> is a JSON file in the user's local data folder
/// unless a platform backend has been registered with <see cref="SetDefault"/>.
/// </summary>
public static class MavPreferences
{
    private static readonly object Gate = new();
    private static IMavPreferences? _default;

    public static IMavPreferences Default
    {
        get
        {
            lock (Gate)
            {
                return _default ??= new JsonFilePreferences(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MavUI.Essentials"));
            }
        }
    }

    /// <summary>Registers a platform backend. Pass <c>null</c> to return to the JSON default.</summary>
    public static void SetDefault(IMavPreferences? preferences)
    {
        lock (Gate)
        {
            _default = preferences;
        }
    }
}
