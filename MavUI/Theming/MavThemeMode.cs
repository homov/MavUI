using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Styling;
using Mav.Platform;

namespace Mav.Theming;

/// <summary>Light or dark appearance of the theme, or follow the system setting.</summary>
public enum MavThemeMode
{
    System,
    Light,
    Dark,
}
