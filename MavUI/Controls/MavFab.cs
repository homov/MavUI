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

    /// <summary>Creates a floating action button with the add icon.</summary>
    public MavFab() : this(MavIcons.Add)
    {
    }

    /// <summary>Creates a floating action button with the given icon, size and variant.</summary>
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
            Child = _icon,
        };
        _surface.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);

        Child = _surface;
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
