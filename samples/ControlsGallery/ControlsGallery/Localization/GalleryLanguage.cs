using System.Globalization;
using Mav;

namespace Mav.ControlsGallery.Localization;

/// <summary>
/// A language the gallery can show. <see cref="English"/> carries no table: the AXAML fallbacks
/// and the library's own defaults already are English, so the provider returns nothing for it and
/// every text falls through to the English it was written with.
/// </summary>
public sealed class GalleryLanguage(
    string code,
    string name,
    string englishName,
    (string Key, string Value)[]? strings)
{
    public static readonly GalleryLanguage English = new("en", "English", "English", null);
    public static readonly GalleryLanguage Ukrainian = new("uk", "Українська", "Ukrainian", UkrainianStrings.Items);
    public static readonly GalleryLanguage German = new("de", "Deutsch", "German", GermanStrings.Items);
    public static readonly GalleryLanguage French = new("fr", "Français", "French", FrenchStrings.Items);
    public static readonly GalleryLanguage Italian = new("it", "Italiano", "Italian", ItalianStrings.Items);
    public static readonly GalleryLanguage Spanish = new("es", "Español", "Spanish", SpanishStrings.Items);

    /// <summary>The languages the picker offers, English first: it is the fallback.</summary>
    public static IReadOnlyList<GalleryLanguage> All { get; } =
        [English, Ukrainian, German, French, Italian, Spanish];

    /// <summary>Two-letter ISO 639-1 code ("uk", "de", …).</summary>
    public string Code { get; } = code;

    /// <summary>The name in the language itself, shown in the picker.</summary>
    public string Name { get; } = name;

    /// <summary>The name in English, shown next to the native one.</summary>
    public string EnglishName { get; } = englishName;

    /// <summary>The table, or <c>null</c> for English.</summary>
    public (string Key, string Value)[]? Strings { get; } = strings;

    /// <summary>
    /// The culture the machine was set to when the app started. The "system" choice in the picker
    /// returns to it, also after the user tried other languages.
    /// </summary>
    public static CultureInfo SystemCulture { get; set; } = CultureInfo.CurrentUICulture;

    /// <summary>The user's choice, or <c>null</c> to follow the startup system culture.</summary>
    public static GalleryLanguage? Selected { get; private set; }

    /// <summary>
    /// Switches the whole gallery to a language, or returns to the system's original culture.
    /// This is deliberately a session preference: reopening the demo starts from the device again.
    /// </summary>
    public static void Select(GalleryLanguage? language)
    {
        if (language is not null && !All.Contains(language))
            throw new ArgumentException("The gallery does not offer that language.", nameof(language));

        Selected = language;
        MavLocalization.SetCulture(language?.Culture ?? SystemCulture);
    }

    /// <summary>
    /// The language to show: the picked one, or the one matching <paramref name="culture"/> —
    /// the culture the app started with when none is given.
    /// </summary>
    public static GalleryLanguage Resolve(CultureInfo? culture = null) => Selected ?? FromCulture(culture ?? SystemCulture);

    /// <summary>
    /// The language for a culture: the user's own when it is one of <see cref="All"/>, English otherwise.
    /// </summary>
    public static GalleryLanguage FromCulture(CultureInfo? culture)
    {
        var language = (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName;
        foreach (var candidate in All)
        {
            if (candidate.Code == language)
                return candidate;
        }

        return English;
    }

    /// <summary>The culture of this language ("uk" → uk).</summary>
    public CultureInfo Culture => new(Code);

    public override string ToString() => Name;
}
