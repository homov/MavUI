using System.ComponentModel;
using Avalonia.Media;

namespace Md3.Avalonia.Theming;

/// <summary>
/// Injectable service facade over <see cref="MdTheme"/> for MVVM / DI consumers.
/// </summary>
public interface IMdThemeService : INotifyPropertyChanged
{
    MdTheme Current { get; }
    MdColorScheme ColorScheme { get; }
    Color SeedColor { get; set; }
    MdThemeMode Mode { get; set; }
    MdSchemeVariant Variant { get; set; }
    MdContrast Contrast { get; set; }
    MdDensity Density { get; set; }
    MdMotionScheme Motion { get; set; }
    MdShapeScale ShapeScale { get; set; }
    bool IsDark { get; }

    event EventHandler<MdTheme>? ThemeChanged;

    void Configure(Action<MdThemeOptions> configure);
    void ApplyCustomSchemes(MdColorScheme light, MdColorScheme dark);
    void ClearCustomSchemes();
}

public sealed class MdThemeService : IMdThemeService
{
    public static MdThemeService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MdTheme>? ThemeChanged;

    public MdThemeService()
    {
        MdTheme.Changed += (_, theme) =>
        {
            ThemeChanged?.Invoke(this, theme);
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(ColorScheme));
            OnPropertyChanged(nameof(SeedColor));
            OnPropertyChanged(nameof(Mode));
            OnPropertyChanged(nameof(Variant));
            OnPropertyChanged(nameof(Contrast));
            OnPropertyChanged(nameof(Density));
            OnPropertyChanged(nameof(Motion));
            OnPropertyChanged(nameof(ShapeScale));
            OnPropertyChanged(nameof(IsDark));
        };
    }

    public MdTheme Current => MdTheme.Current;
    public MdColorScheme ColorScheme => MdTheme.Current.Color;

    public Color SeedColor
    {
        get => MdTheme.SeedColor;
        set => MdTheme.SeedColor = value;
    }

    public MdThemeMode Mode
    {
        get => MdTheme.Mode;
        set => MdTheme.Mode = value;
    }

    public MdSchemeVariant Variant
    {
        get => MdTheme.Variant;
        set => MdTheme.Variant = value;
    }

    public MdContrast Contrast
    {
        get => MdTheme.Contrast;
        set => MdTheme.Contrast = value;
    }

    public MdDensity Density
    {
        get => MdTheme.Density;
        set => MdTheme.Density = value;
    }

    public MdMotionScheme Motion
    {
        get => MdTheme.Motion;
        set => MdTheme.Motion = value;
    }

    public MdShapeScale ShapeScale
    {
        get => MdTheme.ShapeScaleMode;
        set => MdTheme.ShapeScaleMode = value;
    }

    public bool IsDark => MdTheme.Current.IsDark;

    public void Configure(Action<MdThemeOptions> configure) => MdTheme.Configure(configure);

    public void ApplyCustomSchemes(MdColorScheme light, MdColorScheme dark) =>
        MdTheme.ApplyCustomSchemes(light, dark);

    public void ClearCustomSchemes() => MdTheme.ClearCustomSchemes();

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
