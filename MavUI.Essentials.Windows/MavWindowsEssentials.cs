namespace Mav.Essentials.Windows;

public static class MavWindowsEssentials
{
    /// <summary>
    /// Registers DPAPI-protected SecureStorage as the default for <see cref="MavSecureStorage"/>.
    /// Preferences keep their default JSON backend. Call once at startup.
    /// </summary>
    public static void Initialize()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MavUI.Essentials");
        MavSecureStorage.SetDefault(new DpapiSecureStorage(directory));
    }
}
