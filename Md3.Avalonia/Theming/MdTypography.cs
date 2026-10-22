using Avalonia.Controls;
using Avalonia.Media;

namespace Md3.Avalonia.Theming;

/// <summary>
/// Material Design 3 15-token type scale (Display / Headline / Title / Body / Label).
/// </summary>
public enum MdTypeRole
{
    DisplayLarge,
    DisplayMedium,
    DisplaySmall,
    HeadlineLarge,
    HeadlineMedium,
    HeadlineSmall,
    TitleLarge,
    TitleMedium,
    TitleSmall,
    BodyLarge,
    BodyMedium,
    BodySmall,
    LabelLarge,
    LabelMedium,
    LabelSmall,
}

public readonly record struct MdTextStyle(
    double Size,
    double LineHeight,
    double LetterSpacing,
    FontWeight Weight,
    FontFamily FontFamily)
{
    public double LineHeightRatio => Size > 0 ? LineHeight / Size : 1.25;
}

public sealed class MdTypography
{
    public static MdTypography Default { get; } = new();

    public FontFamily BrandFont { get; init; } = MdFonts.RobotoFont;
    public FontFamily PlainFont { get; init; } = MdFonts.RobotoFont;

    public MdTextStyle DisplayLarge => new(57, 64, -0.25, FontWeight.Regular, BrandFont);
    public MdTextStyle DisplayMedium => new(45, 52, 0.0, FontWeight.Regular, BrandFont);
    public MdTextStyle DisplaySmall => new(36, 44, 0.0, FontWeight.Regular, BrandFont);

    public MdTextStyle HeadlineLarge => new(32, 40, 0.0, FontWeight.Regular, BrandFont);
    public MdTextStyle HeadlineMedium => new(28, 36, 0.0, FontWeight.Regular, BrandFont);
    public MdTextStyle HeadlineSmall => new(24, 32, 0.0, FontWeight.Regular, BrandFont);

    public MdTextStyle TitleLarge => new(22, 28, 0.0, FontWeight.Regular, BrandFont);
    public MdTextStyle TitleMedium => new(16, 24, 0.15, FontWeight.Medium, PlainFont);
    public MdTextStyle TitleSmall => new(14, 20, 0.10, FontWeight.Medium, PlainFont);

    public MdTextStyle BodyLarge => new(16, 24, 0.50, FontWeight.Regular, PlainFont);
    public MdTextStyle BodyMedium => new(14, 20, 0.25, FontWeight.Regular, PlainFont);
    public MdTextStyle BodySmall => new(12, 16, 0.40, FontWeight.Regular, PlainFont);

    public MdTextStyle LabelLarge => new(14, 20, 0.10, FontWeight.Medium, PlainFont);
    public MdTextStyle LabelMedium => new(12, 16, 0.50, FontWeight.Medium, PlainFont);
    public MdTextStyle LabelSmall => new(11, 16, 0.50, FontWeight.Medium, PlainFont);

    public MdTextStyle For(MdTypeRole role) => role switch
    {
        MdTypeRole.DisplayLarge => DisplayLarge,
        MdTypeRole.DisplayMedium => DisplayMedium,
        MdTypeRole.DisplaySmall => DisplaySmall,
        MdTypeRole.HeadlineLarge => HeadlineLarge,
        MdTypeRole.HeadlineMedium => HeadlineMedium,
        MdTypeRole.HeadlineSmall => HeadlineSmall,
        MdTypeRole.TitleLarge => TitleLarge,
        MdTypeRole.TitleMedium => TitleMedium,
        MdTypeRole.TitleSmall => TitleSmall,
        MdTypeRole.BodyLarge => BodyLarge,
        MdTypeRole.BodyMedium => BodyMedium,
        MdTypeRole.BodySmall => BodySmall,
        MdTypeRole.LabelLarge => LabelLarge,
        MdTypeRole.LabelMedium => LabelMedium,
        MdTypeRole.LabelSmall => LabelSmall,
        _ => BodyMedium,
    };
}

public static class MdTypographyExtensions
{
    public static void Apply(this TextBlock label, MdTypeRole role, MdTypography? scale = null)
    {
        var s = (scale ?? MdTypography.Default).For(role);
        s.ApplyTo(label);
    }

    public static void ApplyTo(this MdTextStyle s, TextBlock label)
    {
        label.FontFamily = s.FontFamily;
        label.FontSize = s.Size;
        label.FontWeight = s.Weight;
        label.LetterSpacing = s.LetterSpacing;
    }
}
