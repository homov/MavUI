using Avalonia.Media;
using Md3.Avalonia.Theming;
using Xunit;

namespace Md3.Avalonia.Tests;

public class ContrastTests
{
    [Theory]
    [InlineData(false, MdContrast.Standard)]
    [InlineData(true, MdContrast.Standard)]
    [InlineData(false, MdContrast.Medium)]
    [InlineData(true, MdContrast.Medium)]
    [InlineData(false, MdContrast.High)]
    [InlineData(true, MdContrast.High)]
    public void Primary_role_pairs_meet_wcag_aa(bool dark, MdContrast contrast)
    {
        var scheme = MdColorScheme.FromSeed(MdColors.Purple, dark, contrast);

        AssertPair(scheme.OnPrimary, scheme.Primary, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnSecondary, scheme.Secondary, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnTertiary, scheme.Tertiary, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnError, scheme.Error, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnBackground, scheme.Background, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnSurface, scheme.Surface, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.InverseOnSurface, scheme.InverseSurface, MdAccessibility.MinimumBodyTextContrast);
    }

    [Theory]
    [InlineData(false, MdContrast.Standard)]
    [InlineData(true, MdContrast.Standard)]
    [InlineData(false, MdContrast.Medium)]
    [InlineData(true, MdContrast.Medium)]
    [InlineData(false, MdContrast.High)]
    [InlineData(true, MdContrast.High)]
    public void Container_role_pairs_meet_wcag_aa(bool dark, MdContrast contrast)
    {
        var scheme = MdColorScheme.FromSeed(MdColors.Purple, dark, contrast);

        AssertPair(scheme.OnPrimaryContainer, scheme.PrimaryContainer, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnSecondaryContainer, scheme.SecondaryContainer, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnTertiaryContainer, scheme.TertiaryContainer, MdAccessibility.MinimumBodyTextContrast);
        AssertPair(scheme.OnErrorContainer, scheme.ErrorContainer, MdAccessibility.MinimumBodyTextContrast);
    }

    [Theory]
    [InlineData(false, MdContrast.Standard)]
    [InlineData(true, MdContrast.Standard)]
    [InlineData(false, MdContrast.High)]
    [InlineData(true, MdContrast.High)]
    public void Secondary_text_meets_aa_on_surface_containers(bool dark, MdContrast contrast)
    {
        var scheme = MdColorScheme.FromSeed(MdColors.Purple, dark, contrast);

        AssertPair(scheme.OnSurfaceVariant, scheme.Surface, MdAccessibility.MinimumLargeTextContrast);
        AssertPair(scheme.OnSurfaceVariant, scheme.SurfaceContainer, MdAccessibility.MinimumLargeTextContrast);
        AssertPair(scheme.OnSurfaceVariant, scheme.SurfaceContainerHigh, MdAccessibility.MinimumLargeTextContrast);
        AssertPair(scheme.Outline, scheme.Surface, MdAccessibility.MinimumLargeTextContrast);
    }

    [Fact]
    public void Relative_luminance_matches_wcag_reference_values()
    {
        Assert.Equal(0.0, MdAccessibility.RelativeLuminance(Colors.Black), 3);
        Assert.Equal(1.0, MdAccessibility.RelativeLuminance(Colors.White), 3);
    }

    [Fact]
    public void Black_on_white_is_twenty_one_to_one()
    {
        Assert.Equal(21.0, MdAccessibility.ContrastRatio(Colors.Black, Colors.White), 2);
        Assert.Equal(21.0, MdAccessibility.ContrastRatio(Colors.White, Colors.Black), 2);
        Assert.Equal(1.0, MdAccessibility.ContrastRatio(Colors.White, Colors.White), 2);
    }

    private static void AssertPair(Color foreground, Color background, double minimum)
    {
        var ratio = MdAccessibility.ContrastRatio(foreground, background);
        Assert.True(
            ratio + 1e-9 >= minimum,
            $"Contrast {ratio:F2} is below {minimum:F1} for {foreground} on {background}");
    }
}
