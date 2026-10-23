using Avalonia.Controls;

namespace Hc.Platforms.Windows;

/// <summary>Windows host: shows the phone-sized preview window.</summary>
public sealed class WindowsApp : App
{
    protected override Window CreateDesktopWindow() => new PreviewWindow();
}
