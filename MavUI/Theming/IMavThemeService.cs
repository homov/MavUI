using System.ComponentModel;
using Avalonia.Media;

namespace Mav.Theming;

/// <summary>
/// Injectable service facade over <see cref="MavTheme"/> for MVVM / DI consumers.
/// </summary>
public interface IMavThemeService : INotifyPropertyChanged, IDisposable
{
    MavTheme Current { get; }
    MavColorScheme ColorScheme { get; }
    Color SeedColor { get; set; }
    MavThemeMode Mode { get; set; }
    MavSchemeVariant Variant { get; set; }
    MavContrast Contrast { get; set; }
    MavDensity Density { get; set; }
    MavMotionScheme Motion { get; set; }
    MavShapeScale ShapeScale { get; set; }
    bool IsDark { get; }

    event EventHandler<MavTheme>? ThemeChanged;

    void Configure(Action<MavThemeOptions> configure);
    void ApplyCustomSchemes(MavColorScheme light, MavColorScheme dark);
    void ClearCustomSchemes();
}
