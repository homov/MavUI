using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Mav.Platform;

/// <summary>
/// Keeps horizontal drags away from the platform's system-back edge zone so Android's
/// predictive/gesture back can win the gesture instead of a slider, carousel or list row.
/// </summary>
public static class MavGestureZones
{
    /// <summary>Width of the system back-gesture edge zone in DIPs.</summary>
    public const double DefaultSystemEdgeWidth = 20;

    /// <summary>
    /// True when <paramref name="positionInVisual"/> lies inside the left or right
    /// system gesture edge of the current top level.
    /// </summary>
    public static bool IsInSystemEdge(
        Visual visual,
        Point positionInVisual,
        double edgeWidth = DefaultSystemEdgeWidth)
    {
        if (edgeWidth <= 0 || TopLevel.GetTopLevel(visual) is not { } topLevel)
            return false;

        var width = topLevel.ClientSize.Width;
        if (width <= 0)
            return false;

        var origin = visual.TranslatePoint(positionInVisual, topLevel);
        if (origin is null)
            return false;

        var x = origin.Value.X;
        return x <= edgeWidth || x >= width - edgeWidth;
    }

    /// <summary>
    /// True when a pressed event starts inside the system edge zone and the control
    /// should therefore not begin a horizontal drag or capture the pointer.
    /// </summary>
    public static bool ShouldDeferToSystem(
        Visual visual,
        PointerPressedEventArgs e,
        double edgeWidth = DefaultSystemEdgeWidth) =>
        e.Pointer.Type != PointerType.Mouse &&
        IsInSystemEdge(visual, e.GetPosition(visual), edgeWidth);
}
