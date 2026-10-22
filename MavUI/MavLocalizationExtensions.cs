using Avalonia.Controls;
using Mav.Controls;

namespace Mav;

/// <summary>
/// Convenience helpers for application strings. Attach once and the text updates
/// automatically when <see cref="MavLocalization.SetCulture"/> or the provider changes.
/// </summary>
public static class MavLocalizationExtensions
{
    /// <summary>
    /// Localizes a <see cref="TextBlock"/>, <see cref="MavButton"/> or <see cref="Window"/>.
    /// The control keeps its current text until the next localization change when
    /// <paramref name="key"/> has no value and <paramref name="fallback"/> is null.
    /// </summary>
    public static T Localize<T>(this T control, string key, string? fallback = null)
        where T : Control
    {
        ArgumentNullException.ThrowIfNull(control);
        MavLocalization.Observe(control, c => ApplyText(c, MavLocalization.Get(key, fallback ?? key)));
        return control;
    }

    /// <summary>Localizes the floating label of a text field.</summary>
    public static MavTextField LocalizeLabel(
        this MavTextField field,
        string key,
        string? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        MavLocalization.Observe(field, f => f.Label = MavLocalization.Get(key, fallback ?? key));
        return field;
    }

    /// <summary>Localizes the placeholder of a text field.</summary>
    public static MavTextField LocalizePlaceholder(
        this MavTextField field,
        string key,
        string? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        MavLocalization.Observe(field, f => f.Placeholder = MavLocalization.Get(key, fallback ?? key));
        return field;
    }

    private static void ApplyText(Control control, string text)
    {
        switch (control)
        {
            case TextBlock textBlock:
                textBlock.Text = text;
                break;
            case MavButton button:
                button.Text = text;
                break;
            case Window window:
                window.Title = text;
                break;
            default:
                throw new ArgumentException(
                    "Localize supports TextBlock, MavButton and Window. " +
                    "Use MavLocalization.Observe for other controls.",
                    nameof(control));
        }
    }
}
