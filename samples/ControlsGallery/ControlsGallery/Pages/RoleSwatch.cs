using Avalonia.Media;

namespace Mav.ControlsGallery.Pages;

/// <summary>
/// One colour-role swatch. <paramref name="Name"/> is the role name and <paramref name="Label"/> the
/// same name split into words, which is what the tile shows: long names wrap inside 132 px, and the
/// text colour comes from the role itself (some roles are white, some are near-black).
/// </summary>
public sealed record RoleSwatch(string Name, IBrush Background, IBrush Foreground)
{
    public string Label => RoleName.SplitWords(Name);
}
