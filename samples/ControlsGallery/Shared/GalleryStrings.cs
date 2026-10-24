using System.Globalization;
using Mav;

namespace Mav.ControlsGallery.Shared;

/// <summary>
/// Localization provider for both galleries: it answers with the table of the language the user
/// picked, or of the culture the app started with when nobody picked one. English carries no table
/// on purpose — every key has its English text as a fallback in AXAML or in code, so returning
/// nothing lets that text show through.
/// </summary>
public sealed class GalleryStrings : IMavLocalizationProvider
{
    public static readonly GalleryStrings Instance = new();

    private static readonly Dictionary<string, Dictionary<string, string>> Tables = BuildTables();

    public string? GetString(string key, CultureInfo? culture = null)
    {
        var language = GalleryLanguage.Resolve(culture);
        if (language.Strings is null || !Tables.TryGetValue(language.Code, out var table))
            return null;

        return table.TryGetValue(key, out var value) ? value : null;
    }

    private static Dictionary<string, Dictionary<string, string>> BuildTables()
    {
        var tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var language in GalleryLanguage.All)
        {
            if (language.Strings is null)
                continue;

            var table = new Dictionary<string, string>(language.Strings.Length, StringComparer.Ordinal);
            foreach (var (key, value) in language.Strings)
                table[key] = value;

            tables[language.Code] = table;
        }

        return tables;
    }
}
