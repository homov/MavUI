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
