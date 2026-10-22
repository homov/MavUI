using System;
using Avalonia.Headless.XUnit;
using Mav;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class CarouselLayoutTests
{
    private static MavCarousel CreateCarousel(int items = 4)
    {
        var carousel = new MavCarousel { Width = 400, Height = 210 };
        for (var i = 0; i < items; i++)
            carousel.AddItem($"Item {i}", $"Subtitle {i}", MavIcons.Star);
        carousel.SelectedIndex = 0;
        return carousel;
    }

    [AvaloniaFact]
    public void Hero_layout_makes_the_focused_card_the_widest()
    {
        var carousel = CreateCarousel();
        carousel.Layout = MavCarouselLayout.Hero;
        var layouts = carousel.ComputeCardLayouts(400);

        Assert.Equal(4, layouts.Count);
        Assert.True(layouts[0].W > layouts[1].W);
        Assert.True(layouts[1].W > layouts[2].W);
        Assert.Equal(0, layouts[0].X, 1);
    }

    [AvaloniaFact]
    public void Center_aligned_layout_centers_the_focused_card()
    {
        var carousel = CreateCarousel();
        carousel.Layout = MavCarouselLayout.CenterAligned;
        var layouts = carousel.ComputeCardLayouts(400);

        Assert.Equal(200, layouts[0].X + layouts[0].W / 2, 1);
        Assert.True(layouts[0].W > layouts[1].W);
    }

    [AvaloniaFact]
    public void Multi_browse_layout_uses_equal_small_cards()
    {
        var carousel = CreateCarousel();
        carousel.Layout = MavCarouselLayout.MultiBrowse;
        var layouts = carousel.ComputeCardLayouts(400);

        Assert.Equal(0, layouts[0].X, 1);
        Assert.True(Math.Abs(layouts[0].W - layouts[1].W) < layouts[0].W * 0.15);
        Assert.True(layouts[0].W < 160);
    }

    [AvaloniaFact]
    public void Uncontained_layout_centers_a_wide_card_that_bleeds_past_the_edges()
    {
        var carousel = CreateCarousel();
        carousel.Layout = MavCarouselLayout.Uncontained;
        var layouts = carousel.ComputeCardLayouts(400);

        Assert.Equal(200, layouts[0].X + layouts[0].W / 2, 1);
        Assert.True(layouts[0].W > 160);
        Assert.True(layouts[1].X + layouts[1].W > 400);
    }

    [AvaloniaFact]
    public void Hero_layout_scrolls_the_focused_card_to_the_leading_edge()
    {
        var carousel = CreateCarousel();
        carousel.Layout = MavCarouselLayout.Hero;
        var layouts = carousel.ComputeCardLayouts(400, 2);

        Assert.Equal(0, layouts[2].X, 1);
        Assert.True(layouts[2].W > layouts[1].W);
        Assert.True(layouts[2].W > layouts[3].W);
    }

    [AvaloniaFact]
    public void Multi_browse_layout_scrolls_to_keep_the_focused_card_visible()
    {
        var carousel = CreateCarousel();
        carousel.Layout = MavCarouselLayout.MultiBrowse;
        var layouts = carousel.ComputeCardLayouts(400, 3);

        Assert.Equal(400, layouts[3].X + layouts[3].W, 1);
        Assert.True(layouts[3].X < 400);
    }

    [AvaloniaFact]
    public void Switching_layout_keeps_the_selected_item()
    {
        var carousel = CreateCarousel();
        carousel.SelectedIndex = 2;

        carousel.Layout = MavCarouselLayout.MultiBrowse;

        Assert.Equal(2, carousel.SelectedIndex);
        Assert.Equal(2, carousel.CurrentIndex);
    }
}
