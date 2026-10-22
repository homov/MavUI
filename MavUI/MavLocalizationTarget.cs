using System.Globalization;
using System.Resources;
using Avalonia;
using Avalonia.Controls;

namespace Mav;

/// <summary>Selects which property an attached localization key updates.</summary>
public enum MavLocalizationTarget
{
    /// <summary>Pick the natural text property for the control type.</summary>
    Auto,
    /// <summary><see cref="TextBlock.Text"/> or <see cref="Controls.MavButton.Text"/>.</summary>
    Text,
    /// <summary><see cref="Controls.MavTextField.Label"/> or <see cref="Controls.MavSelect.Label"/>.</summary>
    Label,
    /// <summary><see cref="Controls.MavTextField.Placeholder"/> or <see cref="Controls.MavSearchBar.Placeholder"/>.</summary>
    Placeholder,
    /// <summary><see cref="Window.Title"/> or <see cref="Controls.MavDialog.Title"/>.</summary>
    Title,
    /// <summary>The control's tooltip.</summary>
    Tooltip,
}
