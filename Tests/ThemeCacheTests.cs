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
    public void Brush_cache_stays_bounded_and_keeps_recently_used_entries()
    {
        var field = typeof(MdTheme).GetField("BrushCache", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var cache = field.GetValue(null)!;
        var countProperty = cache.GetType().GetProperty("Count")!;

        // A color that is used on every iteration must survive eviction (LRU, not arbitrary order).
        var hot = Color.FromUInt32(0xFF123456u);
        var hotBrush = MdTheme.BrushOf(hot);
        for (var i = 0; i < 4000; i++)
        {
            MdTheme.BrushOf(Color.FromUInt32(0xFF000000u | (uint)i));
            Assert.Same(hotBrush, MdTheme.BrushOf(hot));
        }

        var after = (int)countProperty.GetValue(cache)!;
        Assert.True(after <= 2048, $"brush cache exceeded its limit: {after}");
        Assert.True(after > 100, $"brush cache was cleared to {after} entries");
    }
}
