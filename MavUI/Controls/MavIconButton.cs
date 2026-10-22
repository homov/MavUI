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

/// <summary>Visual style of a <see cref="MavIconButton"/>: standard, filled, tonal or outlined.</summary>
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

    /// <summary>Creates an icon button that shows the favorite icon.</summary>
    public MavIconButton() : this(MavIcons.Favorite)
    {
    }

    /// <summary>Creates an icon button with the given icon, variant and size.</summary>
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

    /// <summary>Redraws the button when a visual property changes.</summary>
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
