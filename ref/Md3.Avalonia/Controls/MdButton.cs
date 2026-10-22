using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdButtonVariant
{
    Filled,
    Tonal,
    Elevated,
    Outlined,
    Text,
}

public enum MdButtonSize
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge,
}

public enum MdButtonShape
{
    Round,
    Square,
}

/// <summary>
/// Material Design 3 button with 5 variants, 5 Expressive sizes, round/square shapes,
/// optional toggle mode, and leading/trailing icons.
/// </summary>
public class MdButton : MdContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdButton, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdButton, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> TrailingIconProperty =
        AvaloniaProperty.Register<MdButton, string?>(nameof(TrailingIcon));

    public static readonly StyledProperty<MdButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MdButton, MdButtonVariant>(nameof(Variant), MdButtonVariant.Filled);

    public static readonly StyledProperty<MdButtonSize> SizeProperty =
        AvaloniaProperty.Register<MdButton, MdButtonSize>(nameof(Size), MdButtonSize.Small);

    public static readonly StyledProperty<MdButtonShape> ButtonShapeProperty =
        AvaloniaProperty.Register<MdButton, MdButtonShape>(nameof(ButtonShape), MdButtonShape.Round);

    public static readonly StyledProperty<bool> IsToggleProperty =
        AvaloniaProperty.Register<MdButton, bool>(nameof(IsToggle));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MdButton, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<MdButton, bool>(nameof(IsLoading));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdButton, object?>(nameof(CommandParameter));

    private readonly MdInkSurface _surface;
    private readonly StackPanel _row;
    private readonly MdCircularProgress _spinner;
    private readonly MdIcon _leadingIcon;
    private readonly TextBlock _label;
    private readonly MdIcon _trailingIcon;
    private CornerRadius? _customCornerRadii;
    private double _shapeProgress;
    private bool _pressed;

    public event EventHandler? Clicked;
    public event EventHandler<bool>? SelectedChanged;
    public event EventHandler<bool>? Toggled;

    static MdButton()
    {
        TextProperty.Changed.AddClassHandler<MdButton>((b, _) =>
        {
            b.SyncContent();
            b.SyncAutomation();
        });
        IconProperty.Changed.AddClassHandler<MdButton>((b, _) => b.SyncContent());
        TrailingIconProperty.Changed.AddClassHandler<MdButton>((b, _) => b.SyncContent());
        VariantProperty.Changed.AddClassHandler<MdButton>((b, _) => b.ApplyTheme(MdTheme.Current));
        SizeProperty.Changed.AddClassHandler<MdButton>((b, _) => b.ApplyTheme(MdTheme.Current));
        ButtonShapeProperty.Changed.AddClassHandler<MdButton>((b, _) => b.ApplyTheme(MdTheme.Current));
        IsToggleProperty.Changed.AddClassHandler<MdButton>((b, _) => b.ApplyTheme(MdTheme.Current));
        IsSelectedProperty.Changed.AddClassHandler<MdButton>((b, e) =>
        {
            b.ApplyTheme(MdTheme.Current);
            var val = e.GetNewValue<bool>();
            b.SelectedChanged?.Invoke(b, val);
            b.Toggled?.Invoke(b, val);
        });
        IsLoadingProperty.Changed.AddClassHandler<MdButton>((b, _) =>
        {
            b.SyncContent();
            b.ApplyTheme(MdTheme.Current);
        });
        CommandProperty.Changed.AddClassHandler<MdButton>((b, e) => b._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MdButton>((b, e) => b._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MdButton() : this(string.Empty)
    {
    }

    public MdButton(
        string text = "",
        MdButtonVariant variant = MdButtonVariant.Filled,
        string? icon = null,
        MdButtonSize size = MdButtonSize.Small)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _spinner = new MdCircularProgress(18, 2.2)
        {
            IsIndeterminate = true,
            IsVisible = false,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _leadingIcon = new MdIcon { IsVisible = false, UseThemeDefaultColor = false };
        _trailingIcon = new MdIcon { IsVisible = false, UseThemeDefaultColor = false };
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

        _surface = new MdInkSurface { Content = _row };
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
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MdAutomation.SetName(this, Text);
        MdAutomation.SetControlType(this, AutomationControlType.Button);
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

    public MdButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public MdButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public MdButtonShape ButtonShape
    {
        get => GetValue(ButtonShapeProperty);
        set => SetValue(ButtonShapeProperty, value);
    }

    public MdButtonShape Shape
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
        ApplyShape(MdTheme.Current);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            _surface.IsEnabled = IsEnabled;
            ApplyTheme(MdTheme.Current);
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
        MdMotion.Animate(this, "btn_morph", _shapeProgress, target, MdMotion.Short3, MdMotion.CubicOut, v =>
        {
            _shapeProgress = v;
            ApplyShape(MdTheme.Current);
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
        ApplySizeTokens(MdTheme.Current);
    }

    private void ApplySizeTokens(MdTheme t)
    {
        var (height, padH, iconSize, role, gap) = Size switch
        {
            MdButtonSize.ExtraSmall => (32.0, 12.0, 16.0, MdTypeRole.LabelMedium, 6.0),
            MdButtonSize.Small => (40.0, 20.0, 18.0, MdTypeRole.LabelLarge, 8.0),
            MdButtonSize.Medium => (48.0, 24.0, 20.0, MdTypeRole.TitleSmall, 8.0),
            MdButtonSize.Large => (56.0, 28.0, 24.0, MdTypeRole.TitleMedium, 10.0),
            MdButtonSize.ExtraLarge => (64.0, 32.0, 26.0, MdTypeRole.TitleLarge, 12.0),
            _ => (40.0, 20.0, 18.0, MdTypeRole.LabelLarge, 8.0),
        };

        var densityDelta = t.DensityOffset;
        var finalHeight = Math.Max(28, height + densityDelta);

        var hasLead = IsLoading || !string.IsNullOrEmpty(Icon);
        var hasTrail = !string.IsNullOrEmpty(TrailingIcon);
        var leftPad = Variant == MdButtonVariant.Text ? 12.0 : (hasLead ? Math.Max(12, padH - 4) : padH);
        var rightPad = Variant == MdButtonVariant.Text ? 12.0 : (hasTrail ? Math.Max(12, padH - 4) : padH);

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

    private void ApplyShape(MdTheme t)
    {
        if (_customCornerRadii is { } custom)
        {
            _surface.CornerRadii = custom;
            return;
        }

        var (height, squareR) = Size switch
        {
            MdButtonSize.ExtraSmall => (32.0, 8.0),
            MdButtonSize.Small => (40.0, 10.0),
            MdButtonSize.Medium => (48.0, 12.0),
            MdButtonSize.Large => (56.0, 16.0),
            MdButtonSize.ExtraLarge => (64.0, 20.0),
            _ => (40.0, 10.0),
        };
        var roundR = (height + t.DensityOffset) / 2.0;

        double baseR = ButtonShape == MdButtonShape.Round ? roundR : squareR;
        if (IsToggle && IsSelected)
            baseR = ButtonShape == MdButtonShape.Round ? squareR : roundR;

        var pressedR = Math.Max(6, squareR - 2);
        var r = baseR + (pressedR - baseR) * _shapeProgress;
        _surface.CornerRadius = r;
    }

    private void ApplyTheme(MdTheme t)
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

            bg = Variant is MdButtonVariant.Outlined or MdButtonVariant.Text ? Colors.Transparent : disContainer;
            fg = disContent;
            if (Variant == MdButtonVariant.Outlined)
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
                    MdButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
                    MdButtonVariant.Tonal => (c.Secondary, c.OnSecondary, Colors.Transparent, 0, 0),
                    MdButtonVariant.Elevated => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 2),
                    MdButtonVariant.Outlined => (c.InverseSurface, c.InverseOnSurface, Colors.Transparent, 0, 0),
                    MdButtonVariant.Text => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0, 0),
                    _ => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
                };
            }
            else
            {
                (bg, fg, stroke, strokeW, elev) = Variant switch
                {
                    MdButtonVariant.Filled => (c.SurfaceContainerHighest, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                    MdButtonVariant.Tonal => (c.SurfaceContainerHigh, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                    MdButtonVariant.Elevated => (c.SurfaceContainerLow, c.Primary, Colors.Transparent, 0, 1),
                    MdButtonVariant.Outlined => (Colors.Transparent, c.OnSurfaceVariant, c.Outline, 1, 0),
                    MdButtonVariant.Text => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                    _ => (c.SurfaceContainerHighest, c.OnSurfaceVariant, Colors.Transparent, 0, 0),
                };
            }
        }
        else
        {
            (bg, fg, stroke, strokeW, elev) = Variant switch
            {
                MdButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
                MdButtonVariant.Tonal => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0, 0),
                MdButtonVariant.Elevated => (c.SurfaceContainerLow, c.Primary, Colors.Transparent, 0, 1),
                MdButtonVariant.Outlined => (Colors.Transparent, c.Primary, c.Outline, 1, 0),
                MdButtonVariant.Text => (Colors.Transparent, c.Primary, Colors.Transparent, 0, 0),
                _ => (c.Primary, c.OnPrimary, Colors.Transparent, 0, 0),
            };
        }

        _surface.FillColor = bg;
        _surface.StrokeColor = stroke;
        _surface.StrokeThickness = strokeW;
        _surface.RippleColor = fg;
        _surface.Elevation = elev;
        _label.Foreground = MdTheme.BrushOf(fg);
        _spinner.CustomActiveColor = fg;
        _leadingIcon.Color = fg;
        _trailingIcon.Color = fg;
    }
}

public enum MdIconButtonVariant
{
    Standard,
    Filled,
    Tonal,
    Outlined,
}

/// <summary>
/// Material Design 3 icon button with Standard, Filled, Tonal, and Outlined styles and toggle state.
/// </summary>
public class MdIconButton : MdContentView
{
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MdIconButton, string>(nameof(Icon), MdIcons.Favorite);

    public static readonly StyledProperty<string?> SelectedIconProperty =
        AvaloniaProperty.Register<MdIconButton, string?>(nameof(SelectedIcon));

    public static readonly StyledProperty<MdIconButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MdIconButton, MdIconButtonVariant>(nameof(Variant), MdIconButtonVariant.Standard);

    public static readonly StyledProperty<bool> IsToggleProperty =
        AvaloniaProperty.Register<MdIconButton, bool>(nameof(IsToggle));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MdIconButton, bool>(nameof(IsSelected));

    public static readonly StyledProperty<double> ButtonSizeProperty =
        AvaloniaProperty.Register<MdIconButton, double>(nameof(ButtonSize), 40.0);

    /// <summary>
    /// Minimum hit area of the button. The visual state layer keeps
    /// <see cref="ButtonSize"/>; the extra space only widens the touch target
    /// (Material Design 3 recommends at least 48dp).
    /// </summary>
    public static readonly StyledProperty<double> TouchTargetSizeProperty =
        AvaloniaProperty.Register<MdIconButton, double>(nameof(TouchTargetSize), 48.0);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdIconButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdIconButton, object?>(nameof(CommandParameter));

    private readonly MdInkSurface _surface;
    private readonly MdIcon _icon;

    public event EventHandler? Clicked;
    public event EventHandler<bool>? SelectedChanged;

    static MdIconButton()
    {
        IconProperty.Changed.AddClassHandler<MdIconButton>((b, _) => b.SyncGlyph());
        SelectedIconProperty.Changed.AddClassHandler<MdIconButton>((b, _) => b.SyncGlyph());
        VariantProperty.Changed.AddClassHandler<MdIconButton>((b, _) => b.ApplyTheme(MdTheme.Current));
        IsToggleProperty.Changed.AddClassHandler<MdIconButton>((b, _) => b.ApplyTheme(MdTheme.Current));
        IsSelectedProperty.Changed.AddClassHandler<MdIconButton>((b, e) =>
        {
            b.SyncGlyph();
            b.ApplyTheme(MdTheme.Current);
            b.SelectedChanged?.Invoke(b, e.GetNewValue<bool>());
        });
        ButtonSizeProperty.Changed.AddClassHandler<MdIconButton>((b, _) => b.ApplySize());
        TouchTargetSizeProperty.Changed.AddClassHandler<MdIconButton>((b, _) => b.ApplySize());
        CommandProperty.Changed.AddClassHandler<MdIconButton>((b, e) => b._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MdIconButton>((b, e) => b._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MdIconButton() : this(MdIcons.Favorite)
    {
    }

    public MdIconButton(
        string icon = MdIcons.Favorite,
        MdIconButtonVariant variant = MdIconButtonVariant.Standard,
        bool isToggle = false)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);

        _icon = new MdIcon(icon, 22) { UseThemeDefaultColor = false };
        _surface = new MdInkSurface
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

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MdAutomation.SetControlType(this, AutomationControlType.Button);
        MdAutomation.SetName(this, ToolTip.GetTip(this) as string);
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

    public MdIconButtonVariant Variant
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
            ApplyTheme(MdTheme.Current);
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

    private void ApplyTheme(MdTheme t)
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
            bg = Variant is MdIconButtonVariant.Filled or MdIconButtonVariant.Tonal ? disContainer : Colors.Transparent;
            fg = disContent;
            if (Variant == MdIconButtonVariant.Outlined)
            {
                stroke = disContainer;
                strokeW = 1;
            }
        }
        else if (IsToggle)
        {
            (bg, fg, stroke, strokeW) = (Variant, IsSelected) switch
            {
                (MdIconButtonVariant.Standard, false) => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0.0),
                (MdIconButtonVariant.Standard, true) => (Colors.Transparent, c.Primary, Colors.Transparent, 0.0),
                (MdIconButtonVariant.Filled, false) => (c.SurfaceContainerHighest, c.Primary, Colors.Transparent, 0.0),
                (MdIconButtonVariant.Filled, true) => (c.Primary, c.OnPrimary, Colors.Transparent, 0.0),
                (MdIconButtonVariant.Tonal, false) => (c.SurfaceContainerHighest, c.OnSurfaceVariant, Colors.Transparent, 0.0),
                (MdIconButtonVariant.Tonal, true) => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0.0),
                (MdIconButtonVariant.Outlined, false) => (Colors.Transparent, c.OnSurfaceVariant, c.Outline, 1.0),
                (MdIconButtonVariant.Outlined, true) => (c.InverseSurface, c.InverseOnSurface, Colors.Transparent, 0.0),
                _ => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0.0),
            };
        }
        else
        {
            (bg, fg, stroke, strokeW) = Variant switch
            {
                MdIconButtonVariant.Standard => (Colors.Transparent, c.OnSurfaceVariant, Colors.Transparent, 0.0),
                MdIconButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0.0),
                MdIconButtonVariant.Tonal => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0.0),
                MdIconButtonVariant.Outlined => (Colors.Transparent, c.OnSurfaceVariant, c.Outline, 1.0),
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

public enum MdFabSize
{
    Small,
    Regular,
    Large,
}

public enum MdFabColor
{
    Primary,
    Secondary,
    Tertiary,
    Surface,
}

/// <summary>
/// Material Design 3 Floating Action Button (Small 40dp, Regular 56dp, Large 96dp).
/// </summary>
public class MdFab : MdContentView
{
    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MdFab, string>(nameof(Icon), MdIcons.Add);

    public static readonly StyledProperty<MdFabSize> SizeProperty =
        AvaloniaProperty.Register<MdFab, MdFabSize>(nameof(Size), MdFabSize.Regular);

    public static readonly StyledProperty<MdFabColor> ColorSchemeProperty =
        AvaloniaProperty.Register<MdFab, MdFabColor>(nameof(ColorScheme), MdFabColor.Primary);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdFab, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdFab, object?>(nameof(CommandParameter));

    private readonly MdInkSurface _surface;
    private readonly MdIcon _icon;

    public event EventHandler? Clicked;

    static MdFab()
    {
        IconProperty.Changed.AddClassHandler<MdFab>((f, e) => f._icon.Glyph = e.GetNewValue<string>());
        SizeProperty.Changed.AddClassHandler<MdFab>((f, _) => f.ApplyTheme(MdTheme.Current));
        ColorSchemeProperty.Changed.AddClassHandler<MdFab>((f, _) => f.ApplyTheme(MdTheme.Current));
        CommandProperty.Changed.AddClassHandler<MdFab>((f, e) => f._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MdFab>((f, e) => f._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MdFab() : this(MdIcons.Add)
    {
    }

    public MdFab(
        string icon = MdIcons.Add,
        MdFabSize size = MdFabSize.Regular,
        MdFabColor color = MdFabColor.Primary)
    {
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _icon = new MdIcon(icon, 24) { UseThemeDefaultColor = false };
        _surface = new MdInkSurface
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

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MdAutomation.SetControlType(this, AutomationControlType.Button);
        MdAutomation.SetName(this, ToolTip.GetTip(this) as string);
    }

    [Content]
    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public MdFabSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public MdFabColor ColorScheme
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

    private void ApplyTheme(MdTheme t)
    {
        var (dim, radius, iconSz) = Size switch
        {
            MdFabSize.Small => (40.0, 12.0, 22.0),
            MdFabSize.Large => (96.0, 28.0, 36.0),
            _ => (56.0, 16.0, 24.0),
        };

        _surface.Width = dim;
        _surface.Height = dim;
        _surface.CornerRadius = radius;
        _icon.Size = iconSz;

        var c = t.Color;
        var (bg, fg) = ColorScheme switch
        {
            MdFabColor.Primary => (c.PrimaryContainer, c.OnPrimaryContainer),
            MdFabColor.Secondary => (c.SecondaryContainer, c.OnSecondaryContainer),
            MdFabColor.Tertiary => (c.TertiaryContainer, c.OnTertiaryContainer),
            MdFabColor.Surface => (c.SurfaceContainerHigh, c.Primary),
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
public class MdExtendedFab : MdContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdExtendedFab, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MdExtendedFab, string>(nameof(Icon), MdIcons.Edit);

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MdExtendedFab, bool>(nameof(IsExpanded), true);

    public static readonly StyledProperty<MdFabColor> ColorSchemeProperty =
        AvaloniaProperty.Register<MdExtendedFab, MdFabColor>(nameof(ColorScheme), MdFabColor.Primary);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdExtendedFab, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdExtendedFab, object?>(nameof(CommandParameter));

    private readonly MdInkSurface _surface;
    private readonly MdIcon _icon;
    private readonly TextBlock _label;
    private readonly Border _labelClip;

    public event EventHandler? Clicked;

    static MdExtendedFab()
    {
        TextProperty.Changed.AddClassHandler<MdExtendedFab>((f, e) =>
        {
            f._label.Text = e.GetNewValue<string>();
            f.SyncAutomation();
        });
        IconProperty.Changed.AddClassHandler<MdExtendedFab>((f, e) => f._icon.Glyph = e.GetNewValue<string>());
        IsExpandedProperty.Changed.AddClassHandler<MdExtendedFab>((f, e) => f.AnimateExpanded(e.GetNewValue<bool>()));
        ColorSchemeProperty.Changed.AddClassHandler<MdExtendedFab>((f, _) => f.ApplyTheme(MdTheme.Current));
        CommandProperty.Changed.AddClassHandler<MdExtendedFab>((f, e) => f._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MdExtendedFab>((f, e) => f._surface.CommandParameter = e.GetNewValue<object?>());
    }

    public MdExtendedFab() : this(string.Empty)
    {
    }

    public MdExtendedFab(
        string text,
        string icon = MdIcons.Edit,
        MdFabColor color = MdFabColor.Primary)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _icon = new MdIcon(icon, 24) { UseThemeDefaultColor = false };
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

        _surface = new MdInkSurface
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

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    private void SyncAutomation()
    {
        MdAutomation.SetName(this, Text);
        MdAutomation.SetControlType(this, AutomationControlType.Button);
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

    public MdFabColor ColorScheme
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

        MdMotion.Animate(this, "efab_exp", from, to, MdMotion.Medium1, MdMotion.EmphasizedDecelerate, v =>
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

    private void ApplyTheme(MdTheme t)
    {
        _label.Apply(MdTypeRole.LabelLarge, t.Typography);
        var c = t.Color;
        var (bg, fg) = ColorScheme switch
        {
            MdFabColor.Primary => (c.PrimaryContainer, c.OnPrimaryContainer),
            MdFabColor.Secondary => (c.SecondaryContainer, c.OnSecondaryContainer),
            MdFabColor.Tertiary => (c.TertiaryContainer, c.OnTertiaryContainer),
            MdFabColor.Surface => (c.SurfaceContainerHigh, c.Primary),
            _ => (c.PrimaryContainer, c.OnPrimaryContainer),
        };

        _surface.FillColor = bg;
        _surface.RippleColor = fg;
        _icon.Color = fg;
        _label.Foreground = MdTheme.BrushOf(fg);
    }
}

public enum MdButtonGroupKind
{
    Standard,
    Connected,
}

/// <summary>
/// Material Design 3 Expressive button group (Standard or Connected with shared inner corners).
/// </summary>
public class MdButtonGroup : StackPanel, IEnumerable<Control>
{
    public static readonly StyledProperty<MdButtonGroupKind> KindProperty =
        AvaloniaProperty.Register<MdButtonGroup, MdButtonGroupKind>(nameof(Kind), MdButtonGroupKind.Standard);

    public static readonly StyledProperty<bool> MultiSelectProperty =
        AvaloniaProperty.Register<MdButtonGroup, bool>(nameof(MultiSelect));

    private readonly HashSet<MdButton> _wiredButtons = new();
    private bool _syncingSelection;

    public event EventHandler<int>? SelectionChanged;

    static MdButtonGroup()
    {
        KindProperty.Changed.AddClassHandler<MdButtonGroup>((g, e) =>
        {
            g.Spacing = e.GetNewValue<MdButtonGroupKind>() == MdButtonGroupKind.Connected ? 2 : 8;
            g.UpdateChildRadii();
        });
    }

    public MdButtonGroup() : this(MdButtonGroupKind.Standard)
    {
    }

    public MdButtonGroup(MdButtonGroupKind kind = MdButtonGroupKind.Standard)
    {
        Orientation = Orientation.Horizontal;
        Kind = kind;
        Spacing = kind == MdButtonGroupKind.Connected ? 2 : 8;
        HorizontalAlignment = HorizontalAlignment.Left;
        Children.CollectionChanged += (_, _) =>
        {
            WireChildButtons();
            UpdateChildRadii();
        };
    }

    public MdButtonGroupKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public MdButtonGroupKind Variant
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
        foreach (var btn in Children.OfType<MdButton>())
        {
            if (_wiredButtons.Add(btn))
            {
                btn.SelectedChanged += (s, isSelected) =>
                {
                    if (_syncingSelection || s is not MdButton clickedBtn) return;
                    var buttons = Children.OfType<MdButton>().ToList();
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
                };
            }
        }
    }

    public MdButtonGroup AddButton(MdButton button)
    {
        Children.Add(button);
        UpdateChildRadii();
        return this;
    }

    public void Add(MdButton button) => AddButton(button);

    public IEnumerator<Control> GetEnumerator() => Children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void UpdateChildRadii()
    {
        var buttons = Children.OfType<MdButton>().ToList();
        for (int i = 0; i < buttons.Count; i++)
        {
            if (Kind != MdButtonGroupKind.Connected || buttons.Count <= 1)
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
