using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Md3.Avalonia;

/// <summary>
/// AXAML markup extension for application strings. Keeps the target property in
/// sync with <see cref="MdLocalization.SetCulture"/> and provider changes without
/// any event handlers in the application:
/// <code>&lt;TextBlock Text="{md:Loc Greeting}" /&gt;</code>
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key) => Key = key;

    public LocExtension(string key, string fallback)
    {
        Key = key;
        Fallback = fallback;
    }

    /// <summary>The localization key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Text used when the provider has no value for <see cref="Key"/>.</summary>
    public string? Fallback { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key;
        if (string.IsNullOrEmpty(key))
            return Fallback ?? string.Empty;

        var fallback = Fallback;
        var text = MdLocalization.Get(key, fallback ?? key);

        // The XAML loader gives us the control and the exact target property.
        // Attaching it here keeps the property live without reflection bindings,
        // which also keeps trimming and AOT builds working.
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target &&
            target.TargetObject is Control control &&
            target.TargetProperty is AvaloniaProperty property)
        {
            MdLocalization.Observe(control, c =>
                c.SetValue(property, MdLocalization.Get(key, fallback ?? key)));
        }

        return text;
    }
}
