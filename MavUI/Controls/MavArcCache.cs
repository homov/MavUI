using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Reuses a single arc geometry while its parameters stay unchanged. Instances are
/// owned by controls so a cached geometry never outlives its platform render interface.
/// </summary>
internal sealed class MavArcCache
{
    private ArcKey _key;
    private Geometry? _geometry;
    private bool _hasKey;

    /// <summary>Returns a cached arc geometry for the given center, radius and angles.</summary>
    public Geometry Get(Point center, double radius, double startAngleDeg, double sweepAngleDeg)
    {
        var key = new ArcKey(center.X, center.Y, radius, startAngleDeg, sweepAngleDeg);
        if (_hasKey && key == _key && _geometry is not null)
            return _geometry;

        _key = key;
        _hasKey = true;
        _geometry = MavGeometryHelpers.CreateArc(center, radius, startAngleDeg, sweepAngleDeg);
        return _geometry;
    }

    private readonly record struct ArcKey(
        double CenterX,
        double CenterY,
        double Radius,
        double StartAngle,
        double SweepAngle);
}
