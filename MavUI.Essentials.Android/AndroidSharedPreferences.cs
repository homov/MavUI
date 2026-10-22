using System.Globalization;
using Android.Content;

namespace Mav.Essentials.Droid;

/// <summary>Preferences backed by Android SharedPreferences (available since API 1).</summary>
public sealed class AndroidSharedPreferences : IMavPreferences
{
    private const string FilePrefix = "mav.essentials.";

    private readonly Context _context;

    public AndroidSharedPreferences(Context context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public T Get<T>(string key, T defaultValue, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        MavPreferenceValues.ThrowIfUnsupported<T>();

        var prefs = Prefs(sharedName);
        if (!prefs.Contains(key))
        {
            return defaultValue;
        }

        if (typeof(T) == typeof(string))
        {
            var text = prefs.GetString(key, null);
            return text is null ? defaultValue : (T)(object)text;
        }

        if (typeof(T) == typeof(bool))
        {
            return (T)(object)prefs.GetBoolean(key, false);
        }

        if (typeof(T) == typeof(int))
        {
            return (T)(object)prefs.GetInt(key, 0);
        }

        if (typeof(T) == typeof(long))
        {
            return (T)(object)prefs.GetLong(key, 0L);
        }

        if (typeof(T) == typeof(float))
        {
            return (T)(object)prefs.GetFloat(key, 0f);
        }

        // double is stored as an invariant-culture string, because SharedPreferences has no double type.
        var stored = prefs.GetString(key, null);
        return stored is not null && double.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? (T)(object)number
            : defaultValue;
    }

    public void Set<T>(string key, T value, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        MavPreferenceValues.ThrowIfUnsupported<T>();

        var editor = Prefs(sharedName).Edit()
            ?? throw new InvalidOperationException("SharedPreferences editor is unavailable.");

        switch (value)
        {
            case null:
                editor.Remove(key);
                break;
            case string text:
                editor.PutString(key, text);
                break;
            case bool flag:
                editor.PutBoolean(key, flag);
                break;
            case int integer:
                editor.PutInt(key, integer);
                break;
            case long longValue:
                editor.PutLong(key, longValue);
                break;
            case float single:
                editor.PutFloat(key, single);
                break;
            case double number:
                editor.PutString(key, number.ToString("R", CultureInfo.InvariantCulture));
                break;
        }

        editor.Apply();
    }

    public bool ContainsKey(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Prefs(sharedName).Contains(key);
    }

    public void Remove(string key, string? sharedName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var editor = Prefs(sharedName).Edit()
            ?? throw new InvalidOperationException("SharedPreferences editor is unavailable.");
        editor.Remove(key);
        editor.Apply();
    }

    public void Clear(string? sharedName = null)
    {
        var editor = Prefs(sharedName).Edit()
            ?? throw new InvalidOperationException("SharedPreferences editor is unavailable.");
        editor.Clear();
        editor.Apply();
    }

    private global::Android.Content.ISharedPreferences Prefs(string? sharedName) =>
        _context.GetSharedPreferences(FilePrefix + MavPreferenceValues.ResolveContainer(sharedName), FileCreationMode.Private)
        ?? throw new InvalidOperationException("SharedPreferences are unavailable.");
}
