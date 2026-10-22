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

/// <summary>Visual style of a <see cref="MavButton"/> (filled, tonal, outlined, text, and so on).</summary>
public enum MavButtonVariant
{
    Filled,
    Tonal,
    Elevated,
    Outlined,
    Text,
}

/// <summary>Size preset of a <see cref="MavButton"/>.</summary>
public enum MavButtonSize
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge,
}

/// <summary>Corner shape of a <see cref="MavButton"/>.</summary>
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

    /// <summary>Creates an empty button.</summary>
    public MavButton() : this(string.Empty)
    {
    }

    /// <summary>Creates a button with the given text, variant and optional leading icon.</summary>
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

    /// <summary>Sets the corner radii used when the button is part of a <see cref="MavButtonGroup"/>, so inner corners look connected.</summary>
    internal void SetGroupCornerRadii(CornerRadius? radii)
    {
        _customCornerRadii = radii;
        ApplyShape(MavTheme.Current);
    }

    /// <summary>Re-applies the theme-dependent colors when a styled property that affects them changes.</summary>
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
