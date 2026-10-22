namespace Mav.Essentials;

/// <summary>Shared rules for preference value types and container names.</summary>
public static class MavPreferenceValues
{
    private const string DefaultContainer = "preferences";
    private const int MaxNameLength = 64;

    public static bool IsSupported<T>() =>
        typeof(T) == typeof(string) ||
        typeof(T) == typeof(bool) ||
        typeof(T) == typeof(int) ||
        typeof(T) == typeof(long) ||
        typeof(T) == typeof(float) ||
        typeof(T) == typeof(double);

    public static void ThrowIfUnsupported<T>()
    {
        if (!IsSupported<T>())
        {
            throw new NotSupportedException(
                $"Preferences do not support {typeof(T)}. Use string, bool, int, long, float or double.");
        }
    }

    /// <summary>Maps a shared name to a container name. Null means the default container.</summary>
    public static string ResolveContainer(string? sharedName)
    {
        if (sharedName is null)
        {
            return DefaultContainer;
        }

        if (!IsValidName(sharedName))
        {
            throw new ArgumentException(
                "Shared name must be 1-64 characters: letters, digits, '.', '_' or '-', starting with a letter or digit.",
                nameof(sharedName));
        }

        return sharedName;
    }

    private static bool IsValidName(string name)
    {
        if (name.Length is < 1 or > MaxNameLength || !char.IsLetterOrDigit(name[0]))
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }
}
