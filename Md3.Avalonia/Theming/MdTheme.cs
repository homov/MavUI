using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Styling;
using Md3.Avalonia.Platform;

namespace Md3.Avalonia.Theming;

public enum MdThemeMode
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
public sealed class MdTheme
{
    private const int CacheLimit = 2048;
    private static readonly MdLruCache<uint, IImmutableSolidColorBrush> BrushCache = new(CacheLimit);
    private static readonly MdLruCache<ulong, IPen> PenCache = new(CacheLimit);
    private static readonly ImmutableSolidColorBrush TransparentBrush = new(Colors.Transparent);

    private static Color _seed = MdColors.Purple;
    private static bool _useSystemAccent;
    private static MdThemeMode _mode = MdThemeMode.System;
    private static MdSchemeVariant _schemeVariant = MdSchemeVariant.TonalSpot;
    private static MdContrast _contrast = MdContrast.Standard;
    private static MdDensity _density = MdDensity.Comfortable;
    private static MdMotionScheme _motion = MdMotionScheme.Expressive;
    private static MdShapeScale _shapeScale = MdShapeScale.Standard;
    private static MdTypography _typography = MdTypography.Default;
    private static MdColorScheme? _customLight;
    private static MdColorScheme? _customDark;
    private static MdTheme? _current;
    private static bool _hookedApp;
    private static int _version;

    public static event EventHandler<MdTheme>? Changed;

    public MdColorScheme Color { get; }
    public MdTypography Typography { get; }
    public MdShapeTokens Shape { get; }
    public MdElevationTokens Elevation { get; } = MdElevationTokens.Default;
    public MdStateLayerTokens StateLayer { get; } = MdStateLayerTokens.Default;
    public MdDensity ScaleDensity { get; }
    public MdContrast ScaleContrast { get; }
    public MdSchemeVariant SchemeVariant { get; }
    public MdMotionScheme MotionScheme { get; }
    public MdShapeScale ShapeScale { get; }
    public MdThemeMode ThemeMode { get; }
    public bool IsDark => Color.IsDark;

    private MdTheme(
        MdColorScheme scheme,
        MdTypography typography,
        MdDensity density,
        MdContrast contrast,
        MdThemeMode mode,
        MdSchemeVariant schemeVariant,
        MdMotionScheme motion,
        MdShapeScale shapeScale)
    {
        Color = scheme;
        Typography = typography;
        Shape = new MdShapeTokens(shapeScale);
        ScaleDensity = density;
        ScaleContrast = contrast;
        ThemeMode = mode;
        SchemeVariant = schemeVariant;
        MotionScheme = motion;
        ShapeScale = shapeScale;
    }

    public static Color ColorOf(MdColorRole role) => Current.Color.Get(role);

    public static IImmutableSolidColorBrush BrushOf(MdColorRole role) => BrushOf(Current.Color.Get(role));

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

    public static MdTheme Current
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

    public static MdThemeMode Mode
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
                    MdThemeMode.Light => ThemeVariant.Light,
                    MdThemeMode.Dark => ThemeVariant.Dark,
                    _ => ThemeVariant.Default,
                };
            }
            Rebuild();
        }
    }

    public static MdSchemeVariant Variant
    {
        get => _schemeVariant;
        set
        {
            if (_schemeVariant == value) return;
            _schemeVariant = value;
            Rebuild();
        }
    }

    public static MdContrast Contrast
    {
        get => _contrast;
        set
        {
            if (_contrast == value) return;
            _contrast = value;
            Rebuild();
        }
    }

    public static MdDensity Density
    {
        get => _density;
        set
        {
            if (_density == value) return;
            _density = value;
            Rebuild();
        }
    }

    public static MdMotionScheme Motion
    {
        get => _motion;
        set
        {
            if (_motion == value) return;
            _motion = value;
            Rebuild();
        }
    }

    public static MdShapeScale ShapeScaleMode
    {
        get => _shapeScale;
        set
        {
            if (_shapeScale == value) return;
            _shapeScale = value;
            Rebuild();
        }
    }

    public static void UseLight() => Mode = MdThemeMode.Light;
    public static void UseDark() => Mode = MdThemeMode.Dark;
    public static void UseSystemMode() => Mode = MdThemeMode.System;
    public static void ToggleDark() => Mode = Current.IsDark ? MdThemeMode.Light : MdThemeMode.Dark;

    /// <summary>
    /// Extracts a Material You seed color from ARGB image pixels and applies it to the active theme.
    /// Returns the extracted seed color.
    /// </summary>
    public static Color ApplySeedFromPixels(ReadOnlySpan<uint> argbPixels, Color? fallback = null)
    {
        var extracted = MdColorScheme.ExtractSeedFromPixels(argbPixels, fallback);
        SeedColor = extracted;
        return extracted;
    }

    /// <summary>
    /// Extracts a Material You seed color from RGBA byte data and applies it to the active theme.
    /// Returns the extracted seed color.
    /// </summary>
    public static Color ApplySeedFromRgba(ReadOnlySpan<byte> rgbaBytes, Color? fallback = null)
    {
        var extracted = MdColorScheme.ExtractSeedFromRgba(rgbaBytes, fallback);
        SeedColor = extracted;
        return extracted;
    }

    public static void ApplyCustomSchemes(MdColorScheme light, MdColorScheme dark)
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
        MdThemeMode? mode = null,
        MdContrast? contrast = null,
        MdDensity? density = null,
        bool? useSystemAccentColor = null,
        MdSchemeVariant? schemeVariant = null,
        MdMotionScheme? motion = null,
        MdShapeScale? shapeScale = null)
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

    public static void Configure(MdThemeOptions options)
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

    public static void Configure(Action<MdThemeOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var opts = new MdThemeOptions
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
    public static void Observe<T>(T view, Action<T, MdTheme> apply) where T : Control
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

        void OnChanged(object? _, MdTheme theme)
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
                if (_mode == MdThemeMode.System || _useSystemAccent)
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

    private static MdTheme Build()
    {
        var colorValues = Application.Current?.PlatformSettings?.GetColorValues();
        bool dark = _mode switch
        {
            MdThemeMode.Dark => true,
            MdThemeMode.Light => false,
            _ => colorValues?.ThemeVariant == PlatformThemeVariant.Dark,
        };

        var effectiveSeed = _seed;
        if (_useSystemAccent && colorValues is { } cv && cv.AccentColor1.A > 0)
            effectiveSeed = cv.AccentColor1;

        var scheme = dark
            ? (_customDark ?? MdColorScheme.FromSeed(effectiveSeed, true, _contrast, _schemeVariant))
            : (_customLight ?? MdColorScheme.FromSeed(effectiveSeed, false, _contrast, _schemeVariant));

        return new MdTheme(scheme, _typography, _density, _contrast, _mode, _schemeVariant, _motion, _shapeScale);
    }

    private static void SyncAppResources(MdTheme theme)
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

        foreach (var role in Enum.GetValues<MdColorRole>())
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

        MdSystemBars.Apply();
    }
}

/// <summary>
/// Declarative Material Design 3 theme <see cref="Style"/> for <c>App.axaml</c>
/// (<c>&lt;md3:Md3Theme SeedColor="#6750A4" Mode="System" Contrast="Standard" /&gt;</c>).
/// </summary>
public class Md3Theme : Style
{
    public static readonly StyledProperty<Color> SeedColorProperty =
        AvaloniaProperty.Register<Md3Theme, Color>(nameof(SeedColor), MdColors.Purple);

    public static readonly StyledProperty<MdThemeMode> ModeProperty =
        AvaloniaProperty.Register<Md3Theme, MdThemeMode>(nameof(Mode), MdThemeMode.System);

    public static readonly StyledProperty<MdSchemeVariant> VariantProperty =
        AvaloniaProperty.Register<Md3Theme, MdSchemeVariant>(nameof(Variant), MdSchemeVariant.TonalSpot);

    public static readonly StyledProperty<MdContrast> ContrastProperty =
        AvaloniaProperty.Register<Md3Theme, MdContrast>(nameof(Contrast), MdContrast.Standard);

    public static readonly StyledProperty<MdDensity> DensityProperty =
        AvaloniaProperty.Register<Md3Theme, MdDensity>(nameof(Density), MdDensity.Comfortable);

    public static readonly StyledProperty<MdMotionScheme> MotionProperty =
        AvaloniaProperty.Register<Md3Theme, MdMotionScheme>(nameof(Motion), MdMotionScheme.Expressive);

    public static readonly StyledProperty<MdShapeScale> ShapeScaleProperty =
        AvaloniaProperty.Register<Md3Theme, MdShapeScale>(nameof(ShapeScale), MdShapeScale.Standard);

    public static readonly StyledProperty<bool> UseDynamicColorProperty =
        AvaloniaProperty.Register<Md3Theme, bool>(nameof(UseDynamicColor));

    static Md3Theme()
    {
        SeedColorProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.SeedColor = e.GetNewValue<Color>());
        ModeProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.Mode = e.GetNewValue<MdThemeMode>());
        VariantProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.Variant = e.GetNewValue<MdSchemeVariant>());
        ContrastProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.Contrast = e.GetNewValue<MdContrast>());
        DensityProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.Density = e.GetNewValue<MdDensity>());
        MotionProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.Motion = e.GetNewValue<MdMotionScheme>());
        ShapeScaleProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.ShapeScaleMode = e.GetNewValue<MdShapeScale>());
        UseDynamicColorProperty.Changed.AddClassHandler<Md3Theme>((_, e) => MdTheme.UseSystemAccentColor = e.GetNewValue<bool>());
    }

    public Md3Theme()
    {
        _ = MdTheme.Current;
    }

    public Color SeedColor
    {
        get => GetValue(SeedColorProperty);
        set => SetValue(SeedColorProperty, value);
    }

    public MdThemeMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public MdSchemeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MdContrast Contrast
    {
        get => GetValue(ContrastProperty);
        set => SetValue(ContrastProperty, value);
    }

    public MdDensity Density
    {
        get => GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    public MdMotionScheme Motion
    {
        get => GetValue(MotionProperty);
        set => SetValue(MotionProperty, value);
    }

    public MdShapeScale ShapeScale
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
