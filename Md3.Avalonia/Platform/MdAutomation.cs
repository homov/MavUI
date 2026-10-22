using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace Md3.Avalonia.Platform;

/// <summary>
/// Applies accessibility metadata to Md3 controls in a consistent way.
/// </summary>
/// <remarks>
/// Screen readers announce <see cref="AutomationProperties.NameProperty"/> first, then
/// the help text and the control role. Md3 controls fill these values from their own
/// visible text (button label, field label, list headline, dialog title) so the
/// accessible tree stays in sync with the UI without extra application code.
/// Explicit values set by the application are never overwritten: the helpers only
/// replace a value that is still the one they generated.
/// </remarks>
public static class MdAutomation
{
    private static readonly AttachedProperty<string> GeneratedNameProperty =
        AvaloniaProperty.RegisterAttached<Control, string>(
            "MdGeneratedAutomationName", typeof(MdAutomation), string.Empty);

    private static readonly AttachedProperty<string> GeneratedHelpProperty =
        AvaloniaProperty.RegisterAttached<Control, string>(
            "MdGeneratedAutomationHelp", typeof(MdAutomation), string.Empty);

    /// <summary>Sets the accessible name, or clears a previously generated one.</summary>
    public static void SetName(Control control, string? name) =>
        SetGenerated(control, AutomationProperties.NameProperty, GeneratedNameProperty, name);

    /// <summary>Sets the accessible help text, or clears a previously generated one.</summary>
    public static void SetHelpText(Control control, string? text) =>
        SetGenerated(control, AutomationProperties.HelpTextProperty, GeneratedHelpProperty, text);

    /// <summary>Marks the control as required in a form.</summary>
    public static void SetRequired(Control control, bool required) =>
        AutomationProperties.SetIsRequiredForForm(control, required);

    /// <summary>Overrides the role reported to accessibility tooling.</summary>
    public static void SetControlType(Control control, AutomationControlType type) =>
        AutomationProperties.SetControlTypeOverride(control, type);

    private static void SetGenerated(
        Control control,
        AttachedProperty<string?> property,
        AttachedProperty<string> markerProperty,
        string? value)
    {
        var current = control.GetValue(property);
        var generated = control.GetValue(markerProperty);
        var appOwned = !string.IsNullOrEmpty(current) &&
                       !string.Equals(current, generated, StringComparison.Ordinal);

        if (appOwned)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (!string.IsNullOrEmpty(generated))
                control.ClearValue(property);
            control.SetValue(markerProperty, string.Empty);
            return;
        }

        control.SetValue(property, value);
        control.SetValue(markerProperty, value);
    }
}
