using System.Globalization;
using Mav;

namespace Mav.ControlsGalleryMarkup.Localization;

/// <summary>
/// Russian strings for the markup gallery. English is never looked up here: every
/// <c>MavLocalization.Get(key, english)</c> call carries the English text as its fallback,
/// so a missing or English culture shows English.
/// </summary>
public sealed class GalleryStrings : IMavLocalizationProvider
{
    public static readonly GalleryStrings Instance = new();

    private static readonly Dictionary<string, string> Russian = BuildRussian();

    public string? GetString(string key, CultureInfo? culture = null)
    {
        var language = (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName;
        if (language != "ru")
            return null;

        return Russian.TryGetValue(key, out var value) ? value : null;
    }

    private static Dictionary<string, string> BuildRussian()
    {
        var table = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in Parts)
        {
            foreach (var (key, value) in part)
                table[key] = value;
        }

        return table;
    }

    private static IEnumerable<(string Key, string Value)[]> Parts =>
    [
        RuShell.Items,
        RuOverview.Items,
        RuActions.Items,
        RuInputs.Items,
        RuSelection.Items,
        RuPickers.Items,
        RuNavigationGroup.Items,
        RuLayout.Items,
        RuCollections.Items,
        RuFeedback.Items,
        RuOverlays.Items,
        RuTheming.Items,
    ];
}
