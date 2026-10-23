// TEMPORARY PROBE, to be deleted after CI. Checks fluent calls with all namespaces imported.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Mav.Controls;

namespace Mav.ControlsGalleryMarkup.Pages;

internal static class ProbePage
{
    internal static object Margin() => new Border().Margin(new Thickness(1));
    internal static object HAlign() => new Border().HorizontalAlignment(HorizontalAlignment.Left);
    internal static object Width() => new Border().Width(120);
    internal static object BtnEnabled() => new MavButton("x", MavButtonVariant.Outlined).IsEnabled(false);
    internal static object BorderEnabled() => new Border().IsEnabled(false);
}
