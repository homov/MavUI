using System;
using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Md3.Avalonia.Platform;
using Xunit;

namespace Md3.Avalonia.Tests;

public class TextLayoutCacheTests
{
    [AvaloniaFact]
    public void Identical_text_layout_requests_reuse_the_same_formatted_text()
    {
        var first = MdTextMetrics.GetFormattedText(
            "Label",
            14,
            MdFonts.RobotoRegularTypeface,
            Colors.Black);
        var second = MdTextMetrics.GetFormattedText(
            "Label",
            14,
            MdFonts.RobotoRegularTypeface,
            Colors.Black);

        Assert.Same(first, second);
    }

    [AvaloniaFact]
    public void Text_layout_cache_separates_color_wrapping_and_trimming()
    {
        var plain = MdTextMetrics.GetFormattedText(
            "Label",
            14,
            MdFonts.RobotoRegularTypeface,
            Colors.Black);

        var colored = MdTextMetrics.GetFormattedText(
            "Label",
            14,
            MdFonts.RobotoRegularTypeface,
            Colors.Red);

        var wrapped = MdTextMetrics.GetFormattedText(
            "Label",
            14,
            MdFonts.RobotoRegularTypeface,
            Colors.Black,
            maxTextWidth: 40,
            maxLineCount: 1,
            trimming: TextTrimming.CharacterEllipsis);

        Assert.NotSame(plain, colored);
        Assert.NotSame(plain, wrapped);
    }

    [AvaloniaFact]
    public void Width_cache_is_cleared_when_the_font_manager_changes()
    {
        MdTextMetrics.MeasureWidth("Measured", 14);

        var cacheField = typeof(MdTextMetrics).GetField(
            "WidthCache", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(cacheField);
        var cache = Assert.IsAssignableFrom<System.Collections.ICollection>(cacheField.GetValue(null));
        Assert.True(cache.Count > 0);

        // Simulate a host replacing (or losing) the FontManager whose widths were cached.
        var fontManagerField = typeof(MdTextMetrics).GetField(
            "_fontManager", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(fontManagerField);
        fontManagerField.SetValue(null, new WeakReference<FontManager>(null!));

        var width = MdTextMetrics.MeasureWidth("Measured", 14);
        Assert.True(width > 0);
        Assert.True(cache.Count == 1, $"width cache was not invalidated (count {cache.Count})");
    }
}
