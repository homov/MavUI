using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Geometry helpers shared by the progress indicators: arc and wave path construction.</summary>
internal static class MavGeometryHelpers
{
    /// <summary>Builds an arc geometry from start angle and sweep, in degrees.</summary>
    public static Geometry CreateArc(Point center, double radius, double startAngleDeg, double sweepAngleDeg)
    {
        var geom = new StreamGeometry();
        if (radius <= 0 || Math.Abs(sweepAngleDeg) < 0.1)
            return geom;

        var clampedSweep = Math.Clamp(sweepAngleDeg, -359.9, 359.9);
        using var ctx = geom.Open();

        // Split sweeps >= 180° into two segments so SVG/Skia arc parameterization is never degenerate.
        int segments = Math.Abs(clampedSweep) > 179.0 ? 2 : 1;
        double segSweep = clampedSweep / segments;

        var startRad = startAngleDeg * Math.PI / 180.0;
        var startPt = new Point(
            center.X + radius * Math.Cos(startRad),
            center.Y + radius * Math.Sin(startRad));
        ctx.BeginFigure(startPt, isFilled: false);

        var sweepDir = clampedSweep >= 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
        for (int i = 1; i <= segments; i++)
        {
            var endDeg = startAngleDeg + segSweep * i;
            var endRad = endDeg * Math.PI / 180.0;
            var endPt = new Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));
            ctx.ArcTo(
                endPt,
                new Size(radius, radius),
                0,
                Math.Abs(segSweep) > 180.0,
                sweepDir);
        }

        ctx.EndFigure(isClosed: false);
        return geom;
    }
}
