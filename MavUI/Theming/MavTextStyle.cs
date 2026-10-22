using Avalonia.Controls;
using Avalonia.Media;

namespace Mav.Theming;

/// <summary>A complete text style: font family, size, weight, line height and letter spacing.</summary>
public readonly record struct MavTextStyle(
    double Size,
    double LineHeight,
    double LetterSpacing,
    FontWeight Weight,
    FontFamily FontFamily)
{
    public double LineHeightRatio => Size > 0 ? LineHeight / Size : 1.25;
}
