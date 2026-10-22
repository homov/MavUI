using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Expressive Split Button with a primary action segment and a menu trigger segment.
/// </summary>
public class MdSplitButton : MdContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdSplitButton, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdSplitButton, string?>(nameof(Icon));

    public static readonly StyledProperty<MdButtonVariant> VariantProperty =
        AvaloniaProperty.Register<MdSplitButton, MdButtonVariant>(nameof(Variant), MdButtonVariant.Filled);

    public static readonly StyledProperty<MdButtonSize> SizeProperty =
        AvaloniaProperty.Register<MdSplitButton, MdButtonSize>(nameof(Size), MdButtonSize.Small);

    public static readonly StyledProperty<bool> IsMenuOpenProperty =
        AvaloniaProperty.Register<MdSplitButton, bool>(nameof(IsMenuOpen));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdSplitButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MdSplitButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<ICommand?> MenuCommandProperty =
        AvaloniaProperty.Register<MdSplitButton, ICommand?>(nameof(MenuCommand));

    public static readonly StyledProperty<object?> MenuCommandParameterProperty =
        AvaloniaProperty.Register<MdSplitButton, object?>(nameof(MenuCommandParameter));

    public static readonly StyledProperty<MdMenu?> MenuProperty =
        AvaloniaProperty.Register<MdSplitButton, MdMenu?>(nameof(Menu));

    private readonly MdInkSurface _leading;
    private readonly MdInkSurface _trailing;
    private readonly MdIcon _leadIcon;
    private readonly TextBlock _label;
    private readonly MdIcon _arrowIcon;
    private MdMenu? _menu;

    public event EventHandler? Clicked;
    public event EventHandler? MenuClicked;

    static MdSplitButton()
    {
        TextProperty.Changed.AddClassHandler<MdSplitButton>((s, _) => s.Sync());
        IconProperty.Changed.AddClassHandler<MdSplitButton>((s, _) => s.Sync());
        VariantProperty.Changed.AddClassHandler<MdSplitButton>((s, _) => s.ApplyTheme(MdTheme.Current));
        SizeProperty.Changed.AddClassHandler<MdSplitButton>((s, _) => s.ApplyTheme(MdTheme.Current));
        IsMenuOpenProperty.Changed.AddClassHandler<MdSplitButton>((s, e) => s.OnMenuOpenChanged(e.GetNewValue<bool>()));
        CommandProperty.Changed.AddClassHandler<MdSplitButton>((s, e) => s._leading.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MdSplitButton>((s, e) => s._leading.CommandParameter = e.GetNewValue<object?>());
        MenuProperty.Changed.AddClassHandler<MdSplitButton>((s, e) =>
        {
            if (s._menu is not null)
                s._menu.Closed -= s.OnMenuClosed;
            s._menu = e.GetNewValue<MdMenu?>();
            MdVisualTreeHelpers.AttachLogicalChild(s, s._menu);
            if (s._menu is not null)
                s._menu.Closed += s.OnMenuClosed;
        });
    }

    public MdSplitButton() : this(string.Empty)
    {
    }

    public MdSplitButton(
        string text = "",
        MdButtonVariant variant = MdButtonVariant.Filled,
        string? icon = null,
        MdButtonSize size = MdButtonSize.Small)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _leadIcon = new MdIcon { Size = 18, IsVisible = false, UseThemeDefaultColor = false };
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

        _leading = new MdInkSurface { Content = leadRow };
        _leading.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);

        _arrowIcon = new MdIcon(MdIcons.KeyboardArrowDown, 20) { UseThemeDefaultColor = false };
        _trailing = new MdInkSurface { Content = _arrowIcon };
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
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public bool IsMenuOpen
    {
        get => GetValue(IsMenuOpenProperty);
        set => SetValue(IsMenuOpenProperty, value);
    }

    [Content]
    public MdMenu? Menu
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
        MdMotion.Animate(this, "split_arrow", _arrowIcon.GetRotation(), open ? 180 : 0, MdMotion.Short4, MdMotion.CubicOut, v =>
        {
            _arrowIcon.SetRotation(v);
        });
        ApplyTheme(MdTheme.Current);
    }

    private void Sync()
    {
        _label.Text = Text;
        var hasIcon = !string.IsNullOrEmpty(Icon);
        _leadIcon.Glyph = Icon;
        _leadIcon.IsVisible = hasIcon;
        ApplyTheme(MdTheme.Current);
    }

    private void ApplyTheme(MdTheme t)
    {
        var (h, padH, trailW, iconSz, role) = Size switch
        {
            MdButtonSize.ExtraSmall => (32.0, 12.0, 30.0, 16.0, MdTypeRole.LabelMedium),
            MdButtonSize.Small => (40.0, 16.0, 36.0, 18.0, MdTypeRole.LabelLarge),
            MdButtonSize.Medium => (48.0, 20.0, 44.0, 20.0, MdTypeRole.TitleSmall),
            MdButtonSize.Large => (56.0, 24.0, 52.0, 22.0, MdTypeRole.TitleMedium),
            MdButtonSize.ExtraLarge => (64.0, 28.0, 58.0, 24.0, MdTypeRole.TitleLarge),
            _ => (40.0, 16.0, 36.0, 18.0, MdTypeRole.LabelLarge),
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
            MdButtonVariant.Filled => (c.Primary, c.OnPrimary, Colors.Transparent, 0.0, 0.0),
            MdButtonVariant.Tonal => (c.SecondaryContainer, c.OnSecondaryContainer, Colors.Transparent, 0.0, 0.0),
            MdButtonVariant.Elevated => (c.SurfaceContainerLow, c.Primary, Colors.Transparent, 0.0, 1.0),
            MdButtonVariant.Outlined => (Colors.Transparent, c.Primary, c.Outline, 1.0, 0.0),
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

        _label.Foreground = MdTheme.BrushOf(fg);
        _leadIcon.Color = fg;
        _arrowIcon.Color = fg;
    }
}
