using Avalonia.Controls;
using Md3.Avalonia.Controls;

namespace Md3.Avalonia;

/// <summary>
/// Convenience helpers for application strings. Attach once and the text updates
/// automatically when <see cref="MdLocalization.SetCulture"/> or the provider changes.
/// </summary>
public static class MdLocalizationExtensions
{
    /// <summary>
    /// Localizes a <see cref="TextBlock"/>, <see cref="MdButton"/> or <see cref="Window"/>.
    /// The control keeps its current text until the next localization change when
    /// <paramref name="key"/> has no value and <paramref name="fallback"/> is null.
    /// </summary>
    public static T Localize<T>(this T control, string key, string? fallback = null)
        where T : Control
    {
        ArgumentNullException.ThrowIfNull(control);
        MdLocalization.Observe(control, c => ApplyText(c, MdLocalization.Get(key, fallback ?? key)));
        return control;
    }

    /// <summary>Localizes the floating label of a text field.</summary>
    public static MdTextField LocalizeLabel(
        this MdTextField field,
        string key,
        string? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        MdLocalization.Observe(field, f => f.Label = MdLocalization.Get(key, fallback ?? key));
        return field;
    }

    /// <summary>Localizes the placeholder of a text field.</summary>
    public static MdTextField LocalizePlaceholder(
        this MdTextField field,
        string key,
        string? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(field);
        MdLocalization.Observe(field, f => f.Placeholder = MdLocalization.Get(key, fallback ?? key));
        return field;
    }

    private static void ApplyText(Control control, string text)
    {
        switch (control)
        {
            case TextBlock textBlock:
                textBlock.Text = text;
                break;
            case MdButton button:
                button.Text = text;
                break;
            case Window window:
                window.Title = text;
                break;
            default:
                throw new ArgumentException(
                    "Localize supports TextBlock, MdButton and Window. " +
                    "Use MdLocalization.Observe for other controls.",
                    nameof(control));
        }
    }
}
