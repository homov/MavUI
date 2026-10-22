using Android.Content;

namespace Mav.Essentials.Droid;

public static class MavAndroidEssentials
{
    /// <summary>
    /// Registers the Android backends as the defaults for <see cref="MavPreferences"/> and <see cref="MavSecureStorage"/>.
    /// Call once, early (for example in Application.OnCreate), before any storage is used.
    /// </summary>
    public static void Initialize(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var app = context.ApplicationContext ?? context;
        MavPreferences.SetDefault(new AndroidSharedPreferences(app));
        MavSecureStorage.SetDefault(new AndroidKeystoreSecureStorage(app));
    }
}
