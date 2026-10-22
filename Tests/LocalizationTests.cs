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
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Xunit;

namespace Md3.Avalonia.Tests;

public class LocalizationTests
{
    [Fact]
    public void Default_provider_returns_builtins_and_null_for_unknown_keys()
    {
        var provider = new MdLocalization.DefaultProvider();

        Assert.Equal("OK", provider.GetString(MdLocalization.Keys.Ok));
        Assert.Equal("Cancel", provider.GetString(MdLocalization.Keys.Cancel));
        Assert.Null(provider.GetString("NoSuchKey"));
    }

    [Fact]
    public void Dictionary_provider_falls_back_to_builtins()
    {
        var provider = new MdLocalization.DictionaryProvider(
            new Dictionary<string, string>
            {
                [MdLocalization.Keys.Ok] = "Хорошо",
            });

        Assert.Equal("Хорошо", provider.GetString(MdLocalization.Keys.Ok));
        Assert.Equal("Cancel", provider.GetString(MdLocalization.Keys.Cancel));
        Assert.Null(provider.GetString("NoSuchKey"));
    }

    [Fact]
    public void Chained_provider_returns_first_non_empty_value()
    {
        var provider = new MdLocalization.ChainedProvider(
            new MdLocalization.DelegateProvider((key, _) =>
                key == "First" ? "first" : null),
            new MdLocalization.DelegateProvider((key, _) =>
                key == "Greeting" ? "Hello" : null));

        Assert.Equal("Hello", provider.GetString("Greeting"));
        Assert.Equal("first", provider.GetString("First"));
        Assert.Null(provider.GetString("Missing"));
    }

    [Fact]
    public void Get_uses_fallback_and_key_when_provider_has_no_value()
    {
        var previous = MdLocalization.Current;
        try
        {
            MdLocalization.Current = new MdLocalization.DelegateProvider(
                (key, _) => key == "Greeting" ? "Hello" : null);

            Assert.Equal("Hello", MdLocalization.Get("Greeting", "Hi"));
            Assert.Equal("Fallback", MdLocalization.Get("Missing", "Fallback"));
            Assert.Equal("Missing", MdLocalization.Get("Missing"));
        }
        finally
        {
            MdLocalization.Current = previous;
        }
    }

    [Fact]
    public void Format_uses_the_current_formatting_culture()
    {
        var previousProvider = MdLocalization.Current;
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            MdLocalization.Current = new MdLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Привет, {0}!",
                });
            MdLocalization.SetCulture(new CultureInfo("ru-RU"));

            Assert.Equal("Привет, Анна!", MdLocalization.Format("Greeting", "Hello, {0}!", "Анна"));
        }
        finally
        {
            MdLocalization.Current = previousProvider;
            MdLocalization.SetCulture(previousCulture);
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
            MdLocalization.Changed += handler;

            MdLocalization.SetCulture(new CultureInfo("ru-RU"));

            Assert.Equal("ru-RU", CultureInfo.CurrentUICulture.Name);
            Assert.Equal("ru-RU", CultureInfo.CurrentCulture.Name);
            Assert.Equal(1, raised);
        }
        finally
        {
            MdLocalization.Changed -= handler;
            MdLocalization.SetCulture(previousCulture);
        }
    }

    [AvaloniaFact]
    public void Localize_updates_text_after_provider_change_without_event_subscription()
    {
        var previous = MdLocalization.Current;
        try
        {
            MdLocalization.Current = new MdLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Hello",
                });

            var text = new TextBlock().Localize("Greeting", "Greeting");
            Assert.Equal("Hello", text.Text);

            var window = new Window { Width = 300, Height = 200, Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            MdLocalization.Current = new MdLocalization.DictionaryProvider(
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
            MdLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void SetCulture_refreshes_localized_text_without_event_subscription()
    {
        var previousProvider = MdLocalization.Current;
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            MdLocalization.Current = new MdLocalization.DelegateProvider(
                (key, culture) => key == "Greeting"
                    ? (culture?.TwoLetterISOLanguageName == "ru" ? "Привет" : "Hello")
                    : null);

            var text = new TextBlock().Localize("Greeting", "Hello");
            var window = new Window { Width = 300, Height = 200, Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Hello", text.Text);

            MdLocalization.SetCulture(new CultureInfo("ru-RU"));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Привет", text.Text);
            window.Close();
        }
        finally
        {
            MdLocalization.Current = previousProvider;
            MdLocalization.SetCulture(previousCulture);
        }
    }

    [AvaloniaFact]
    public void Attached_localization_key_updates_text_and_fallback()
    {
        var previous = MdLocalization.Current;
        try
        {
            MdLocalization.Current = new MdLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Greeting"] = "Hello",
                });

            var text = new TextBlock();
            MdLocalization.SetKey(text, "Greeting");
            MdLocalization.SetFallback(text, "Hi");
            Assert.Equal("Hello", text.Text);

            var missing = new TextBlock();
            MdLocalization.SetKey(missing, "Missing");
            MdLocalization.SetFallback(missing, "Fallback");
            Assert.Equal("Fallback", missing.Text);

            var window = new Window { Width = 300, Height = 200, Content = text };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            MdLocalization.Current = new MdLocalization.DictionaryProvider(
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
            MdLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void Attached_localization_target_updates_a_text_field_label()
    {
        var previous = MdLocalization.Current;
        try
        {
            MdLocalization.Current = new MdLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Name"] = "Имя",
                });

            var field = new MdTextField();
            MdLocalization.SetKey(field, "Name");
            MdLocalization.SetTarget(field, MdLocalizationTarget.Label);

            Assert.Equal("Имя", field.Label);
        }
        finally
        {
            MdLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void Loc_markup_extension_updates_target_property_without_event_subscription()
    {
        var previous = MdLocalization.Current;
        try
        {
            MdLocalization.Current = new MdLocalization.DictionaryProvider(
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

            MdLocalization.Current = new MdLocalization.DictionaryProvider(
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
            MdLocalization.Current = previous;
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
        var previous = MdLocalization.Current;
        try
        {
            MdLocalization.Current = new MdLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    ["Name"] = "Имя",
                    ["Hint"] = "Введите имя",
                });

            var field = new MdTextField()
                .LocalizeLabel("Name")
                .LocalizePlaceholder("Hint");

            Assert.Equal("Имя", field.Label);
            Assert.Equal("Введите имя", field.Placeholder);
        }
        finally
        {
            MdLocalization.Current = previous;
        }
    }

    [AvaloniaFact]
    public void MdDialog_uses_current_localization_for_default_confirm_text()
    {
        var previous = MdLocalization.Current;
        try
        {
            MdLocalization.Current = new MdLocalization.DictionaryProvider(
                new Dictionary<string, string>
                {
                    [MdLocalization.Keys.Ok] = "Хорошо",
                });

            var anchor = new MdButton("Anchor");
            var page = new MdContentPage("Root") { Content = anchor };
            var navigation = new MdNavigationPage(page);
            var window = new Window { Width = 400, Height = 600, Content = navigation };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var dialog = new MdDialog
            {
                Title = "Delete project?",
                Message = "This cannot be undone.",
            };
            dialog.Show(anchor);
            Dispatcher.UIThread.RunJobs();

            var buttons = window.GetVisualDescendants().OfType<MdButton>().ToList();
            Assert.Contains(buttons, button => button.Text == "Хорошо");

            dialog.Close();
            window.Close();
        }
        finally
        {
            MdLocalization.Current = previous;
        }
    }
}
