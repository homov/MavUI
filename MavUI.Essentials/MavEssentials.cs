#if ANDROID
using Android.Content;
#endif

namespace Mav.Essentials;

/// <summary>
/// Registers the platform backends for <see cref="MavPreferences"/> and <see cref="MavSecureStorage"/>.
/// Call once, early in startup, before any storage is used. Without a registered backend, SecureStorage
/// fails closed and Preferences keep the JSON file default.
/// </summary>
public static class MavEssentials
{
#if ANDROID
    /// <summary>Registers the Android backends (SharedPreferences and Keystore-protected SecureStorage).</summary>
    public static void Initialize(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var app = context.ApplicationContext ?? context;
        MavPreferences.SetDefault(new Mav.Essentials.Droid.AndroidSharedPreferences(app));
        MavSecureStorage.SetDefault(new Mav.Essentials.Droid.AndroidKeystoreSecureStorage(app));
    }
#else
    /// <summary>Registers the SecureStorage backend for the current desktop platform.</summary>
    public static void Initialize()
    {
#if WINDOWS
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MavUI.Essentials");
        MavSecureStorage.SetDefault(new Mav.Essentials.Windows.DpapiSecureStorage(directory));
#else
        if (OperatingSystem.IsLinux())
            MavSecureStorage.SetDefault(new Mav.Essentials.Linux.LibsecretSecureStorage());
#endif
    }
#endif
}
