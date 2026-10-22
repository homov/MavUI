using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>State of a <see cref="MavRefreshIndicator"/> during a pull-to-refresh gesture.</summary>
public enum MavRefreshIndicatorState
{
    Idle,
    Pulling,
    Armed,
    Refreshing,
}
