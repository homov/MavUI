namespace Mav.Essentials.Linux;

public static class MavLinuxEssentials
{
    /// <summary>
    /// Registers libsecret-backed SecureStorage as the default for <see cref="MavSecureStorage"/>.
    /// Preferences keep their default JSON backend. Call once at startup.
    /// </summary>
    public static void Initialize()
    {
        MavSecureStorage.SetDefault(new LibsecretSecureStorage());
    }
}
