using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Mav;
using Mav.ControlsGallery.Localization;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// The demo follows the device language on launch, offers a manual override, and falls back to
/// English for unsupported languages. English is already the AXAML fallback, not another table.
/// </summary>
public class GalleryLanguageTests
{
    private sealed class GalleryState : IDisposable
    {
        private readonly IMavLocalizationProvider _provider = MavLocalization.Current;
        private readonly CultureInfo _culture = MavLocalization.Culture;
        private readonly CultureInfo _system = GalleryLanguage.SystemCulture;
        private readonly GalleryLanguage? _selected = GalleryLanguage.Selected;
        private readonly MavMotionScheme _motion = MavTheme.Motion;

        public GalleryState()
        {
            GalleryLanguage.SystemCulture = new CultureInfo("en-US");
            MavLocalization.Current = GalleryStrings.Instance;
            GalleryLanguage.Select(null);
        }

        public void Dispose()
        {
            GalleryLanguage.SystemCulture = _system;
            GalleryLanguage.Select(_selected);
            MavLocalization.Current = _provider;
            MavLocalization.SetCulture(_culture);
            MavTheme.Configure(motion: _motion);
        }
    }

    [AvaloniaFact]
    public void Without_a_choice_the_language_is_the_one_of_the_device()
    {
        using var _ = new GalleryState();
        GalleryLanguage.SystemCulture = new CultureInfo("uk-UA");
        GalleryLanguage.Select(null);

        Assert.Same(GalleryLanguage.Ukrainian, GalleryLanguage.Resolve());
        Assert.Equal("uk-UA", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("Огляд", MavLocalization.Get("Nav.Overview.Label", "Overview"));
        Assert.Same(GalleryLanguage.German, GalleryLanguage.FromCulture(new CultureInfo("de-DE")));
        Assert.Same(GalleryLanguage.French, GalleryLanguage.FromCulture(new CultureInfo("fr-CA")));
        Assert.Same(GalleryLanguage.Italian, GalleryLanguage.FromCulture(new CultureInfo("it-IT")));
        Assert.Same(GalleryLanguage.Spanish, GalleryLanguage.FromCulture(new CultureInfo("es-MX")));

        // Keep a regional format while the UI uses the language's table.
        GalleryLanguage.SystemCulture = new CultureInfo("fr-CA");
        GalleryLanguage.Select(null);
        Assert.Equal("fr-CA", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("Vue d’ensemble", MavLocalization.Get("Nav.Overview.Label", "Overview"));
    }

    [AvaloniaFact]
    public void Manual_selection_changes_all_ui_text_and_system_restores_the_device_language()
    {
        using var _ = new GalleryState();
        GalleryLanguage.SystemCulture = new CultureInfo("de-AT");
        GalleryLanguage.Select(null);
        Assert.Equal("Übersicht", MavLocalization.Get("Nav.Overview.Label", "Overview"));

        GalleryLanguage.Select(GalleryLanguage.Italian);
        Assert.Same(GalleryLanguage.Italian, GalleryLanguage.Selected);
        Assert.Equal("Panoramica", MavLocalization.Get("Nav.Overview.Label", "Overview"));
        Assert.Equal("Annulla", MavLocalization.Cancel); // dialogs from the library, as used in the demo

        GalleryLanguage.Select(GalleryLanguage.Spanish);
        Assert.Equal("Resumen", MavLocalization.Get("Nav.Overview.Label", "Overview"));
        Assert.Equal("Cancelar", MavLocalization.Cancel);

        GalleryLanguage.Select(null);
        Assert.Null(GalleryLanguage.Selected);
        Assert.Same(GalleryLanguage.German, GalleryLanguage.Resolve());
        Assert.Equal("de-AT", CultureInfo.CurrentUICulture.Name);
        Assert.Equal("Übersicht", MavLocalization.Get("Nav.Overview.Label", "Overview"));
    }

    [AvaloniaFact]
    public void Russian_and_other_unsupported_locales_use_english_ui_but_keep_their_formatting()
    {
        using var _ = new GalleryState();
        foreach (var tag in new[] { "ru-RU", "pl-PL", "ja-JP" })
        {
            GalleryLanguage.SystemCulture = new CultureInfo(tag);
            GalleryLanguage.Select(null);
            Assert.Same(GalleryLanguage.English, GalleryLanguage.Resolve());
            Assert.Equal(tag, CultureInfo.CurrentUICulture.Name);
            Assert.Equal("Overview", MavLocalization.Get("Nav.Overview.Label", "Overview"));
            Assert.Equal("Cancel", MavLocalization.Cancel);
        }

        GalleryLanguage.Select(GalleryLanguage.English);
        Assert.Equal("Overview", MavLocalization.Get("Nav.Overview.Label", "Overview"));
        Assert.Null(GalleryStrings.Instance.GetString("Nav.Overview.Label", GalleryLanguage.English.Culture));
        Assert.Equal("Missing key", MavLocalization.Get("No.Such.Key", "Missing key"));
    }

    [AvaloniaFact]
    public void Every_translation_table_has_the_same_keys_and_matching_format_placeholders()
    {
        var translations = GalleryLanguage.All.Where(language => language != GalleryLanguage.English).ToArray();
        Assert.Equal(5, translations.Length);
        var reference = GalleryLanguage.Ukrainian.Strings!.ToDictionary(entry => entry.Key, entry => entry.Value);
        Assert.True(reference.Count > 400, "Translation tables must cover the whole demo, not just the navigation.");

        var failures = new List<string>();
        foreach (var language in translations)
        {
            var entries = language.Strings;
            if (entries is null)
            {
                failures.Add($"{language.Code}: no translations");
                continue;
            }

            var keys = entries.Select(entry => entry.Key).ToArray();
            foreach (var missing in reference.Keys.Except(keys))
                failures.Add($"{language.Code}: missing {missing}");
            foreach (var extra in keys.Except(reference.Keys))
                failures.Add($"{language.Code}: unexpected {extra}");
            if (keys.Length != keys.Distinct().Count())
                failures.Add($"{language.Code}: duplicate keys");

            foreach (var (key, value) in entries)
            {
                if (string.IsNullOrWhiteSpace(value))
                    failures.Add($"{language.Code}: empty {key}");

                try
                {
                    // Parse before we try formatting. A missing brace would otherwise crash a page.
                    CompositeFormat.Parse(value);
                    if (reference.TryGetValue(key, out var expected) &&
                        !Placeholders(value).SequenceEqual(Placeholders(expected)))
                        failures.Add($"{language.Code}: incorrect placeholders in {key}");
                }
                catch (FormatException error)
                {
                    failures.Add($"{language.Code}: invalid format in {key}: {error.Message}");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(30)));
    }

    private static int[] Placeholders(string value) =>
        Regex.Matches(value, @"(?<!\{)\{(\d+)(?:[:,}])")
            .Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture))
            .OrderBy(index => index)
            .ToArray();

    [AvaloniaFact]
    public void Browser_and_mobile_start_with_reduced_motion_desktop_stays_expressive()
    {
        Assert.Equal(MavMotionScheme.Expressive, GalleryStartup.MotionFor(isDesktop: true));
        Assert.Equal(MavMotionScheme.Reduced, GalleryStartup.MotionFor(isDesktop: false));
        Assert.Equal(GalleryStartup.MotionFor(GalleryStartup.IsDesktop), GalleryStartup.DefaultMotion());
    }

    [AvaloniaFact]
    public void Startup_uses_the_current_system_culture_not_a_previous_manual_choice()
    {
        using var _ = new GalleryState();
        GalleryLanguage.Select(GalleryLanguage.Spanish);
        MavLocalization.SetCulture(new CultureInfo("fr-FR"));
        GalleryStartup.Apply();

        Assert.Null(GalleryLanguage.Selected);
        Assert.Same(GalleryLanguage.French, GalleryLanguage.Resolve());
        Assert.Equal("fr-FR", GalleryLanguage.SystemCulture.Name);
        Assert.Equal("Vue d’ensemble", MavLocalization.Get("Nav.Overview.Label", "Overview"));
        Assert.Same(GalleryStrings.Instance, MavLocalization.Current);
        Assert.Equal(GalleryStartup.DefaultMotion(), MavTheme.Motion);
    }
}
