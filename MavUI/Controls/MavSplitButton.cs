using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Expressive Split Button with a primary action segment and a menu trigger segment.
/// </summary>
public class MavSplitButton : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavSplitButton, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavSplitButton, string?>(nameof(Icon));

    public static readonly StyledProperty<MavButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MavSplitButton, MavButtonVariant>(nameof(Variant), MavButtonVariant.Filled);

    public static readonly StyledProperty<MavButtonSize> SizeProperty =
        AvaloniaProperty.Register<MavSplitButton, MavButtonSize>(nameof(Size), MavButtonSize.Small);

    public static readonly StyledProperty<bool> IsMenuOpenProperty =
        AvaloniaProperty.Register<MavSplitButton, bool>(nameof(IsMenuOpen));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavSplitButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavSplitButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<ICommand?> MenuCommandProperty =
        AvaloniaProperty.Register<MavSplitButton, ICommand?>(nameof(MenuCommand));

    public static readonly StyledProperty<object?> MenuCommandParameterProperty =
        AvaloniaProperty.Register<MavSplitButton, object?>(nameof(MenuCommandParameter));

    public static readonly StyledProperty<MavMenu?> MenuProperty =
        AvaloniaProperty.Register<MavSplitButton, MavMenu?>(nameof(Menu));

    private readonly MavInkSurface _leading;
    private readonly MavInkSurface _trailing;
    private readonly MavIcon _leadIcon;
    private readonly TextBlock _label;
    private readonly MavIcon _arrowIcon;
    private MavMenu? _menu;

    public event EventHandler? Clicked;
    public event EventHandler? MenuClicked;

    static MavSplitButton()
    {
        TextProperty.Changed.AddClassHandler<MavSplitButton>((s, _) => s.Sync());
        IconProperty.Changed.AddClassHandler<MavSplitButton>((s, _) => s.Sync());
        VariantProperty.Changed.AddClassHandler<MavSplitButton>((s, _) => s.ApplyTheme(MavTheme.Current));
        SizeProperty.Changed.AddClassHandler<MavSplitButton>((s, _) => s.ApplyTheme(MavTheme.Current));
        IsMenuOpenProperty.Changed.AddClassHandler<MavSplitButton>((s, e) => s.OnMenuOpenChanged(e.GetNewValue<bool>()));
        CommandProperty.Changed.AddClassHandler<MavSplitButton>((s, e) => s._leading.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavSplitButton>((s, e) => s._leading.CommandParameter = e.GetNewValue<object?>());
        MenuProperty.Changed.AddClassHandler<MavSplitButton>((s, e) =>
        {
            if (s._menu is not null)
                s._menu.Closed -= s.OnMenuClosed;
            s._menu = e.GetNewValue<MavMenu?>();
            MavVisualTreeHelpers.AttachLogicalChild(s, s._menu);
            if (s._menu is not null)
                s._menu.Closed += s.OnMenuClosed;
        });
    }

    /// <summary>Creates an empty split button.</summary>
    public MavSplitButton() : this(string.Empty)
    {
    }

    /// <summary>Creates a split button with a main action and a menu of secondary actions.</summary>
    public MavSplitButton(
        string text = "",
        MavButtonVariant variant = MavButtonVariant.Filled,
        string? icon = null,
        MavButtonSize size = MavButtonSize.Small)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _leadIcon = new MavIcon { Size = 18, IsVisible = false, UseThemeDefaultColor = false };
        _label = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };

        var leadRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        leadRow.Children.Add(_leadIcon);
        leadRow.Children.Add(_label);

        _leading = new MavInkSurface { Content = leadRow };
        _leading.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);

        _arrowIcon = new MavIcon(MavIcons.KeyboardArrowDown, 20) { UseThemeDefaultColor = false };
        _trailing = new MavInkSurface { Content = _arrowIcon };
        _trailing.Clicked += (_, _) =>
        {
            MenuClicked?.Invoke(this, EventArgs.Empty);
            if (MenuCommand is { } mCmd && mCmd.CanExecute(MenuCommandParameter))
                mCmd.Execute(MenuCommandParameter);
            if (_menu is not null)
            {
                IsMenuOpen = true;
                _menu.Show(this);
            }
            else
            {
                IsMenuOpen = !IsMenuOpen;
            }
        };

        var host = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
        };
        host.Children.Add(_leading);
        host.Children.Add(_trailing);

        Content = host;
        Text = text;
        Variant = variant;
        Icon = icon;
        Size = size;

        Sync();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

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

    public string? LeadingIcon
    {
        get => Icon;
        set => Icon = value;
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

    public bool IsMenuOpen
    {
        get => GetValue(IsMenuOpenProperty);
        set => SetValue(IsMenuOpenProperty, value);
    }

    [Content]
    public MavMenu? Menu
    {
        get => GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
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

    public ICommand? MenuCommand
    {
        get => GetValue(MenuCommandProperty);
        set => SetValue(MenuCommandProperty, value);
    }

    public object? MenuCommandParameter
    {
        get => GetValue(MenuCommandParameterProperty);
        set => SetValue(MenuCommandParameterProperty, value);
    }

    private void OnMenuClosed(object? sender, EventArgs e) => IsMenuOpen = false;

    private void OnMenuOpenChanged(bool open)
    {
        MavMotion.Animate(this, "split_arrow", _arrowIcon.GetRotation(), open ? 180 : 0, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            _arrowIcon.SetRotation(v);
        });
        ApplyTheme(MavTheme.Current);
    }

    private void Sync()
    {
        _label.Text = Text;
        var hasIcon = !string.IsNullOrEmpty(Icon);
        _leadIcon.Glyph = Icon;
        _leadIcon.IsVisible = hasIcon;
        ApplyTheme(MavTheme.Current);
    }

    private void ApplyTheme(MavTheme t)
    {
        var (h, padH, trailW, iconSz, role) = Size switch
        {
            MavButtonSize.ExtraSmall => (32.0, 12.0, 30.0, 16.0, MavTypeRole.LabelMedium),
            MavButtonSize.Small => (40.0, 16.0, 36.0, 18.0, MavTypeRole.LabelLarge),
            MavButtonSize.Medium => (48.0, 20.0, 44.0, 20.0, MavTypeRole.TitleSmall),
            MavButtonSize.Large => (56.0, 24.0, 52.0, 22.0, MavTypeRole.TitleMedium),
            MavButtonSize.ExtraLarge => (64.0, 28.0, 58.0, 24.0, MavTypeRole.TitleLarge),
            _ => (40.0, 16.0, 36.0, 18.0, MavTypeRole.LabelLarge),
        };

        var finalH = Math.Max(28, h + t.DensityOffset);
        var outerR = finalH / 2.0;
        const double innerR = 4.0;

        _leading.MinHeight = finalH;
        _leading.Padding = new Thickness(
            !string.IsNullOrEmpty(Icon) ? Math.Max(12, padH - 4) : padH,
            0,
            Math.Max(10, padH - 4),
            0);
        _leading.CornerRadii = new CornerRadius(outerR, innerR, innerR, outerR);

        _trailing.Width = trailW;
        _trailing.Height = finalH;
        _trailing.CornerRadii = IsMenuOpen
            ? new CornerRadius(outerR)
            : new CornerRadius(innerR, outerR, outerR, innerR);

        _leadIcon.Size = iconSz;
        _arrowIcon.Size = iconSz + 2;
        _label.Apply(role, t.Typography);

        var c = t.Color;
        var (bg, fg, stroke, strokeW, elev) = Variant switch
        {
            MavButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0.0, 0.0),
            MavButtonVariant.Tonal => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0.0, 0.0),
            MavButtonVariant.Elevated => (c.SurfaceContainerLow, c.Primary, Colors.Transparent, 0.0, 1.0),
            MavButtonVariant.Outlined => (Colors.Transparent, c.Primary, c.Outline, 1.0, 0.0),
            _ => (c.Primary, c.OnPrimary, Colors.Transparent, 0.0, 0.0),
        };

        foreach (var surf in new[] { _leading, _trailing })
        {
            surf.FillColor = bg;
            surf.StrokeColor = stroke;
            surf.StrokeThickness = strokeW;
            surf.RippleColor = fg;
            surf.Elevation = elev;
        }

        _label.Foreground = MavTheme.BrushOf(fg);
        _leadIcon.Color = fg;
        _arrowIcon.Color = fg;
    }
}
