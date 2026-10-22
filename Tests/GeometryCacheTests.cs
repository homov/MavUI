using Avalonia;
using Avalonia.Headless.XUnit;
using Mav.Controls;
using Mav.Primitives;
using Xunit;

namespace Mav.Tests;

public class GeometryCacheTests
{
    [AvaloniaFact]
    public void Arc_cache_reuses_geometry_for_identical_parameters()
    {
        var cache = new MavArcCache();

        var first = cache.Get(new Point(40, 40), 30, -90, 270);
        var second = cache.Get(new Point(40, 40), 30, -90, 270);

        Assert.Same(first, second);
    }

    [AvaloniaFact]
    public void Arc_cache_rebuilds_geometry_when_parameters_change()
    {
        var cache = new MavArcCache();

        var first = cache.Get(new Point(40, 40), 30, -90, 270);
        var resized = cache.Get(new Point(50, 50), 40, -90, 270);
        var rotated = cache.Get(new Point(50, 50), 40, -45, 270);

        Assert.NotSame(first, resized);
        Assert.NotSame(resized, rotated);
    }

    [AvaloniaFact]
    public void Field_outline_cache_reuses_geometry_for_identical_parameters()
    {
        var cache = new MavFieldChromeCache();

        var first = cache.Get(320, 56, 1, 0);
        var second = cache.Get(320, 56, 1, 0);

        Assert.Same(first, second);
    }

    [AvaloniaFact]
    public void Field_outline_cache_rebuilds_geometry_when_key_changes()
    {
        var cache = new MavFieldChromeCache();

        var plain = cache.Get(320, 56, 1, 0);
        var resized = cache.Get(321, 56, 1, 0);
        var focused = cache.Get(321, 56, 2, 0);
        var notched = cache.Get(321, 56, 2, 64);

        Assert.NotSame(plain, resized);
        Assert.NotSame(resized, focused);
        Assert.NotSame(focused, notched);
    }
}
