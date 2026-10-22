using Avalonia.Controls;
using Avalonia.Media;

namespace Mav.Theming;

/// <summary>Type scale of the theme: a <see cref="MavTextStyle"/> for each Material 3 role.</summary>
public sealed class MavTypography
{
    public static MavTypography Default { get; } = new();

    public FontFamily BrandFont { get; init; } = MavFonts.RobotoFont;
    public FontFamily PlainFont { get; init; } = MavFonts.RobotoFont;

    public MavTextStyle DisplayLarge => new(57, 64, -0.25, FontWeight.Regular, BrandFont);
    public MavTextStyle DisplayMedium => new(45, 52, 0.0, FontWeight.Regular, BrandFont);
    public MavTextStyle DisplaySmall => new(36, 44, 0.0, FontWeight.Regular, BrandFont);

    public MavTextStyle HeadlineLarge => new(32, 40, 0.0, FontWeight.Regular, BrandFont);
    public MavTextStyle HeadlineMedium => new(28, 36, 0.0, FontWeight.Regular, BrandFont);
    public MavTextStyle HeadlineSmall => new(24, 32, 0.0, FontWeight.Regular, BrandFont);

    public MavTextStyle TitleLarge => new(22, 28, 0.0, FontWeight.Regular, BrandFont);
    public MavTextStyle TitleMedium => new(16, 24, 0.15, FontWeight.Medium, PlainFont);
    public MavTextStyle TitleSmall => new(14, 20, 0.10, FontWeight.Medium, PlainFont);

    public MavTextStyle BodyLarge => new(16, 24, 0.50, FontWeight.Regular, PlainFont);
    public MavTextStyle BodyMedium => new(14, 20, 0.25, FontWeight.Regular, PlainFont);
    public MavTextStyle BodySmall => new(12, 16, 0.40, FontWeight.Regular, PlainFont);

    public MavTextStyle LabelLarge => new(14, 20, 0.10, FontWeight.Medium, PlainFont);
    public MavTextStyle LabelMedium => new(12, 16, 0.50, FontWeight.Medium, PlainFont);
    public MavTextStyle LabelSmall => new(11, 16, 0.50, FontWeight.Medium, PlainFont);

    /// <summary>
    /// Returns the text style for the given type role.
    /// </summary>
    public MavTextStyle For(MavTypeRole role) => role switch
    {
        MavTypeRole.DisplayLarge => DisplayLarge,
        MavTypeRole.DisplayMedium => DisplayMedium,
        MavTypeRole.DisplaySmall => DisplaySmall,
        MavTypeRole.HeadlineLarge => HeadlineLarge,
        MavTypeRole.HeadlineMedium => HeadlineMedium,
        MavTypeRole.HeadlineSmall => HeadlineSmall,
        MavTypeRole.TitleLarge => TitleLarge,
        MavTypeRole.TitleMedium => TitleMedium,
        MavTypeRole.TitleSmall => TitleSmall,
        MavTypeRole.BodyLarge => BodyLarge,
        MavTypeRole.BodyMedium => BodyMedium,
        MavTypeRole.BodySmall => BodySmall,
        MavTypeRole.LabelLarge => LabelLarge,
        MavTypeRole.LabelMedium => LabelMedium,
        MavTypeRole.LabelSmall => LabelSmall,
        _ => BodyMedium,
    };
}
