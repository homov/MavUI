using Avalonia.Controls;

namespace Hc.Platforms.Desktop;

/// <summary>Desktop host: shows the phone-sized preview window.</summary>
public sealed class DesktopApp : App
{
    protected override Window CreateDesktopWindow() => new PreviewWindow();
}
