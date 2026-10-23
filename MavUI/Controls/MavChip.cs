using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 chip (Assist, Filter, Input, Suggestion) with optional leading icon,
/// avatar, selection check, close button, and elevated style.
/// </summary>
public class MavChip : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavChip, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavChip, string?>(nameof(Icon));

    public static readonly StyledProperty<MavChipKind> KindProperty =
        AvaloniaProperty.Register<MavChip, MavChipKind>(nameof(Kind), MavChipKind.Assist);

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavChip, bool>(nameof(IsSelected), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsElevatedProperty =
        AvaloniaProperty.Register<MavChip, bool>(nameof(IsElevated));

    public static readonly StyledProperty<bool> ShowCloseIconProperty =
        AvaloniaProperty.Register<MavChip, bool>(nameof(ShowCloseIcon));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavChip, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavChip, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<MavChip, ICommand?>(nameof(CloseCommand));

    public static readonly StyledProperty<object?> CloseCommandParameterProperty =
        AvaloniaProperty.Register<MavChip, object?>(nameof(CloseCommandParameter));

    public static readonly StyledProperty<Control?> AvatarViewProperty =
        AvaloniaProperty.Register<MavChip, Control?>(nameof(AvatarView));

    private readonly MavInkSurface _surface;
    private readonly Border _avatarHost;
    private readonly MavIcon _leadingIcon;
    private readonly TextBlock _label;
    private readonly Border _closeHit;
    private readonly MavIcon _closeIcon;
    private Control? _avatarView;

    public event EventHandler? Clicked;
    public event EventHandler<bool>? SelectedChanged;
    public event EventHandler? CloseClicked;

    static MavChip()
    {
        TextProperty.Changed.AddClassHandler<MavChip>((c, _) =>
        {
            c.Sync();
            MavAutomation.SetName(c, c.Text);
        });
        IconProperty.Changed.AddClassHandler<MavChip>((c, _) => c.Sync());
        KindProperty.Changed.AddClassHandler<MavChip>((c, _) => c.Sync());
        IsSelectedProperty.Changed.AddClassHandler<MavChip>((c, e) =>
        {
            c.Sync();
            c.SelectedChanged?.Invoke(c, e.GetNewValue<bool>());
        });
        IsElevatedProperty.Changed.AddClassHandler<MavChip>((c, _) => c.ApplyTheme(MavTheme.Current));
        ShowCloseIconProperty.Changed.AddClassHandler<MavChip>((c, _) => c.Sync());
        CommandProperty.Changed.AddClassHandler<MavChip>((c, e) => c._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavChip>((c, e) => c._surface.CommandParameter = e.GetNewValue<object?>());
        AvatarViewProperty.Changed.AddClassHandler<MavChip>((c, e) =>
        {
            c._avatarView = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(c._avatarView);
            c.Sync();
        });
    }

    /// <summary>Creates an empty chip.</summary>
    public MavChip() : this(string.Empty)
    {
    }

    /// <summary>Creates a chip with a label, optional icon, and selected state.</summary>
    public MavChip(
        string text = "",
        MavChipKind kind = MavChipKind.Assist,
        string? icon = null,
        bool selected = false,
        bool elevated = false)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _avatarHost = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
        };
        _leadingIcon = new MavIcon { Size = 18, IsVisible = false, UseThemeDefaultColor = false };
        _label = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
        };
        _closeIcon = new MavIcon(MavIcons.Close, 18) { UseThemeDefaultColor = false };
        _closeHit = new Border
        {
            Width = 22,
            Height = 22,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            IsVisible = false,
            Child = _closeIcon,
        };
        _closeHit.PointerPressed += (_, e) => e.Handled = true;
        _closeHit.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            CloseClicked?.Invoke(this, EventArgs.Empty);
            if (CloseCommand is { } cCmd && cCmd.CanExecute(CloseCommandParameter))
                cCmd.Execute(CloseCommandParameter);
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Children.Add(_avatarHost);
        row.Children.Add(_leadingIcon);
        row.Children.Add(_label);
        row.Children.Add(_closeHit);

        _surface = new MavInkSurface
        {
            MinHeight = 32,
            CornerRadius = 8,
            Padding = new Thickness(12, 0),
            Child = row,
        };
        _surface.Clicked += (_, _) =>
        {
            if (AutoToggleOnClick && Kind is MavChipKind.Filter or MavChipKind.Input)
                IsSelected = !IsSelected;
            Clicked?.Invoke(this, EventArgs.Empty);
        };

        Child = _surface;
        Text = text;
        Kind = kind;
        Icon = icon;
        IsSelected = selected;
        IsElevated = elevated;
        if (kind == MavChipKind.Input)
            ShowCloseIcon = true;

        Sync();
        MavAutomation.SetName(this, Text);
        MavAutomation.SetControlType(this, AutomationControlType.Button);
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    internal bool AutoToggleOnClick { get; set; } = true;

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

    public MavChipKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public MavChipKind Variant
    {
        get => Kind;
        set => Kind = value;
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsElevated
    {
        get => GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }

    public bool ShowCloseIcon
    {
        get => GetValue(ShowCloseIconProperty);
        set => SetValue(ShowCloseIconProperty, value);
    }

    public bool IsRemovable
    {
        get => ShowCloseIcon;
        set => ShowCloseIcon = value;
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

    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public object? CloseCommandParameter
    {
        get => GetValue(CloseCommandParameterProperty);
        set => SetValue(CloseCommandParameterProperty, value);
    }

    public Control? AvatarView
    {
        get => GetValue(AvatarViewProperty);
        set => SetValue(AvatarViewProperty, value);
    }

    /// <summary>Redraws the chip when a visual property changes.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEnabledProperty)
        {
            _surface.IsEnabled = IsEnabled;
            ApplyTheme(MavTheme.Current);
        }
    }

    private void Sync()
    {
        _label.Text = Text;

        var hasAvatar = _avatarView is not null && !(Kind == MavChipKind.Filter && IsSelected);
        if (hasAvatar)
        {
            if (!ReferenceEquals(_avatarHost.Child, _avatarView))
            {
                _avatarHost.Child = null;
                MavVisualTreeHelpers.DetachFromParent(_avatarView);
                _avatarHost.Child = _avatarView;
            }
        }
        else
        {
            _avatarHost.Child = null;
        }
        _avatarHost.IsVisible = hasAvatar;

        string? leadGlyph = Icon;
        if (Kind == MavChipKind.Filter && IsSelected)
            leadGlyph = MavIcons.Check;

        var hasLead = !hasAvatar && !string.IsNullOrEmpty(leadGlyph);
        _leadingIcon.Glyph = leadGlyph;
        _leadingIcon.IsVisible = hasLead;

        var hasClose = ShowCloseIcon || Kind == MavChipKind.Input;
        _closeHit.IsVisible = hasClose;

        var leftPad = hasAvatar ? 4.0 : (hasLead ? 8.0 : 12.0);
        var rightPad = hasClose ? 8.0 : 12.0;
        _surface.Padding = new Thickness(leftPad, 0, rightPad, 0);

        ApplyTheme(MavTheme.Current);
    }

    private void ApplyTheme(MavTheme t)
    {
        _label.Apply(MavTypeRole.LabelLarge, t.Typography);
        _surface.MinHeight = Math.Max(28, 32 + t.DensityOffset * 0.5);
        _surface.CornerRadius = t.Shape.Small;

        var c = t.Color;
        Color bg;
        Color fg;
        Color iconCol;
        Color stroke;
        double strokeW;
        double elev = IsElevated ? 1.0 : 0.0;

        if (!IsEnabled)
        {
            bg = IsElevated || IsSelected ? c.OnSurface.WithAlpha(t.StateLayer.DisabledContainer) : Colors.Transparent;
            fg = c.OnSurface.WithAlpha(t.StateLayer.DisabledContent);
            iconCol = fg;
            stroke = IsElevated || IsSelected ? Colors.Transparent : c.OnSurface.WithAlpha(t.StateLayer.DisabledContainer);
            strokeW = stroke.A > 0 ? 1 : 0;
            elev = 0;
        }
        else if (IsSelected)
        {
            bg = c.SecondaryContainer;
            fg = c.OnSecondaryContainer;
            iconCol = c.OnSecondaryContainer;
            stroke = Colors.Transparent;
            strokeW = 0;
        }
        else if (IsElevated)
        {
            bg = c.SurfaceContainerLow;
            fg = c.OnSurface;
            iconCol = c.Primary;
            stroke = Colors.Transparent;
            strokeW = 0;
        }
        else
        {
            bg = Colors.Transparent;
            fg = c.OnSurfaceVariant;
            iconCol = Kind == MavChipKind.Assist ? c.Primary : c.OnSurfaceVariant;
            stroke = c.OutlineVariant;
            strokeW = 1;
        }

        _surface.FillColor = bg;
        _surface.StrokeColor = stroke;
        _surface.StrokeThickness = strokeW;
        _surface.RippleColor = fg;
        _surface.Elevation = elev;
        _label.Foreground = MavTheme.BrushOf(fg);
        _leadingIcon.Color = iconCol;
        _closeIcon.Color = fg;
    }
}
