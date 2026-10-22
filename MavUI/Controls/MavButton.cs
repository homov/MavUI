using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

public enum MavButtonVariant
{
    Filled,
    Tonal,
    Elevated,
    Outlined,
    Text,
}

public enum MavButtonSize
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge,
}

public enum MavButtonShape
{
    Round,
    Square,
}

/// <summary>
/// Material Design 3 button with 5 variants, 5 Expressive sizes, round/square shapes,
/// optional toggle mode, and leading/trailing icons.
/// </summary>
public class MavButton : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavButton, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavButton, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> TrailingIconProperty =
        AvaloniaProperty.Register<MavButton, string?>(nameof(TrailingIcon));

    public static readonly StyledProperty<MavButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MavButton, MavButtonVariant>(nameof(Variant), MavButtonVariant.Filled);

    public static readonly StyledProperty<MavButtonSize> SizeProperty =
        AvaloniaProperty.Register<MavButton, MavButtonSize>(nameof(Size), MavButtonSize.Small);

    public static readonly StyledProperty<MavButtonShape> ButtonShapeProperty =
        AvaloniaProperty.Register<MavButton, MavButtonShape>(nameof(ButtonShape), MavButtonShape.Round);

    public static readonly StyledProperty<bool> IsToggleProperty =
        AvaloniaProperty.Register<MavButton, bool>(nameof(IsToggle));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavButton, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<MavButton, bool>(nameof(IsLoading));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavButton, object?>(nameof(CommandParameter));

    private readonly MavInkSurface _surface;
    private readonly StackPanel _row;
    private readonly MavCircularProgress _spinner;
    private readonly MavIcon _leadingIcon;
    private readonly TextBlock _label;
    private readonly MavIcon _trailingIcon;
    private CornerRadius? _customCornerRadii;
    private double _shapeProgress;
    private bool _pressed;

    public event EventHandler? Clicked;
    public event EventHandler<bool>? SelectedChanged;
    public event EventHandler<bool>? Toggled;

    static MavButton()
    {
        TextProperty.Changed.AddClassHandler<MavButton>((b, _) =>
        {
            b.SyncContent();
            b.SyncAutomation();
        });
        IconProperty.Changed.AddClassHandler<MavButton>((b, _) => b.SyncContent());
        TrailingIconProperty.Changed.AddClassHandler<MavButton>((b, _) => b.SyncContent());
        VariantProperty.Changed.AddClassHandler<MavButton>((b, _) => b.ApplyTheme(MavTheme.Current));
        SizeProperty.Changed.AddClassHandler<MavButton>((b, _) => b.ApplyTheme(MavTheme.Current));
        ButtonShapeProperty.Changed.AddClassHandler<MavButton>((b, _) => b.ApplyTheme(MavTheme.Current));
        IsToggleProperty.Changed.AddClassHandler<MavButton>((b, _) => b.ApplyTheme(MavTheme.Current));
        IsSelectedProperty.Changed.AddClassHandler<MavButton>((b, e) =>
        {
            b.ApplyTheme(MavTheme.Current);
            var val = e.GetNewValue<bool>();
            b.SelectedChanged?.Invoke(b, val);
            b.Toggled?.Invoke(b, val);
        });
        IsLoadingProperty.Changed.AddClassHandler<MavButton>((b, _) =>
        {
            b.SyncContent();
            b.ApplyTheme(MavTheme.Current);
        });
        CommandProperty.Changed.AddClassHandler<MavButton>((b, e) => b._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavButton>((b, e) => b._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MavButton() : this(string.Empty)
    {
    }

    public MavButton(
        string text = "",
        MavButtonVariant variant = MavButtonVariant.Filled,
        string? icon = null,
        MavButtonSize size = MavButtonSize.Small)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _spinner = new MavCircularProgress(18, 2.2)
        {
            IsIndeterminate = true,
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _leadingIcon = new MavIcon { IsVisible = false, UseThemeDefaultColor = false };
        _trailingIcon = new MavIcon { IsVisible = false, UseThemeDefaultColor = false };
        _label = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };

        _row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _row.Children.Add(_spinner);
        _row.Children.Add(_leadingIcon);
        _row.Children.Add(_label);
        _row.Children.Add(_trailingIcon);

        _surface = new MavInkSurface { Content = _row };
        _surface.Clicked += OnSurfaceClicked;
        _surface.Pressed += (_, _) => SetPressedMorph(true);
        _surface.Released += (_, _) => SetPressedMorph(false);
        _surface.Cancelled += (_, _) => SetPressedMorph(false);

        Content = _surface;

        Variant = variant;
        Size = size;
        Text = text;
        Icon = icon;

        SyncContent();
        SyncAutomation();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MavAutomation.SetName(this, Text);
        MavAutomation.SetControlType(this, AutomationControlType.Button);
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? TrailingIcon
    {
        get => GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    public MavButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MavButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public MavButtonShape ButtonShape
    {
        get => GetValue(ButtonShapeProperty);
        set => SetValue(ButtonShapeProperty, value);
    }

    public MavButtonShape Shape
    {
        get => ButtonShape;
        set => ButtonShape = value;
    }

    public bool IsToggle
    {
        get => GetValue(IsToggleProperty);
        set => SetValue(IsToggleProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    internal void SetGroupCornerRadii(CornerRadius? radii)
    {
        _customCornerRadii = radii;
        ApplyShape(MavTheme.Current);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            _surface.IsEnabled = IsEnabled;
            ApplyTheme(MavTheme.Current);
        }
    }

    private void OnSurfaceClicked(object? sender, EventArgs e)
    {
        if (IsLoading)
            return;

        if (IsToggle)
            IsSelected = !IsSelected;

        Clicked?.Invoke(this, EventArgs.Empty);
    }

    private void SetPressedMorph(bool pressed)
    {
        if (_pressed == pressed) return;
        _pressed = pressed;
        var target = pressed ? 1.0 : 0.0;
        MavMotion.Animate(this, "btn_morph", _shapeProgress, target, MavMotion.Short3, MavMotion.CubicOut, v =>
        {
            _shapeProgress = v;
            ApplyShape(MavTheme.Current);
        });
    }

    private void SyncContent()
    {
        _label.Text = Text;
        _label.IsVisible = !string.IsNullOrEmpty(Text);

        var loading = IsLoading;
        _spinner.IsVisible = loading;

        var hasLead = !loading && !string.IsNullOrEmpty(Icon);
        _leadingIcon.Glyph = Icon;
        _leadingIcon.IsVisible = hasLead;

        var hasTrail = !string.IsNullOrEmpty(TrailingIcon);
        _trailingIcon.Glyph = TrailingIcon;
        _trailingIcon.IsVisible = hasTrail;

        _surface.IsInteractive = !loading;
        ApplySizeTokens(MavTheme.Current);
    }

    private void ApplySizeTokens(MavTheme t)
    {
        var (height, padH, iconSize, role, gap) = Size switch
        {
            MavButtonSize.ExtraSmall => (32.0, 12.0, 16.0, MavTypeRole.LabelMedium, 6.0),
            MavButtonSize.Small => (40.0, 20.0, 18.0, MavTypeRole.LabelLarge, 8.0),
            MavButtonSize.Medium => (48.0, 24.0, 20.0, MavTypeRole.TitleSmall, 8.0),
            MavButtonSize.Large => (56.0, 28.0, 24.0, MavTypeRole.TitleMedium, 10.0),
            MavButtonSize.ExtraLarge => (64.0, 32.0, 26.0, MavTypeRole.TitleLarge, 12.0),
            _ => (40.0, 20.0, 18.0, MavTypeRole.LabelLarge, 8.0),
        };

        var densityDelta = t.DensityOffset;
        var finalHeight = Math.Max(28, height + densityDelta);

        var hasLead = IsLoading || !string.IsNullOrEmpty(Icon);
        var hasTrail = !string.IsNullOrEmpty(TrailingIcon);
        var leftPad = Variant == MavButtonVariant.Text ? 12.0 : (hasLead ? Math.Max(12, padH - 4) : padH);
        var rightPad = Variant == MavButtonVariant.Text ? 12.0 : (hasTrail ? Math.Max(12, padH - 4) : padH);

        _surface.MinHeight = finalHeight;
        _surface.MinWidth = 48;
        _surface.Padding = new Thickness(leftPad, 0, rightPad, 0);

        _row.Spacing = gap;
        _spinner.Diameter = iconSize;
        _leadingIcon.Size = iconSize;
        _trailingIcon.Size = iconSize;
        _label.Apply(role, t.Typography);

        ApplyShape(t);
    }

    private void ApplyShape(MavTheme t)
    {
        if (_customCornerRadii is { } custom)
        {
            _surface.CornerRadii = custom;
            return;
        }

        var (height, squareR) = Size switch
        {
            MavButtonSize.ExtraSmall => (32.0, 8.0),
            MavButtonSize.Small => (40.0, 10.0),
            MavButtonSize.Medium => (48.0, 12.0),
            MavButtonSize.Large => (56.0, 16.0),
            MavButtonSize.ExtraLarge => (64.0, 20.0),
            _ => (40.0, 10.0),
        };
        var roundR = (height + t.DensityOffset) / 2.0;

        double baseR = ButtonShape == MavButtonShape.Round ? roundR : squareR;
        if (IsToggle && IsSelected)
            baseR = ButtonShape == MavButtonShape.Round ? squareR : roundR;

        var pressedR = Math.Max(6, squareR - 2);
        var r = baseR + (pressedR - baseR) * _shapeProgress;
        _surface.CornerRadius = r;
    }

    private void ApplyTheme(MavTheme t)
    {
        ApplySizeTokens(t);

        var c = t.Color;
        Color bg;
        Color fg;
        Color stroke = Colors.Transparent;
        double strokeW = 0;
        double elev = 0;

        if (!IsEnabled)
        {
            var disContainer = c.OnSurface.WithAlpha(t.StateLayer.DisabledContainer);
            var disContent = c.OnSurface.WithAlpha(t.StateLayer.DisabledContent);

            bg = Variant is MavButtonVariant.Outlined or MavButtonVariant.Text ? Colors.Transparent : disContainer;
            fg = disContent;
            if (Variant == MavButtonVariant.Outlined)
            {
                stroke = disContainer;
                strokeW = 1;
            }
        }
        else if (IsToggle)
        {
            if (IsSelected)
            {
                (bg, fg, stroke, strokeW, elev) = Variant switch
                {
                    MavButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
                    MavButtonVariant.Tonal => (c.Secondary, c.OnSecondary, Colors.Transparent, 0, 0),
                    MavButtonVariant.Elevated => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 2),
                    MavButtonVariant.Outlined => (c.InverseSurface, c.InverseOnSurface, Colors.Transparent, 0, 0),
                    MavButtonVariant.Text => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0, 0),
                    _ => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
                };
            }
            else
            {
                (bg, fg, stroke, strokeW, elev) = Variant switch
                {
                    MavButtonVariant.Filled => (c.SurfaceContainerHighest, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                    MavButtonVariant.Tonal => (c.SurfaceContainerHigh, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                    MavButtonVariant.Elevated => (c.SurfaceContainerLow, c.Primary, Colors.Transparent, 0, 1),
                    MavButtonVariant.Outlined => (Colors.Transparent, c.OnSurfaceVariant, c.Outline, 1, 0),
                    MavButtonVariant.Text => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                    _ => (c.SurfaceContainerHighest, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                };
            }
        }
        else
        {
            (bg, fg, stroke, strokeW, elev) = Variant switch
            {
                MavButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
                MavButtonVariant.Tonal => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0, 0),
                MavButtonVariant.Elevated => (c.SurfaceContainerLow, c.Primary, Colors.Transparent, 0, 1),
                MavButtonVariant.Outlined => (Colors.Transparent, c.Primary, c.Outline, 1, 0),
                MavButtonVariant.Text => (Colors.Transparent, c.Primary, Colors.Transparent, 0, 0),
                _ => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
            };
        }

        _surface.FillColor = bg;
        _surface.StrokeColor = stroke;
        _surface.StrokeThickness = strokeW;
        _surface.RippleColor = fg;
        _surface.Elevation = elev;
        _label.Foreground = MavTheme.BrushOf(fg);
        _spinner.CustomActiveColor = fg;
        _leadingIcon.Color = fg;
        _trailingIcon.Color = fg;
    }
}

public enum MavIconButtonVariant
{
    Standard,
    Filled,
    Tonal,
    Outlined,
}

/// <summary>
/// Material Design 3 icon button with Standard, Filled, Tonal, and Outlined styles and toggle state.
/// </summary>
public class MavIconButton : MavContentView
{
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavIconButton, string>(nameof(Icon), MavIcons.Favorite);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MavIconButton, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<MavIconButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MavIconButton, MavIconButtonVariant>(nameof(Variant), MavIconButtonVariant.Standard);

    public static readonly StyledProperty<bool> IsToggleProperty =
        AvaloniaProperty.Register<MavIconButton, bool>(nameof(IsToggle));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavIconButton, bool>(nameof(IsSelected));

    public static readonly StyledProperty<double> ButtonSizeProperty =
        AvaloniaProperty.Register<MavIconButton, double>(nameof(ButtonSize), 40.0);

    /// <summary>
    /// Minimum hit area of the button. The visual state layer keeps
    /// <see cref="ButtonSize"/>; the extra space only widens the touch target
    /// (Material Design 3 recommends at least 48dp).
    /// </summary>
    public static readonly StyledProperty<double> TouchTargetSizeProperty =
        AvaloniaProperty.Register<MavIconButton, double>(nameof(TouchTargetSize), 48.0);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavIconButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavIconButton, object?>(nameof(CommandParameter));

    private readonly MavInkSurface _surface;
    private readonly MavIcon _icon;

    public event EventHandler? Clicked;
    public event EventHandler<bool>? SelectedChanged;

    static MavIconButton()
    {
        IconProperty.Changed.AddClassHandler<MavIconButton>((b, _) => b.SyncGlyph());
        SelectedIconProperty.Changed.AddClassHandler<MavIconButton>((b, _) => b.SyncGlyph());
        VariantProperty.Changed.AddClassHandler<MavIconButton>((b, _) => b.ApplyTheme(MavTheme.Current));
        IsToggleProperty.Changed.AddClassHandler<MavIconButton>((b, _) => b.ApplyTheme(MavTheme.Current));
        IsSelectedProperty.Changed.AddClassHandler<MavIconButton>((b, e) =>
        {
            b.SyncGlyph();
            b.ApplyTheme(MavTheme.Current);
            b.SelectedChanged?.Invoke(b, e.GetNewValue<bool>());
        });
        ButtonSizeProperty.Changed.AddClassHandler<MavIconButton>((b, _) => b.ApplySize());
        TouchTargetSizeProperty.Changed.AddClassHandler<MavIconButton>((b, _) => b.ApplySize());
        CommandProperty.Changed.AddClassHandler<MavIconButton>((b, e) => b._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavIconButton>((b, e) => b._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MavIconButton() : this(MavIcons.Favorite)
    {
    }

    public MavIconButton(
        string icon = MavIcons.Favorite,
        MavIconButtonVariant variant = MavIconButtonVariant.Standard,
        bool isToggle = false)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);

        _icon = new MavIcon(icon, 22) { UseThemeDefaultColor = false };
        _surface = new MavInkSurface
        {
            Width = 40,
            Height = 40,
            CornerRadius = 20,
            Content = _icon,
        };
        _surface.Clicked += (_, _) => RaiseClick();

        // Clicks that land in the touch-target padding around the 40dp state
        // layer still activate the button.
        PointerReleased += (_, e) =>
        {
            if (!e.Handled && IsEnabled)
                RaiseClick();
        };

        Content = _surface;
        Icon = icon;
        Variant = variant;
        IsToggle = isToggle;

        // Icon-only buttons have no visible text: use the tooltip (when present) as
        // the accessible name, otherwise leave the automation name to the application.
        PropertyChanged += (_, e) =>
        {
            if (e.Property == ToolTip.TipProperty)
                SyncAutomation();
        };
        SyncAutomation();

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MavAutomation.SetControlType(this, AutomationControlType.Button);
        MavAutomation.SetName(this, ToolTip.GetTip(this) as string);
    }

    [Content]
    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? SelectedIcon
    {
        get => GetValue(SelectedIconProperty);
        set => SetValue(SelectedIconProperty, value);
    }

    public MavIconButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsToggle
    {
        get => GetValue(IsToggleProperty);
        set => SetValue(IsToggleProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public double ButtonSize
    {
        get => GetValue(ButtonSizeProperty);
        set => SetValue(ButtonSizeProperty, value);
    }

    public double TouchTargetSize
    {
        get => GetValue(TouchTargetSizeProperty);
        set => SetValue(TouchTargetSizeProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            _surface.IsEnabled = IsEnabled;
            ApplyTheme(MavTheme.Current);
        }
    }

    private void SyncGlyph()
    {
        _icon.Glyph = IsToggle && IsSelected && !string.IsNullOrEmpty(SelectedIcon)
            ? SelectedIcon
            : Icon;
    }

    private void RaiseClick()
    {
        if (IsToggle)
            IsSelected = !IsSelected;
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    private void ApplySize()
    {
        var sz = Math.Max(28, ButtonSize);
        _surface.Width = sz;
        _surface.Height = sz;
        _surface.CornerRadius = sz / 2.0;
        _icon.Size = Math.Round(sz * 0.55);

        var target = Math.Max(sz, TouchTargetSize);
        MinWidth = target;
        MinHeight = target;
    }

    private void ApplyTheme(MavTheme t)
    {
        ApplySize();
        var c = t.Color;
        Color bg;
        Color fg;
        Color stroke = Colors.Transparent;
        double strokeW = 0;

        if (!IsEnabled)
        {
            var disContainer = c.OnSurface.WithAlpha(t.StateLayer.DisabledContainer);
            var disContent = c.OnSurface.WithAlpha(t.StateLayer.DisabledContent);
            bg = Variant is MavIconButtonVariant.Filled or MavIconButtonVariant.Tonal ? disContainer : Colors.Transparent;
            fg = disContent;
            if (Variant == MavIconButtonVariant.Outlined)
            {
                stroke = disContainer;
                strokeW = 1;
            }
        }
        else if (IsToggle)
        {
            (bg, fg, stroke, strokeW) = (Variant, IsSelected) switch
            {
                (MavIconButtonVariant.Standard, false) => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0.0),
                (MavIconButtonVariant.Standard, true) => (Colors.Transparent, c.Primary, Colors.Transparent, 0.0),
                (MavIconButtonVariant.Filled, false) => (c.SurfaceContainerHighest, c.Primary, Colors.Transparent, 0.0),
                (MavIconButtonVariant.Filled, true) => (c.Primary, c.OnPrimary, Colors.Transparent, 0.0),
                (MavIconButtonVariant.Tonal, false) => (c.SurfaceContainerHighest, c.OnSurfaceVariant, Colors.Transparent, 0.0),
                (MavIconButtonVariant.Tonal, true) => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0.0),
                (MavIconButtonVariant.Outlined, false) => (Colors.Transparent, c.OnSurfaceVariant, c.Outline, 1.0),
                (MavIconButtonVariant.Outlined, true) => (c.InverseSurface, c.InverseOnSurface, Colors.Transparent, 0.0),
                _ => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0.0),
            };
        }
        else
        {
            (bg, fg, stroke, strokeW) = Variant switch
            {
                MavIconButtonVariant.Standard => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0.0),
                MavIconButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0.0),
                MavIconButtonVariant.Tonal => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0.0),
                MavIconButtonVariant.Outlined => (Colors.Transparent, c.OnSurfaceVariant, c.Outline, 1.0),
                _ => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0.0),
            };
        }

        _surface.FillColor = bg;
        _surface.StrokeColor = stroke;
        _surface.StrokeThickness = strokeW;
        _surface.RippleColor = fg;
        _icon.Color = fg;
    }
}

public enum MavFabSize
{
    Small,
    Regular,
    Large,
}

public enum MavFabColor
{
    Primary,
    Secondary,
    Tertiary,
    Surface,
}

/// <summary>
/// Material Design 3 Floating Action Button (Small 40dp, Regular 56dp, Large 96dp).
/// </summary>
public class MavFab : MavContentView
{
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavFab, string>(nameof(Icon), MavIcons.Add);

    public static readonly StyledProperty<MavFabSize> SizeProperty =
        AvaloniaProperty.Register<MavFab, MavFabSize>(nameof(Size), MavFabSize.Regular);

    public static readonly StyledProperty<MavFabColor> ColorSchemeProperty =
        AvaloniaProperty.Register<MavFab, MavFabColor>(nameof(ColorScheme), MavFabColor.Primary);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavFab, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavFab, object?>(nameof(CommandParameter));

    private readonly MavInkSurface _surface;
    private readonly MavIcon _icon;

    public event EventHandler? Clicked;

    static MavFab()
    {
        IconProperty.Changed.AddClassHandler<MavFab>((f, e) => f._icon.Glyph = e.GetNewValue<string>());
        SizeProperty.Changed.AddClassHandler<MavFab>((f, _) => f.ApplyTheme(MavTheme.Current));
        ColorSchemeProperty.Changed.AddClassHandler<MavFab>((f, _) => f.ApplyTheme(MavTheme.Current));
        CommandProperty.Changed.AddClassHandler<MavFab>((f, e) => f._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavFab>((f, e) => f._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MavFab() : this(MavIcons.Add)
    {
    }

    public MavFab(
        string icon = MavIcons.Add,
        MavFabSize size = MavFabSize.Regular,
        MavFabColor color = MavFabColor.Primary)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _icon = new MavIcon(icon, 24) { UseThemeDefaultColor = false };
        _surface = new MavInkSurface
        {
            Elevation = 3,
            Content = _icon,
        };
        _surface.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);

        Content = _surface;
        Icon = icon;
        Size = size;
        ColorScheme = color;

        PropertyChanged += (_, e) =>
        {
            if (e.Property == ToolTip.TipProperty)
                SyncAutomation();
        };
        SyncAutomation();

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MavAutomation.SetControlType(this, AutomationControlType.Button);
        MavAutomation.SetName(this, ToolTip.GetTip(this) as string);
    }

    [Content]
    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public MavFabSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public MavFabColor ColorScheme
    {
        get => GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private void ApplyTheme(MavTheme t)
    {
        var (dim, radius, iconSz) = Size switch
        {
            MavFabSize.Small => (40.0, 12.0, 22.0),
            MavFabSize.Large => (96.0, 28.0, 36.0),
            _ => (56.0, 16.0, 24.0),
        };

        _surface.Width = dim;
        _surface.Height = dim;
        _surface.CornerRadius = radius;
        _icon.Size = iconSz;

        var c = t.Color;
        var (bg, fg) = ColorScheme switch
        {
            MavFabColor.Primary => (c.PrimaryContainer, c.OnPrimaryContainer),
            MavFabColor.Secondary => (c.SecondaryContainer, c.OnSecondaryContainer),
            MavFabColor.Tertiary => (c.TertiaryContainer, c.OnTertiaryContainer),
            MavFabColor.Surface => (c.SurfaceContainerHigh, c.Primary),
            _ => (c.PrimaryContainer, c.OnPrimaryContainer),
        };

        _surface.FillColor = bg;
        _surface.RippleColor = fg;
        _icon.Color = fg;
    }
}

/// <summary>
/// Material Design 3 Extended Floating Action Button with collapsible label.
/// </summary>
public class MavExtendedFab : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavExtendedFab, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavExtendedFab, string>(nameof(Icon), MavIcons.Edit);

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MavExtendedFab, bool>(nameof(IsExpanded), true);

    public static readonly StyledProperty<MavFabColor> ColorSchemeProperty =
        AvaloniaProperty.Register<MavExtendedFab, MavFabColor>(nameof(ColorScheme), MavFabColor.Primary);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavExtendedFab, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavExtendedFab, object?>(nameof(CommandParameter));

    private readonly MavInkSurface _surface;
    private readonly MavIcon _icon;
    private readonly TextBlock _label;
    private readonly Border _labelClip;

    public event EventHandler? Clicked;

    static MavExtendedFab()
    {
        TextProperty.Changed.AddClassHandler<MavExtendedFab>((f, e) =>
        {
            f._label.Text = e.GetNewValue<string>();
            f.SyncAutomation();
        });
        IconProperty.Changed.AddClassHandler<MavExtendedFab>((f, e) => f._icon.Glyph = e.GetNewValue<string>());
        IsExpandedProperty.Changed.AddClassHandler<MavExtendedFab>((f, e) => f.AnimateExpanded(e.GetNewValue<bool>()));
        ColorSchemeProperty.Changed.AddClassHandler<MavExtendedFab>((f, _) => f.ApplyTheme(MavTheme.Current));
        CommandProperty.Changed.AddClassHandler<MavExtendedFab>((f, e) => f._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavExtendedFab>((f, e) => f._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MavExtendedFab() : this(string.Empty)
    {
    }

    public MavExtendedFab(
        string text,
        string icon = MavIcons.Edit,
        MavFabColor color = MavFabColor.Primary)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _icon = new MavIcon(icon, 24) { UseThemeDefaultColor = false };
        _label = new TextBlock
        {
            Text = text,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };
        _labelClip = new Border
        {
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Center,
            Child = _label,
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        row.Children.Add(_icon);
        row.Children.Add(_labelClip);

        _surface = new MavInkSurface
        {
            Height = 56,
            MinWidth = 80,
            CornerRadius = 16,
            Elevation = 3,
            Padding = new Thickness(16, 0, 20, 0),
            Content = row,
        };
        _surface.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);

        Content = _surface;
        Text = text;
        Icon = icon;
        ColorScheme = color;
        SyncAutomation();

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MavAutomation.SetName(this, Text);
        MavAutomation.SetControlType(this, AutomationControlType.Button);
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public MavFabColor ColorScheme
    {
        get => GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private void AnimateExpanded(bool expanded)
    {
        _label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var targetWidth = Math.Max(40, _label.DesiredSize.Width + 12);
        var from = expanded ? 0.0 : 1.0;
        var to = expanded ? 1.0 : 0.0;

        if (expanded)
            _labelClip.IsVisible = true;

        MavMotion.Animate(this, "efab_exp", from, to, MavMotion.Medium1, MavMotion.EmphasizedDecelerate, v =>
        {
            _labelClip.MaxWidth = targetWidth * v;
            _labelClip.Opacity = v;
            _surface.Padding = new Thickness(16, 0, 16 + 4 * v, 0);
            _surface.MinWidth = 56 + 24 * v;
        }, () =>
        {
            _labelClip.IsVisible = expanded;
            _labelClip.MaxWidth = expanded ? double.PositiveInfinity : 0;
        });
    }

    private void ApplyTheme(MavTheme t)
    {
        _label.Apply(MavTypeRole.LabelLarge, t.Typography);
        var c = t.Color;
        var (bg, fg) = ColorScheme switch
        {
            MavFabColor.Primary => (c.PrimaryContainer, c.OnPrimaryContainer),
            MavFabColor.Secondary => (c.SecondaryContainer, c.OnSecondaryContainer),
            MavFabColor.Tertiary => (c.TertiaryContainer, c.OnTertiaryContainer),
            MavFabColor.Surface => (c.SurfaceContainerHigh, c.Primary),
            _ => (c.PrimaryContainer, c.OnPrimaryContainer),
        };

        _surface.FillColor = bg;
        _surface.RippleColor = fg;
        _icon.Color = fg;
        _label.Foreground = MavTheme.BrushOf(fg);
    }
}

public enum MavButtonGroupKind
{
    Standard,
    Connected,
}

/// <summary>
/// Material Design 3 Expressive button group (Standard or Connected with shared inner corners).
/// </summary>
public class MavButtonGroup : StackPanel, IEnumerable<Control>
{
    public static readonly StyledProperty<MavButtonGroupKind> KindProperty =
        AvaloniaProperty.Register<MavButtonGroup, MavButtonGroupKind>(nameof(Kind), MavButtonGroupKind.Standard);

    public static readonly StyledProperty<bool> MultiSelectProperty =
        AvaloniaProperty.Register<MavButtonGroup, bool>(nameof(MultiSelect));

    private readonly MavItemEventWiring<MavButton, EventHandler<bool>> _wiredButtons = new(
        static (button, handler) => button.SelectedChanged += handler,
        static (button, handler) => button.SelectedChanged -= handler);
    private bool _syncingSelection;

    public event EventHandler<int>? SelectionChanged;

    static MavButtonGroup()
    {
        KindProperty.Changed.AddClassHandler<MavButtonGroup>((g, e) =>
        {
            g.Spacing = e.GetNewValue<MavButtonGroupKind>() == MavButtonGroupKind.Connected ? 2 : 8;
            g.UpdateChildRadii();
        });
    }

    public MavButtonGroup() : this(MavButtonGroupKind.Standard)
    {
    }

    public MavButtonGroup(MavButtonGroupKind kind = MavButtonGroupKind.Standard)
    {
        Orientation = Orientation.Horizontal;
        Kind = kind;
        Spacing = kind == MavButtonGroupKind.Connected ? 2 : 8;
        HorizontalAlignment = HorizontalAlignment.Left;
        Children.CollectionChanged += (_, e) =>
        {
            _wiredButtons.HandleCollectionChanged(e);
            WireChildButtons();
            UpdateChildRadii();
        };
    }

    public MavButtonGroupKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public MavButtonGroupKind Variant
    {
        get => Kind;
        set => Kind = value;
    }

    public bool MultiSelect
    {
        get => GetValue(MultiSelectProperty);
        set => SetValue(MultiSelectProperty, value);
    }

    private void WireChildButtons()
    {
        foreach (var btn in Children.OfType<MavButton>())
        {
            _wiredButtons.Add(btn, (s, isSelected) =>
            {
                if (_syncingSelection || s is not MavButton clickedBtn) return;
                var buttons = Children.OfType<MavButton>().ToList();
                var idx = buttons.IndexOf(clickedBtn);
                if (!MultiSelect && isSelected)
                {
                    _syncingSelection = true;
                    for (int i = 0; i < buttons.Count; i++)
                    {
                        if (i != idx && buttons[i].IsToggle)
                            buttons[i].IsSelected = false;
                    }
                    _syncingSelection = false;
                }
                if (idx >= 0 && (MultiSelect || isSelected))
                    SelectionChanged?.Invoke(this, idx);
            });
        }
    }

    public MavButtonGroup AddButton(MavButton button)
    {
        Children.Add(button);
        UpdateChildRadii();
        return this;
    }

    public void Add(MavButton button) => AddButton(button);

    public IEnumerator<Control> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void UpdateChildRadii()
    {
        var buttons = Children.OfType<MavButton>().ToList();
        for (int i = 0; i < buttons.Count; i++)
        {
            if (Kind != MavButtonGroupKind.Connected || buttons.Count <= 1)
            {
                buttons[i].SetGroupCornerRadii(null);
                continue;
            }

            const double outer = 20;
            const double inner = 6;
            if (i == 0)
                buttons[i].SetGroupCornerRadii(new CornerRadius(outer, inner, inner, outer));
            else if (i == buttons.Count - 1)
                buttons[i].SetGroupCornerRadii(new CornerRadius(inner, outer, outer, inner));
            else
                buttons[i].SetGroupCornerRadii(new CornerRadius(inner));
        }
    }
}
