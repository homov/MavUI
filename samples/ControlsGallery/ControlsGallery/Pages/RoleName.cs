namespace Mav.ControlsGallery.Pages;

/// <summary>"SurfaceContainerHigh" becomes "Surface Container High" for the swatch labels.</summary>
internal static class RoleName
{
    public static string SplitWords(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
}
