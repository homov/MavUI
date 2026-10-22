using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Severity of a <see cref="MavInfoBar"/>, which selects its colors and icon.</summary>
public enum MavInfoBarSeverity
{
    Info,
    Success,
    Warning,
    Error,
}
