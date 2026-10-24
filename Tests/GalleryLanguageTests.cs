using System.Globalization;
using System.Linq;
using Avalonia.Headless.XUnit;
using Mav;
using Mav.ControlsGallery.Shared;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// The galleries pick the language of the machine when nobody chose one, and let the user override
/// it. English is not a table: every key carries its English text as a fallback, so the provider
/// returns nothing for it.
/// </summary>
public class GalleryLanguageTests
{
    private static void Reset()
    {
        GalleryLanguage.Selected = null;
        GalleryLanguage.SystemCulture = new CultureInfo("en-US");
        MavLocalization.Current = GalleryStrings.Instance;
        MavLocalization.SetCulture(new CultureInfo("en-US"));
    }

    [AvaloniaFact]
    public void Without_a_choice_the_language_is_the_one_of_the_culture()
    {
        var previous = MavLocalization.Current;
        try
        {
            Reset();
            GalleryLanguage.SystemCulture = new CultureInfo("uk-UA");

            Assert.Same(GalleryLanguage.Ukrainian, GalleryLanguage.Resolve());
            Assert.Same(GalleryLanguage.German, GalleryLanguage.Resolve(new CultureInfo("de-DE")));
            Assert.Same(GalleryLanguage.French, GalleryLanguage.Resolve(new CultureInfo("fr-CA")));
            Assert.Same(GalleryLanguage.Italian, GalleryLanguage.Resolve(new CultureInfo("it-IT")));
            Assert.Same(GalleryLanguage.Spanish, GalleryLanguage.Resolve(new CultureInfo("es-MX")));

            // A language the gallery has no table for falls back to English, not to a wrong one.
            Assert.Same(GalleryLanguage.English, GalleryLanguage.Resolve(new CultureInfo("pl-PL")));
            Assert.Same(GalleryLanguage.English, GalleryLanguage.Resolve(new CultureInfo("ja-JP")));
        }
        finally
        {
            MavLocalization.Current = previous;
            Reset();
        }
    }

    [AvaloniaFact]
    public void A_picked_language_wins_over_the_culture()
    {
        var previous = MavLocalization.Current;
        try
        {
            Reset();
            GalleryLanguage.Selected = GalleryLanguage.Italian;

            Assert.Same(GalleryLanguage.Italian, GalleryLanguage.Resolve());
            Assert.Same(GalleryLanguage.Italian, GalleryLanguage.Resolve(new CultureInfo("de-DE")));

            // "System" gives the culture back its say.
            GalleryLanguage.Selected = null;
            Assert.Same(GalleryLanguage.German, GalleryLanguage.Resolve(new CultureInfo("de-AT")));
        }
        finally
        {
            MavLocalization.Current = previous;
            Reset();
        }
    }

    [AvaloniaFact]
    public void The_provider_answers_in_the_resolved_language()
    {
        var previous = MavLocalization.Current;
        try
        {
            Reset();
            GalleryLanguage.Selected = GalleryLanguage.Ukrainian;

            Assert.Equal("Огляд", MavLocalization.Get("Nav.Overview.Label", "Overview"));
            Assert.Equal("Скасувати", MavLocalization.Get(MavLocalization.Keys.Cancel, "Cancel"));

            // English has no table: the fallback the AXAML was written with shows through.
            GalleryLanguage.Selected = GalleryLanguage.English;
            Assert.Equal("Overview", MavLocalization.Get("Nav.Overview.Label", "Overview"));
            Assert.Equal("Cancel", MavLocalization.Get(MavLocalization.Keys.Cancel, "Cancel"));

            // A key no language knows keeps its fallback too.
            GalleryLanguage.Selected = GalleryLanguage.Ukrainian;
            Assert.Equal("Not translated", MavLocalization.Get("No.Such.Key", "Not translated"));
        }
        finally
        {
            MavLocalization.Current = previous;
            Reset();
        }
    }

    [AvaloniaFact]
    public void Every_language_covers_the_same_keys_and_no_key_is_empty()
    {
        var tables = GalleryLanguage.All
            .Where(language => language.Strings is not null)
            .Select(language => (language.Code, Keys: language.Strings!.Select(entry => entry.Key).ToArray()))
            .ToArray();

        Assert.NotEmpty(tables);

        var reference = tables[0];
        var failures = new List<string>();
        foreach (var (code, keys) in tables)
        {
            var missing = reference.Keys.Except(keys).ToArray();
            if (missing.Length > 0)
                failures.Add($"{code} is missing {missing.Length} key(s): {string.Join(", ", missing.Take(5))}");

            var extra = keys.Except(reference.Keys).ToArray();
            if (extra.Length > 0)
                failures.Add($"{code} has {extra.Length} unknown key(s): {string.Join(", ", extra.Take(5))}");

            if (keys.Length != keys.Distinct().Count())
                failures.Add($"{code} repeats keys");

            var empty = GalleryLanguage.All.Single(l => l.Code == code).Strings!
                .Where(entry => string.IsNullOrWhiteSpace(entry.Value)).ToArray();
            if (empty.Length > 0)
                failures.Add($"{code} has {empty.Length} empty value(s): {string.Join(", ", empty.Take(5).Select(e => e.Key))}");
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [AvaloniaFact]
    public void A_browser_and_a_phone_start_with_the_reduced_motion_profile()
    {
        // The desktop keeps the expressive profile; every other host starts reduced, because a
        // browser tab and a phone animate on a software or battery-powered path.
        Assert.Equal(MavMotionScheme.Expressive, GalleryStartup.MotionFor(isDesktop: true));
        Assert.Equal(MavMotionScheme.Reduced, GalleryStartup.MotionFor(isDesktop: false));
        Assert.Equal(GalleryStartup.MotionFor(GalleryStartup.IsDesktop), GalleryStartup.DefaultMotion());
    }

    [AvaloniaFact]
    public void Startup_keeps_the_culture_the_machine_was_set_to()
    {
        var previous = MavLocalization.Current;
        var previousCulture = MavLocalization.Culture;
        var previousSelection = GalleryLanguage.Selected;
        var previousMotion = MavTheme.Motion;
        try
        {
            MavLocalization.SetCulture(new CultureInfo("fr-FR"));
            GalleryStartup.Apply();

            // Nothing was picked, so the machine's French is what the gallery speaks.
            Assert.Same(GalleryLanguage.French, GalleryLanguage.Resolve());
            Assert.Equal("fr", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            Assert.Equal("fr-FR", GalleryLanguage.SystemCulture.Name);
            Assert.Same(GalleryStrings.Instance, MavLocalization.Current);

            // Picking a language by hand and going back to "System" returns to French.
            GalleryLanguage.Selected = GalleryLanguage.Spanish;
            Assert.Same(GalleryLanguage.Spanish, GalleryLanguage.Resolve());
            GalleryLanguage.Selected = null;
            Assert.Same(GalleryLanguage.French, GalleryLanguage.Resolve());
        }
        finally
        {
            GalleryLanguage.Selected = previousSelection;
            MavLocalization.Current = previous;
            MavLocalization.SetCulture(previousCulture);
            MavTheme.Configure(motion: previousMotion);
        }
    }
}
