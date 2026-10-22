using System;
using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Mav.Platform;
using Xunit;

namespace Mav.Tests;

public class TextLayoutCacheTests
{
    [AvaloniaFact]
    public void Identical_text_layout_requests_reuse_the_same_formatted_text()
    {
        var first = MavTextMetrics.GetFormattedText(
            "Label",
            14,
            MavFonts.RobotoRegularTypeface,
            Colors.Black);
        var second = MavTextMetrics.GetFormattedText(
            "Label",
            14,
            MavFonts.RobotoRegularTypeface,
            Colors.Black);

        Assert.Same(first, second);
    }

    [AvaloniaFact]
    public void Text_layout_cache_separates_color_wrapping_and_trimming()
    {
        var plain = MavTextMetrics.GetFormattedText(
            "Label",
            14,
            MavFonts.RobotoRegularTypeface,
            Colors.Black);

        var colored = MavTextMetrics.GetFormattedText(
            "Label",
            14,
            MavFonts.RobotoRegularTypeface,
            Colors.Red);

        var wrapped = MavTextMetrics.GetFormattedText(
            "Label",
            14,
            MavFonts.RobotoRegularTypeface,
            Colors.Black,
            maxTextWidth: 40,
            maxLineCount: 1,
            trimming: TextTrimming.CharacterEllipsis);

        Assert.NotSame(plain, colored);
        Assert.NotSame(plain, wrapped);
    }

    [AvaloniaFact]
    public void Wrap_widths_within_one_step_share_the_same_layout()
    {
        // Resizing or animating a width by a fraction of a step must not create a new layout.
        var a = MavTextMetrics.GetFormattedText("Wrapped label", 14, MavFonts.RobotoRegularTypeface,
            Colors.Black, maxTextWidth: 101.0, maxLineCount: 2);
        var b = MavTextMetrics.GetFormattedText("Wrapped label", 14, MavFonts.RobotoRegularTypeface,
            Colors.Black, maxTextWidth: 102.5, maxLineCount: 2);

        Assert.Same(a, b);
        Assert.True(a.MaxTextWidth <= 101.0, $"layout got more room than requested: {a.MaxTextWidth}");
    }

    [AvaloniaFact]
    public void Width_cache_is_cleared_when_the_font_manager_changes()
    {
        MavTextMetrics.MeasureWidth("Measured", 14);

        var cacheField = typeof(MavTextMetrics).GetField(
            "WidthCache", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(cacheField);
        var cache = cacheField.GetValue(null)!;
        var countProperty = cache.GetType().GetProperty("Count")!;
        Assert.True((int)countProperty.GetValue(cache)! > 0);

        // Simulate a host replacing (or losing) the FontManager whose widths were cached.
        var fontManagerField = typeof(MavTextMetrics).GetField(
            "_fontManager", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(fontManagerField);
        fontManagerField.SetValue(null, new WeakReference<FontManager>(null!));

        var width = MavTextMetrics.MeasureWidth("Measured", 14);
        Assert.True(width > 0);
        var count = (int)countProperty.GetValue(cache)!;
        Assert.True(count == 1, $"width cache was not invalidated (count {count})");
    }
}
