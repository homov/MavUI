using System;
using Avalonia;
using Avalonia.Headless.XUnit;
using Mav.Controls;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

/// <summary>
/// The shape scale is a global multiplier on corner radii (Compact ×0.72, Standard ×1.0,
/// Expressive ×1.28). It is only worth a switch in the gallery if controls actually follow it.
/// </summary>
public class ShapeScaleTests
{
    private static void WithScale(MavShapeScale scale, Action body)
    {
        var previous = MavTheme.ShapeScaleMode;
        try
        {
            MavTheme.ShapeScaleMode = scale;
            body();
        }
        finally
        {
            MavTheme.ShapeScaleMode = previous;
        }
    }

    [AvaloniaFact]
    public void Tokens_multiply_every_radius()
    {
        var standard = new MavShapeTokens(MavShapeScale.Standard);
        var compact = new MavShapeTokens(MavShapeScale.Compact);
        var expressive = new MavShapeTokens(MavShapeScale.Expressive);

        Assert.Equal(12, standard.Medium);
        Assert.Equal(8.6, compact.Medium, 3);
        Assert.Equal(15.4, expressive.Medium, 3);

        // 0 and the pill radius are not scaled: they have no "smaller" or "rounder" variant.
        Assert.Equal(0, compact.None);
        Assert.Equal(999, compact.Full);
    }

    [AvaloniaFact]
    public void A_card_follows_the_scale()
    {
        WithScale(MavShapeScale.Compact, () =>
            Assert.Equal(8.6, new MavCard().CornerRadius, 3));

        WithScale(MavShapeScale.Expressive, () =>
            Assert.Equal(15.4, new MavCard().CornerRadius, 3));
    }

    [AvaloniaFact]
    public void A_card_re_rounds_when_the_scale_changes()
    {
        var card = new MavCard();
        WithScale(MavShapeScale.Expressive, () =>
            Assert.Equal(15.4, card.CornerRadius, 3));
        WithScale(MavShapeScale.Standard, () =>
            Assert.Equal(12, card.CornerRadius, 3));
    }

    [AvaloniaFact]
    public void An_explicit_radius_opts_out_of_the_scale()
    {
        var card = new MavCard { CardRadius = 4 };
        Assert.Equal(4, card.CornerRadius);

        WithScale(MavShapeScale.Expressive, () =>
            Assert.Equal(4, card.CornerRadius));
    }
}
