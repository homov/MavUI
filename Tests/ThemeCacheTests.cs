using System;
using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class ThemeCacheTests
{
    [AvaloniaFact]
    public void Brush_cache_trims_in_batches_instead_of_discarding_every_entry()
    {
        var field = typeof(MdTheme).GetField("BrushCache", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var cache = field.GetValue(null);
        Assert.NotNull(cache);
        var countProperty = cache.GetType().GetProperty("Count");
        Assert.NotNull(countProperty);

        var before = (int)countProperty.GetValue(cache)!;
        for (var i = 0; i < 2400; i++)
            MdTheme.BrushOf(Color.FromUInt32(0xFF000000u | (uint)i));

        var after = (int)countProperty.GetValue(cache)!;

        Assert.True(after > 100, $"brush cache was cleared to {after} entries");
        Assert.True(after < before + 2400, "brush cache did not trim above its limit");
    }
}
