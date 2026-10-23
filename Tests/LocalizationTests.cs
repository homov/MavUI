using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class LocalizationTests
{
    [Fact]
    public void Default_provider_returns_builtins_and_null_for_unknown_keys()
    {
        var provider = new MavLocalization.DefaultProvider();

        Assert.Equal("OK", provider.GetString(MavLocalization.Keys.Ok));
        Assert.Equal("Cancel", provider.GetString(MavLocalization.Keys.Cancel));
        Assert.Null(provider.GetString("NoSuchKey"));
    }

    [Fact]
    public void Dictionary_provider_falls_back_to_builtins()
    {
        var provider = new MavLocalization.DictionaryProvider(
            new Dictionary<string, string>
            {
                [MavLocalization.Keys.Ok] = "Хорошо",
            });

        Assert.Equal("Хорошо", provider.GetString(MavLocalization.Keys.Ok));
        Assert.Equal("Cancel", provider.GetString(MavLocalization.Keys.Cancel));
        Assert.Null(provider.GetString("NoSuchKey"));
    }

    [Fact]
    public void Chained_provider_returns_first_non_empty_value()
    {
        var provider = new MavLocalization.ChainedProvider(
            new MavLocalization.DelegateProvider((key, _) =>
                key == "First" ? "first" : null),
            new MavLocalization.DelegateProvider((key, _) =>
                key == "Greeting" ? "Hello" : null));

        Assert.Equal("Hello", provider.GetString("Greeting"));
        Assert.Equal("first", provider.GetString("First"));
        Assert.Null(provider.GetString("Missing"));
    }

    [Fact]
    public void Get_uses_fallback_and_key_when_provider_has_no_value()
    {
        var previous = MavLocalization.Current;
        try
        {
            MavLocalization.Current = new MavLocalization.DelegateProvider(
                (key, _) => key == "Greeting" ? "Hello" : null);

            Assert.Equal("Hello", MavLocalization.Get("Greeting", "Hi"));
            Assert.Equal("Fallback", MavLocalization.Get("Missing", "Fallback"));
            Assert.Equal("Missing", MavLocalization.Get("Missing"));
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }

    [Fact]
    public void Format_uses_the_current_formatting_culture()
    {
        var previousProvider = MavLocalization.Current;
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Привет, {0}!",
                });
            MavLocalization.SetCulture(new CultureInfo("ru-RU"));

            Assert.Equal("Привет, Анна!", MavLocalization.Format("Greeting", "Hello, {0}!", "Анна"));
        }
        finally
        {
            MavLocalization.Current = previousProvider;
            MavLocalization.SetCulture(previousCulture);
        }
    }

    [Fact]
    public void SetCulture_updates_ui_culture_and_raises_changed()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        try
        {
            MavLocalization.Changed += handler;

            MavLocalization.SetCulture(new CultureInfo("ru-RU"));

            Assert.Equal("ru-RU", CultureInfo.CurrentUICulture.Name);
            Assert.Equal("ru-RU", CultureInfo.CurrentCulture.Name);
            Assert.Equal(1, raised);
        }
        finally
        {
            MavLocalization.Changed -= handler;
            MavLocalization.SetCulture(previousCulture);
        }
    }

    [AvaloniaFact]
    public void Localize_updates_text_after_provider_change_without_event_subscription()
    {
        var previous = MavLocalization.Current;
        try
        {
            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Hello",
                });

            var text = new TextBlock().Localize("Greeting", "Greeting");
            Assert.Equal("Hello", text.Text);

            var window = new Window { Width = 300, Height = 200, Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Привет",
                });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Привет", text.Text);
            window.Close();
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void SetCulture_refreshes_localized_text_without_event_subscription()
    {
        var previousProvider = MavLocalization.Current;
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            MavLocalization.Current = new MavLocalization.DelegateProvider(
                (key, culture) => key == "Greeting"
                    ? (culture?.TwoLetterISOLanguageName == "ru" ? "Привет" : "Hello")
                    : null);

            var text = new TextBlock().Localize("Greeting", "Hello");
            var window = new Window { Width = 300, Height = 200, Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Hello", text.Text);

            MavLocalization.SetCulture(new CultureInfo("ru-RU"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Привет", text.Text);
            window.Close();
        }
        finally
        {
            MavLocalization.Current = previousProvider;
            MavLocalization.SetCulture(previousCulture);
        }
    }

    [AvaloniaFact]
    public void Attached_localization_key_updates_text_and_fallback()
    {
        var previous = MavLocalization.Current;
        try
        {
            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Hello",
                });

            var text = new TextBlock();
            MavLocalization.SetKey(text, "Greeting");
            MavLocalization.SetFallback(text, "Hi");
            Assert.Equal("Hello", text.Text);

            var missing = new TextBlock();
            MavLocalization.SetKey(missing, "Missing");
            MavLocalization.SetFallback(missing, "Fallback");
            Assert.Equal("Fallback", missing.Text);

            var window = new Window { Width = 300, Height = 200, Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Привет",
                });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Привет", text.Text);
            window.Close();
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void Attached_localization_target_updates_a_text_field_label()
    {
        var previous = MavLocalization.Current;
        try
        {
            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Name"] = "Имя",
                });

            var field = new MavTextField();
            MavLocalization.SetKey(field, "Name");
            MavLocalization.SetTarget(field, MavLocalizationTarget.Label);

            Assert.Equal("Имя", field.Label);
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void Loc_markup_extension_updates_target_property_without_event_subscription()
    {
        var previous = MavLocalization.Current;
        try
        {
            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Hello",
                });

            var text = new TextBlock();
            var value = new LocExtension("Greeting")
                .ProvideValue(new TestValueTarget(text, TextBlock.TextProperty));

            Assert.Equal("Hello", value);
            Assert.Equal("Hello", text.Text);

            var window = new Window { Width = 300, Height = 200, Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Привет",
                });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Привет", text.Text);
            window.Close();
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }

    private sealed class TestValueTarget(object target, object property)
        : IServiceProvider, IProvideValueTarget
    {
        public object TargetObject { get; } = target;
        public object TargetProperty { get; } = property;
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IProvideValueTarget) ? this : null;
    }

    [AvaloniaFact]
    public void LocalizeLabel_and_LocalizePlaceholder_update_fields()
    {
        var previous = MavLocalization.Current;
        try
        {
            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Name"] = "Имя",
                    ["Hint"] = "Введите имя",
                });

            var field = new MavTextField()
                .LocalizeLabel("Name")
                .LocalizePlaceholder("Hint");

            Assert.Equal("Имя", field.Label);
            Assert.Equal("Введите имя", field.Placeholder);
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void MavDialog_uses_current_localization_for_default_confirm_text()
    {
        var previous = MavLocalization.Current;
        try
        {
            MavLocalization.Current = new MavLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    [MavLocalization.Keys.Ok] = "Хорошо",
                });

            var anchor = new MavButton("Anchor");
            var page = new MavContentPage("Root") { Child = anchor };
            var navigation = new MavNavigationPage(page);
            var window = new Window { Width = 400, Height = 600, Content = navigation };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var dialog = new MavDialog
            {
                Title = "Delete project?",
                Message = "This cannot be undone.",
            };
            dialog.Show(anchor);
            Dispatcher.UIThread.RunJobs();

            var buttons = window.GetVisualDescendants().OfType<MavButton>().ToList();
            Assert.Contains(buttons, button => button.Text == "Хорошо");

            dialog.Close();
            window.Close();
        }
        finally
        {
            MavLocalization.Current = previous;
        }
    }
}
