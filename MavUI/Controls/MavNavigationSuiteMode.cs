using System.Collections;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Layout chosen by <see cref="MavNavigationSuiteScaffold"/> for the current width: bottom bar, rail or drawer.</summary>
public enum MavNavigationSuiteMode
{
    Auto,
    NavigationBar,
    NavigationRail,
    NavigationDrawer,
    None,
}
