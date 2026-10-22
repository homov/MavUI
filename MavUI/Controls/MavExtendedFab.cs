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

    /// <summary>Creates an extended floating action button without a label.</summary>
    public MavExtendedFab() : this(string.Empty)
    {
    }

    /// <summary>Creates an extended floating action button with a label and icon.</summary>
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
