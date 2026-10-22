using Avalonia.Controls;
using Avalonia.Media;

namespace Mav.Theming;

/// <summary>Helpers for applying a type role from <see cref="MavTypography"/> to text.</summary>
public static class MavTypographyExtensions
{
    /// <summary>Applies the type role and optional scale to <paramref name="label"/>.</summary>
    public static void Apply(this TextBlock label, MavTypeRole role, MavTypography? scale = null)
    {
        var s = (scale ?? MavTypography.Default).For(role);
        s.ApplyTo(label);
    }

    /// <summary>Applies the text style to <paramref name="label"/>.</summary>
    public static void ApplyTo(this MavTextStyle s, TextBlock label)
    {
        label.FontFamily = s.FontFamily;
        label.FontSize = s.Size;
        label.FontWeight = s.Weight;
        label.LetterSpacing = s.LetterSpacing;
    }
}
