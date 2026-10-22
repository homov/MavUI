using Avalonia.Media;
using Mav.Theming;
using Xunit;

namespace Mav.Tests;

public class ContrastTests
{
    [Theory]
    [InlineData(false, MavContrast.Standard)]
    [InlineData(true, MavContrast.Standard)]
    [InlineData(false, MavContrast.Medium)]
    [InlineData(true, MavContrast.Medium)]
    [InlineData(false, MavContrast.High)]
    [InlineData(true, MavContrast.High)]
    public void Primary_role_pairs_meet_wcag_aa(bool dark, MavContrast contrast)
    {
        var scheme = MavColorScheme.FromSeed(MavColors.Purple, dark, contrast);

        AssertPair(scheme.OnPrimary, scheme.Primary, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnSecondary, scheme.Secondary, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnTertiary, scheme.Tertiary, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnError, scheme.Error, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnBackground, scheme.Background, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnSurface, scheme.Surface, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.InverseOnSurface, scheme.InverseSurface, MavAccessibility.MinimumBodyTextContrast);
    }

    [Theory]
    [InlineData(false, MavContrast.Standard)]
    [InlineData(true, MavContrast.Standard)]
    [InlineData(false, MavContrast.Medium)]
    [InlineData(true, MavContrast.Medium)]
    [InlineData(false, MavContrast.High)]
    [InlineData(true, MavContrast.High)]
    public void Container_role_pairs_meet_wcag_aa(bool dark, MavContrast contrast)
    {
        var scheme = MavColorScheme.FromSeed(MavColors.Purple, dark, contrast);

        AssertPair(scheme.OnPrimaryContainer, scheme.PrimaryContainer, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnSecondaryContainer, scheme.SecondaryContainer, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnTertiaryContainer, scheme.TertiaryContainer, MavAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnErrorContainer, scheme.ErrorContainer, MavAccessibility.MinimumBodyTextContrast);
    }

    [Theory]
    [InlineData(false, MavContrast.Standard)]
    [InlineData(true, MavContrast.Standard)]
    [InlineData(false, MavContrast.High)]
    [InlineData(true, MavContrast.High)]
    public void Secondary_text_meets_aa_on_surface_containers(bool dark, MavContrast contrast)
    {
        var scheme = MavColorScheme.FromSeed(MavColors.Purple, dark, contrast);

        AssertPair(scheme.OnSurfaceVariant, scheme.Surface, MavAccessibility.MinimumLargeTextContrast);
        AssertPair(scheme.OnSurfaceVariant, scheme.SurfaceContainer, MavAccessibility.MinimumLargeTextContrast);
        AssertPair(scheme.OnSurfaceVariant, scheme.SurfaceContainerHigh, MavAccessibility.MinimumLargeTextContrast);
        AssertPair(scheme.Outline, scheme.Surface, MavAccessibility.MinimumLargeTextContrast);
    }

    [Fact]
    public void Relative_luminance_matches_wcag_reference_values()
    {
        Assert.Equal(0.0, MavAccessibility.RelativeLuminance(Colors.Black), 3);
        Assert.Equal(1.0, MavAccessibility.RelativeLuminance(Colors.White), 3);
    }

    [Fact]
    public void Black_on_white_is_twenty_one_to_one()
    {
        Assert.Equal(21.0, MavAccessibility.ContrastRatio(Colors.Black, Colors.White), 2);
        Assert.Equal(21.0, MavAccessibility.ContrastRatio(Colors.White, Colors.Black), 2);
        Assert.Equal(1.0, MavAccessibility.ContrastRatio(Colors.White, Colors.White), 2);
    }

    private static void AssertPair(Color foreground, Color background, double minimum)
    {
        var ratio = MavAccessibility.ContrastRatio(foreground, background);
        Assert.True(
            ratio + 1e-9 >= minimum,
            $"Contrast {ratio:F2} is below {minimum:F1} for {foreground} on {background}");
    }
}
