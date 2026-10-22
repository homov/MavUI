using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Styling;
using Mav.Platform;

namespace Mav.Theming;

public enum MavThemeMode
{
    System,
    Light,
    Dark,
}

/// <summary>
/// Central Material Design 3 theme state with reactive change notifications and shared brush cache.
/// Controls subscribe via <see cref="Observe"/> so seed, contrast, density,
/// or light/dark changes propagate live without rebuilding the visual tree.
/// </summary>
public sealed class MavTheme
{
    private const int CacheLimit = 2048;
    private static readonly MavLruCache<uint, IImmutableSolidColorBrush> BrushCache = new(CacheLimit);
    private static readonly MavLruCache<ulong, IPen> PenCache = new(CacheLimit);
    private static readonly ImmutableSolidColorBrush TransparentBrush = new(Colors.Transparent);

    private static Color _seed = MavColors.Purple;
    private static bool _useSystemAccent;
    private static MavThemeMode _mode = MavThemeMode.System;
    private static MavSchemeVariant _schemeVariant = MavSchemeVariant.TonalSpot;
    private static MavContrast _contrast = MavContrast.Standard;
    private static MavDensity _density = MavDensity.Comfortable;
    private static MavMotionScheme _motion = MavMotionScheme.Expressive;
    private static MavShapeScale _shapeScale = MavShapeScale.Standard;
    private static MavTypography _typography = MavTypography.Default;
    private static MavColorScheme? _customLight;
    private static MavColorScheme? _customDark;
    private static MavTheme? _current;
    private static bool _hookedApp;
    private static int _version;

    public static event EventHandler<MavTheme>? Changed;

    public MavColorScheme Color { get; }
    public MavTypography Typography { get; }
    public MavShapeTokens Shape { get; }
    public MavElevationTokens Elevation { get; } = MavElevationTokens.Default;
    public MavStateLayerTokens StateLayer { get; } = MavStateLayerTokens.Default;
    public MavDensity ScaleDensity { get; }
    public MavContrast ScaleContrast { get; }
    public MavSchemeVariant SchemeVariant { get; }
    public MavMotionScheme MotionScheme { get; }
    public MavShapeScale ShapeScale { get; }
    public MavThemeMode ThemeMode { get; }
    public bool IsDark => Color.IsDark;

    private MavTheme(
        MavColorScheme scheme,
        MavTypography typography,
        MavDensity density,
        MavContrast contrast,
        MavThemeMode mode,
        MavSchemeVariant schemeVariant,
        MavMotionScheme motion,
        MavShapeScale shapeScale)
    {
        Color = scheme;
        Typography = typography;
        Shape = new MavShapeTokens(shapeScale);
        ScaleDensity = density;
        ScaleContrast = contrast;
        ThemeMode = mode;
        SchemeVariant = schemeVariant;
        MotionScheme = motion;
        ShapeScale = shapeScale;
    }

    public static Color ColorOf(MavColorRole role) => Current.Color.Get(role);

    public static IImmutableSolidColorBrush BrushOf(MavColorRole role) => BrushOf(Current.Color.Get(role));

    /// <summary>
    /// Returns a cached <see cref="IImmutableSolidColorBrush"/> for <paramref name="color"/> to avoid per-frame brush allocations
    /// and compositor resource tracking overhead.
    /// </summary>
    public static IImmutableSolidColorBrush BrushOf(Color color)
    {
        if (color.A == 0)
            return TransparentBrush;

        var key = color.ToUInt32();
        if (BrushCache.TryGet(key, out var existing))
            return existing;

        var created = new ImmutableSolidColorBrush(color);
        BrushCache.Set(key, created);
        return created;
    }

    /// <summary>
    /// Returns a cached <see cref="IPen"/> for the specified color, thickness, line cap, and line join
    /// to eliminate per-frame <see cref="Pen"/> allocations inside custom <c>Render</c> overrides.
    /// </summary>
    public static IPen PenOf(
        Color color,
        double thickness = 1.0,
        PenLineCap lineCap = PenLineCap.Flat,
        PenLineJoin lineJoin = PenLineJoin.Miter)
    {
        var tQuant = (uint)Math.Clamp((int)Math.Round(thickness * 100.0), 0, 0xFFFFF);
        var styleBits = (ulong)((tQuant << 4) | (((uint)lineCap & 0x3) << 2) | ((uint)lineJoin & 0x3));
        var key = ((ulong)color.ToUInt32() << 32) | styleBits;

        if (PenCache.TryGet(key, out var existing))
            return existing;

        var created = new Pen(BrushOf(color), thickness, lineCap: lineCap, lineJoin: lineJoin);
        PenCache.Set(key, created);
        return created;
    }

    public static MavTheme Current
    {
        get
        {
            EnsureAppHook();
            return _current ??= Build();
        }
    }

    public static Color SeedColor
    {
        get => _seed;
        set
        {
            if (_seed == value && !_useSystemAccent && _customLight is null && _customDark is null) return;
            _seed = value;
            _useSystemAccent = false;
            _customLight = null;
            _customDark = null;
            Rebuild();
        }
    }

    /// <summary>
    /// When <see langword="true"/>, derives the HCT dynamic color scheme from the operating system's accent color.
    /// </summary>
    public static bool UseSystemAccentColor
    {
        get => _useSystemAccent;
        set
        {
            if (_useSystemAccent == value) return;
            _useSystemAccent = value;
            Rebuild();
        }
    }

    public static MavThemeMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = value switch
                {
                    MavThemeMode.Light => ThemeVariant.Light,
                    MavThemeMode.Dark => ThemeVariant.Dark,
                    _ => ThemeVariant.Default,
                };
            }
            Rebuild();
        }
    }

    public static MavSchemeVariant Variant
    {
        get => _schemeVariant;
        set
        {
            if (_schemeVariant == value) return;
            _schemeVariant = value;
            Rebuild();
        }
    }

    public static MavContrast Contrast
    {
        get => _contrast;
        set
        {
            if (_contrast == value) return;
            _contrast = value;
            Rebuild();
        }
    }

    public static MavDensity Density
    {
        get => _density;
        set
        {
            if (_density == value) return;
            _density = value;
            Rebuild();
        }
    }

    public static MavMotionScheme Motion
    {
        get => _motion;
        set
        {
            if (_motion == value) return;
            _motion = value;
            Rebuild();
        }
    }

    public static MavShapeScale ShapeScaleMode
    {
        get => _shapeScale;
        set
        {
            if (_shapeScale == value) return;
            _shapeScale = value;
            Rebuild();
        }
    }

    public static void UseLight() => Mode = MavThemeMode.Light;
    public static void UseDark() => Mode = MavThemeMode.Dark;
    public static void UseSystemMode() => Mode = MavThemeMode.System;
    public static void ToggleDark() => Mode = Current.IsDark ? MavThemeMode.Light : MavThemeMode.Dark;

    /// <summary>
    /// Extracts a Material You seed color from ARGB image pixels and applies it to the active theme.
    /// Returns the extracted seed color.
    /// </summary>
    public static Color ApplySeedFromPixels(ReadOnlySpan<uint> argbPixels, Color? fallback = null)
    {
        var extracted = MavColorScheme.ExtractSeedFromPixels(argbPixels, fallback);
        SeedColor = extracted;
        return extracted;
    }

    /// <summary>
    /// Extracts a Material You seed color from RGBA byte data and applies it to the active theme.
    /// Returns the extracted seed color.
    /// </summary>
    public static Color ApplySeedFromRgba(ReadOnlySpan<byte> rgbaBytes, Color? fallback = null)
    {
        var extracted = MavColorScheme.ExtractSeedFromRgba(rgbaBytes, fallback);
        SeedColor = extracted;
        return extracted;
    }

    public static void ApplyCustomSchemes(MavColorScheme light, MavColorScheme dark)
    {
        _customLight = light;
        _customDark = dark;
        Rebuild();
    }

    public static void ClearCustomSchemes()
    {
        _customLight = null;
        _customDark = null;
        Rebuild();
    }

    public static void Configure(
        Color? seed = null,
        MavThemeMode? mode = null,
        MavContrast? contrast = null,
        MavDensity? density = null,
        bool? useSystemAccentColor = null,
        MavSchemeVariant? schemeVariant = null,
        MavMotionScheme? motion = null,
        MavShapeScale? shapeScale = null)
    {
        if (seed is not null)
        {
            _seed = seed.Value;
            _useSystemAccent = false;
            _customLight = null;
            _customDark = null;
        }
        if (useSystemAccentColor is not null) _useSystemAccent = useSystemAccentColor.Value;
        if (mode is not null) _mode = mode.Value;
        if (schemeVariant is not null) _schemeVariant = schemeVariant.Value;
        if (contrast is not null) _contrast = contrast.Value;
        if (density is not null) _density = density.Value;
        if (motion is not null) _motion = motion.Value;
        if (shapeScale is not null) _shapeScale = shapeScale.Value;
        Rebuild();
    }

    public static void Configure(MavThemeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _seed = options.SeedColor;
        _useSystemAccent = options.UseSystemAccentColor;
        _mode = options.Mode;
        _schemeVariant = options.SchemeVariant;
        _contrast = options.Contrast;
        _density = options.Density;
        _motion = options.Motion;
        _shapeScale = options.ShapeScale;
        _customLight = options.CustomLightScheme;
        _customDark = options.CustomDarkScheme;
        Rebuild();
    }

    public static void Configure(Action<MavThemeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var opts = new MavThemeOptions
        {
            SeedColor = _seed,
            UseSystemAccentColor = _useSystemAccent,
            Mode = _mode,
            SchemeVariant = _schemeVariant,
            Contrast = _contrast,
            Density = _density,
            Motion = _motion,
            ShapeScale = _shapeScale,
            CustomLightScheme = _customLight,
            CustomDarkScheme = _customDark,
        };
        configure(opts);
        Configure(opts);
    }

    /// <summary>
    /// Attaches a theme observer to <paramref name="view"/> that runs immediately
    /// and re-runs whenever the theme changes while the view is attached to the visual tree.
    /// </summary>
    public static void Observe<T>(T view, Action<T, MavTheme> apply) where T : Control
    {
        var appliedVersion = -1;
        var subscribed = false;

        // The static Changed event must not root the view if its detach event is
        // missed (for example while a window is being torn down).
        var weakView = new WeakReference<T>(view);

        void Invoke()
        {
            if (!weakView.TryGetTarget(out var target))
                return;

            appliedVersion = _version;
            apply(target, Current);
        }

        void OnChanged(object? _, MavTheme theme)
        {
            if (!weakView.TryGetTarget(out var target))
                return;

            appliedVersion = _version;
            apply(target, theme);
        }

        Invoke();

        view.AttachedToVisualTree += (_, _) =>
        {
            if (!subscribed)
            {
                Changed += OnChanged;
                subscribed = true;
            }
            if (appliedVersion != _version)
                Invoke();
        };

        view.DetachedFromVisualTree += (_, _) =>
        {
            if (subscribed)
            {
                Changed -= OnChanged;
                subscribed = false;
            }
        };

        if (view.IsLoaded || TopLevel.GetTopLevel(view) is not null)
        {
            Changed += OnChanged;
            subscribed = true;
        }
    }

    public double DensityOffset => (int)ScaleDensity * 4.0;

    private static void EnsureAppHook()
    {
        if (_hookedApp || Application.Current is not { } app) return;
        _hookedApp = true;
        if (app.PlatformSettings is { } platformSettings)
        {
            platformSettings.ColorValuesChanged += (_, _) =>
            {
                if (_mode == MavThemeMode.System || _useSystemAccent)
                    Rebuild();
            };
        }
        SyncAppResources(_current ?? Build());
    }

    private static void Rebuild()
    {
        _version++;
        _current = Build();
        SyncAppResources(_current);
        Changed?.Invoke(null, _current);
    }

    private static MavTheme Build()
    {
        var colorValues = Application.Current?.PlatformSettings?.GetColorValues();
        bool dark = _mode switch
        {
            MavThemeMode.Dark => true,
            MavThemeMode.Light => false,
            _ => colorValues?.ThemeVariant == PlatformThemeVariant.Dark,
        };

        var effectiveSeed = _seed;
        if (_useSystemAccent && colorValues is { } cv && cv.AccentColor1.A > 0)
            effectiveSeed = cv.AccentColor1;

        var scheme = dark
            ? (_customDark ?? MavColorScheme.FromSeed(effectiveSeed, true, _contrast, _schemeVariant))
            : (_customLight ?? MavColorScheme.FromSeed(effectiveSeed, false, _contrast, _schemeVariant));

        return new MavTheme(scheme, _typography, _density, _contrast, _mode, _schemeVariant, _motion, _shapeScale);
    }

    private static void SyncAppResources(MavTheme theme)
    {
        if (Application.Current is not { } app) return;
        var c = theme.Color;
        var targetVariant = theme.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
        if (app.RequestedThemeVariant != targetVariant)
            app.RequestedThemeVariant = targetVariant;

        app.Resources["NavigationBarBackground"] = BrushOf(c.Surface);
        app.Resources["NavigationBarForeground"] = BrushOf(c.OnSurface);
        app.Resources["SystemRegionBrush"] = BrushOf(c.Background);
        app.Resources["TextControlSelectionHighlightColor"] = BrushOf(c.Primary.WithAlpha(0.28f));

        foreach (var role in Enum.GetValues<MavColorRole>())
        {
            var col = c.Get(role);
            app.Resources[$"Md.Color.{role}"] = col;
            app.Resources[$"Md.Brush.{role}"] = BrushOf(col);
        }

        app.Resources["Md.Theme.IsDark"] = theme.IsDark;
        app.Resources["Md.Shape.None"] = theme.Shape.None;
        app.Resources["Md.Shape.ExtraSmall"] = theme.Shape.ExtraSmall;
        app.Resources["Md.Shape.Small"] = theme.Shape.Small;
        app.Resources["Md.Shape.Medium"] = theme.Shape.Medium;
        app.Resources["Md.Shape.Large"] = theme.Shape.Large;
        app.Resources["Md.Shape.LargeIncreased"] = theme.Shape.LargeIncreased;
        app.Resources["Md.Shape.ExtraLarge"] = theme.Shape.ExtraLarge;
        app.Resources["Md.Shape.Full"] = theme.Shape.Full;

        MavSystemBars.Apply();
    }
}

/// <summary>
/// Declarative Material Design 3 theme <see cref="Style"/> for <c>App.axaml</c>
/// (<c>&lt;mav:MavThemeStyle SeedColor="#6750A4" Mode="System" Contrast="Standard" /&gt;</c>).
/// </summary>
public class MavThemeStyle : Style
{
    public static readonly StyledProperty<Color> SeedColorProperty =
        AvaloniaProperty.Register<MavThemeStyle, Color>(nameof(SeedColor), MavColors.Purple);

    public static readonly StyledProperty<MavThemeMode> ModeProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavThemeMode>(nameof(Mode), MavThemeMode.System);

    public static readonly StyledProperty<MavSchemeVariant> VariantProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavSchemeVariant>(nameof(Variant), MavSchemeVariant.TonalSpot);

    public static readonly StyledProperty<MavContrast> ContrastProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavContrast>(nameof(Contrast), MavContrast.Standard);

    public static readonly StyledProperty<MavDensity> DensityProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavDensity>(nameof(Density), MavDensity.Comfortable);

    public static readonly StyledProperty<MavMotionScheme> MotionProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavMotionScheme>(nameof(Motion), MavMotionScheme.Expressive);

    public static readonly StyledProperty<MavShapeScale> ShapeScaleProperty =
        AvaloniaProperty.Register<MavThemeStyle, MavShapeScale>(nameof(ShapeScale), MavShapeScale.Standard);

    public static readonly StyledProperty<bool> UseDynamicColorProperty =
        AvaloniaProperty.Register<MavThemeStyle, bool>(nameof(UseDynamicColor));

    static MavThemeStyle()
    {
        SeedColorProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.SeedColor = e.GetNewValue<Color>());
        ModeProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Mode = e.GetNewValue<MavThemeMode>());
        VariantProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Variant = e.GetNewValue<MavSchemeVariant>());
        ContrastProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Contrast = e.GetNewValue<MavContrast>());
        DensityProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Density = e.GetNewValue<MavDensity>());
        MotionProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.Motion = e.GetNewValue<MavMotionScheme>());
        ShapeScaleProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.ShapeScaleMode = e.GetNewValue<MavShapeScale>());
        UseDynamicColorProperty.Changed.AddClassHandler<MavThemeStyle>((_, e) => MavTheme.UseSystemAccentColor = e.GetNewValue<bool>());
    }

    public MavThemeStyle()
    {
        _ = MavTheme.Current;
    }

    public Color SeedColor
    {
        get => GetValue(SeedColorProperty);
        set => SetValue(SeedColorProperty, value);
    }

    public MavThemeMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public MavSchemeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MavContrast Contrast
    {
        get => GetValue(ContrastProperty);
        set => SetValue(ContrastProperty, value);
    }

    public MavDensity Density
    {
        get => GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    public MavMotionScheme Motion
    {
        get => GetValue(MotionProperty);
        set => SetValue(MotionProperty, value);
    }

    public MavShapeScale ShapeScale
    {
        get => GetValue(ShapeScaleProperty);
        set => SetValue(ShapeScaleProperty, value);
    }

    public bool UseDynamicColor
    {
        get => GetValue(UseDynamicColorProperty);
        set => SetValue(UseDynamicColorProperty, value);
    }
}
