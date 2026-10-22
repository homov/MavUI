using System.ComponentModel;
using Avalonia.Media;

namespace Mav.Theming;

/// <summary>Default implementation of <see cref="IMavThemeService"/>; applies theme changes to <see cref="MavTheme"/>.</summary>
public sealed class MavThemeService : IMavThemeService, IDisposable
{
    public static MavThemeService Instance { get; } = new();

    private readonly EventHandler<MavTheme> _themeChangedHandler;
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<MavTheme>? ThemeChanged;

    /// <summary>Creates the service and subscribes to the application theme changes.</summary>
    public MavThemeService()
    {
        _themeChangedHandler = OnThemeChanged;
        MavTheme.Changed += _themeChangedHandler;
    }

    private void OnThemeChanged(object? _, MavTheme theme)
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
    }

    /// <summary>
    /// Detaches the service from the process-wide <see cref="MavTheme.Changed"/> event.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        MavTheme.Changed -= _themeChangedHandler;
    }

    public MavTheme Current => MavTheme.Current;
    public MavColorScheme ColorScheme => MavTheme.Current.Color;

    public Color SeedColor
    {
        get => MavTheme.SeedColor;
        set => MavTheme.SeedColor = value;
    }

    public MavThemeMode Mode
    {
        get => MavTheme.Mode;
        set => MavTheme.Mode = value;
    }

    public MavSchemeVariant Variant
    {
        get => MavTheme.Variant;
        set => MavTheme.Variant = value;
    }

    public MavContrast Contrast
    {
        get => MavTheme.Contrast;
        set => MavTheme.Contrast = value;
    }

    public MavDensity Density
    {
        get => MavTheme.Density;
        set => MavTheme.Density = value;
    }

    public MavMotionScheme Motion
    {
        get => MavTheme.Motion;
        set => MavTheme.Motion = value;
    }

    public MavShapeScale ShapeScale
    {
        get => MavTheme.ShapeScaleMode;
        set => MavTheme.ShapeScaleMode = value;
    }

    public bool IsDark => MavTheme.Current.IsDark;

    public void Configure(Action<MavThemeOptions> configure) => MavTheme.Configure(configure);

    /// <summary>Applies the custom light and dark schemes to the service.</summary>
    public void ApplyCustomSchemes(MavColorScheme light, MavColorScheme dark) =>
        MavTheme.ApplyCustomSchemes(light, dark);

    public void ClearCustomSchemes() => MavTheme.ClearCustomSchemes();

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
