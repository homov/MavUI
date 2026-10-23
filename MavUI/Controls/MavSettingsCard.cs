using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Settings Card row with optional leading tonal icon badge, header, description,
/// trailing action control (e.g., <see cref="MavSwitch"/>, <see cref="MavSelect"/>, <see cref="MavButton"/>),
/// and optional clickable action mode.
/// </summary>
public class MavSettingsCard : MavContentView
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<MavSettingsCard, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MavSettingsCard, string?>(nameof(Description));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavSettingsCard, string?>(nameof(Icon));

    public static readonly StyledProperty<bool> IsActionableProperty =
        AvaloniaProperty.Register<MavSettingsCard, bool>(nameof(IsActionable));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavSettingsCard, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavSettingsCard, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<Control?> ActionContentProperty =
        AvaloniaProperty.Register<MavSettingsCard, Control?>(nameof(ActionContent));

    private readonly MavInkSurface _surface;
    private readonly MavSurface _iconBox;
    private readonly MavIcon _iconView;
    private readonly TextBlock _headerLbl;
    private readonly TextBlock _descLbl;
    private readonly Border _actionHost;
    private readonly MavIcon _chevron;

    public event EventHandler? Clicked;

    static MavSettingsCard()
    {
        HeaderProperty.Changed.AddClassHandler<MavSettingsCard>((c, _) => c.Sync());
        DescriptionProperty.Changed.AddClassHandler<MavSettingsCard>((c, _) => c.Sync());
        IconProperty.Changed.AddClassHandler<MavSettingsCard>((c, _) => c.Sync());
        IsActionableProperty.Changed.AddClassHandler<MavSettingsCard>((c, _) => c.Sync());
        CommandProperty.Changed.AddClassHandler<MavSettingsCard>((c, e) => c._surface.Command = e.GetNewValue<ICommand?>());
        CommandParameterProperty.Changed.AddClassHandler<MavSettingsCard>((c, e) => c._surface.CommandParameter = e.GetNewValue<object?>());
        ActionContentProperty.Changed.AddClassHandler<MavSettingsCard>((c, e) =>
        {
            var ctrl = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(ctrl);
            c._actionHost.Child = ctrl;
            c.Sync();
        });
    }

    /// <summary>Creates an empty settings card.</summary>
    public MavSettingsCard() : this(string.Empty)
    {
    }

    /// <summary>Creates a settings card with a title, description, icon and optional trailing control.</summary>
    public MavSettingsCard(
        string header = "",
        string? description = null,
        string? icon = null,
        Control? actionContent = null,
        bool isActionable = false)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _iconView = new MavIcon { Size = 22, UseThemeDefaultColor = false };
        _iconBox = new MavSurface
        {
            Width = 40,
            Height = 40,
            Radius = 12,
            Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
            Child = _iconView,
        };

        _headerLbl = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
        };
        _descLbl = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false,
        };

        var textCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        textCol.Children.Add(_headerLbl);
        textCol.Children.Add(_descLbl);

        _actionHost = new Border
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
            IsVisible = false,
        };

        _chevron = new MavIcon(MavIcons.ChevronRight, 22)
        {
            UseThemeDefaultColor = false,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false,
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Add(_iconBox, 0, 0);
        row.Add(textCol, 1, 0);
        row.Add(_actionHost, 2, 0);
        row.Add(_chevron, 3, 0);

        _surface = new MavInkSurface
        {
            MinHeight = 68,
            CornerRadius = 16,
            Padding = new Thickness(16, 14),
            StrokeThickness = 1,
            Child = row,
        };
        _surface.Clicked += (_, _) =>
        {
            if (IsActionable)
                Clicked?.Invoke(this, EventArgs.Empty);
        };

        Child = _surface;

        Header = header;
        Description = description;
        Icon = icon;
        ActionContent = actionContent;
        IsActionable = isActionable;

        Sync();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Title
    {
        get => Header;
        set => Header = value;
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsActionable
    {
        get => GetValue(IsActionableProperty);
        set => SetValue(IsActionableProperty, value);
    }

    public bool IsActionCard
    {
        get => IsActionable;
        set => IsActionable = value;
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

    [Content]
    public Control? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    internal MavInkSurface InnerSurface => _surface;

    private void Sync()
    {
        _headerLbl.Text = Header;

        var hasDesc = !string.IsNullOrEmpty(Description);
        _descLbl.Text = Description ?? string.Empty;
        _descLbl.IsVisible = hasDesc;

        var hasIcon = !string.IsNullOrEmpty(Icon);
        if (hasIcon) _iconView.Glyph = Icon;
        _iconBox.IsVisible = hasIcon;

        var actionCtrl = ActionContent;
        _actionHost.IsVisible = actionCtrl is not null;
        _chevron.IsVisible = IsActionable && actionCtrl is null;
        _surface.IsInteractive = IsActionable;
    }

    private void ApplyTheme(MavTheme t)
    {
        Sync();
        var c = t.Color;
        _headerLbl.Apply(MavTypeRole.TitleMedium, t.Typography);
        _descLbl.Apply(MavTypeRole.BodyMedium, t.Typography);

        _headerLbl.Foreground = MavTheme.BrushOf(c.OnSurface);
        _descLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _iconBox.FillColor = c.SecondaryContainer;
        _iconView.Color = c.OnSecondaryContainer;
        _chevron.Color = c.OnSurfaceVariant;

        _surface.CornerRadius = t.Shape.Large;
        _surface.FillColor = c.SurfaceContainerLow;
        _surface.StrokeColor = c.OutlineVariant;
        _surface.RippleColor = c.OnSurface;
    }
}
