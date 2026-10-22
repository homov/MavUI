using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Empty / Placeholder State component with expressive tonal icon container,
/// headline, supporting description, primary action button, and optional secondary action button.
/// </summary>
public class MavEmptyState : MavContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavEmptyState, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MavEmptyState, string?>(nameof(Description));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavEmptyState, string?>(nameof(Icon), MavIcons.AllInbox);

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<MavEmptyState, string?>(nameof(ActionText));

    public static readonly StyledProperty<string?> SecondaryActionTextProperty =
        AvaloniaProperty.Register<MavEmptyState, string?>(nameof(SecondaryActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<MavEmptyState, ICommand?>(nameof(ActionCommand));

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<MavEmptyState, object?>(nameof(ActionCommandParameter));

    public static readonly StyledProperty<ICommand?> SecondaryActionCommandProperty =
        AvaloniaProperty.Register<MavEmptyState, ICommand?>(nameof(SecondaryActionCommand));

    public static readonly StyledProperty<object?> SecondaryActionCommandParameterProperty =
        AvaloniaProperty.Register<MavEmptyState, object?>(nameof(SecondaryActionCommandParameter));

    private readonly MavSurface _iconBadge;
    private readonly MavIcon _iconView;
    private readonly TextBlock _titleLbl;
    private readonly TextBlock _descLbl;
    private readonly StackPanel _actionsRow;
    private readonly MavButton _primaryBtn;
    private readonly MavButton _secondaryBtn;

    public event EventHandler? ActionClicked;
    public event EventHandler? SecondaryActionClicked;

    static MavEmptyState()
    {
        TitleProperty.Changed.AddClassHandler<MavEmptyState>((s, _) => s.Sync());
        DescriptionProperty.Changed.AddClassHandler<MavEmptyState>((s, _) => s.Sync());
        IconProperty.Changed.AddClassHandler<MavEmptyState>((s, _) => s.Sync());
        ActionTextProperty.Changed.AddClassHandler<MavEmptyState>((s, _) => s.Sync());
        SecondaryActionTextProperty.Changed.AddClassHandler<MavEmptyState>((s, _) => s.Sync());
        ActionCommandProperty.Changed.AddClassHandler<MavEmptyState>((s, e) =>
            s._primaryBtn.Command = e.GetNewValue<ICommand?>());
        ActionCommandParameterProperty.Changed.AddClassHandler<MavEmptyState>((s, e) =>
            s._primaryBtn.CommandParameter = e.GetNewValue<object?>());
        SecondaryActionCommandProperty.Changed.AddClassHandler<MavEmptyState>((s, e) =>
            s._secondaryBtn.Command = e.GetNewValue<ICommand?>());
        SecondaryActionCommandParameterProperty.Changed.AddClassHandler<MavEmptyState>((s, e) =>
            s._secondaryBtn.CommandParameter = e.GetNewValue<object?>());
    }

    public MavEmptyState() : this("Nothing here yet")
    {
    }

    public MavEmptyState(
        string title = "Nothing here yet",
        string? description = null,
        string? icon = MavIcons.AllInbox,
        string? actionText = null,
        string? secondaryActionText = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(24, 32);

        _iconView = new MavIcon(icon, 36) { UseThemeDefaultColor = false };
        _iconBadge = new MavSurface
        {
            Width = 76,
            Height = 76,
            Radius = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
            Content = _iconView,
        };

        _titleLbl = new TextBlock
        {
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };

        _descLbl = new TextBlock
        {
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 380,
        };

        _primaryBtn = new MavButton(string.Empty, MavButtonVariant.Filled);
        _primaryBtn.Clicked += (_, _) => ActionClicked?.Invoke(this, EventArgs.Empty);

        _secondaryBtn = new MavButton(string.Empty, MavButtonVariant.Text);
        _secondaryBtn.Clicked += (_, _) => SecondaryActionClicked?.Invoke(this, EventArgs.Empty);

        _actionsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
        };
        _actionsRow.Children.Add(_primaryBtn);
        _actionsRow.Children.Add(_secondaryBtn);

        var col = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        col.Children.Add(_iconBadge);
        col.Children.Add(_titleLbl);
        col.Children.Add(_descLbl);
        col.Children.Add(_actionsRow);

        Content = col;

        Title = title;
        Description = description;
        Icon = icon;
        ActionText = actionText;
        SecondaryActionText = secondaryActionText;

        Sync();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
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

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public string? SecondaryActionText
    {
        get => GetValue(SecondaryActionTextProperty);
        set => SetValue(SecondaryActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public object? ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    public ICommand? SecondaryActionCommand
    {
        get => GetValue(SecondaryActionCommandProperty);
        set => SetValue(SecondaryActionCommandProperty, value);
    }

    public object? SecondaryActionCommandParameter
    {
        get => GetValue(SecondaryActionCommandParameterProperty);
        set => SetValue(SecondaryActionCommandParameterProperty, value);
    }

    private void Sync()
    {
        _titleLbl.Text = Title;
        _titleLbl.IsVisible = !string.IsNullOrEmpty(Title);

        var hasDesc = !string.IsNullOrEmpty(Description);
        _descLbl.Text = Description ?? string.Empty;
        _descLbl.IsVisible = hasDesc;

        var hasIcon = !string.IsNullOrEmpty(Icon);
        if (hasIcon) _iconView.Glyph = Icon;
        _iconBadge.IsVisible = hasIcon;

        var hasPrimary = !string.IsNullOrEmpty(ActionText);
        _primaryBtn.Text = ActionText ?? string.Empty;
        _primaryBtn.IsVisible = hasPrimary;

        var hasSecondary = !string.IsNullOrEmpty(SecondaryActionText);
        _secondaryBtn.Text = SecondaryActionText ?? string.Empty;
        _secondaryBtn.IsVisible = hasSecondary;

        _actionsRow.IsVisible = hasPrimary || hasSecondary;
    }

    private void ApplyTheme(MavTheme t)
    {
        Sync();
        var c = t.Color;
        _titleLbl.Apply(MavTypeRole.TitleLarge, t.Typography);
        _descLbl.Apply(MavTypeRole.BodyMedium, t.Typography);

        _titleLbl.Foreground = MavTheme.BrushOf(c.OnSurface);
        _descLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _iconBadge.Radius = t.Shape.ExtraLarge;
        _iconBadge.FillColor = c.SecondaryContainer;
        _iconView.Color = c.OnSecondaryContainer;
    }
}

public enum MavInfoBarSeverity
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>
/// Inline Material Design 3 status alert bar with semantic container color, leading icon,
/// title, supporting message, optional action button, and optional close button.
/// </summary>
public class MavInfoBar : MavContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavInfoBar, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<MavInfoBar, string?>(nameof(Message));

    public static readonly StyledProperty<MavInfoBarSeverity> SeverityProperty =
        AvaloniaProperty.Register<MavInfoBar, MavInfoBarSeverity>(nameof(Severity), MavInfoBarSeverity.Info);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavInfoBar, bool>(nameof(IsOpen), true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsClosableProperty =
        AvaloniaProperty.Register<MavInfoBar, bool>(nameof(IsClosable), true);

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<MavInfoBar, string?>(nameof(ActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<MavInfoBar, ICommand?>(nameof(ActionCommand));

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<MavInfoBar, object?>(nameof(ActionCommandParameter));

    private readonly MavSurface _card;
    private readonly MavIcon _icon;
    private readonly TextBlock _titleLbl;
    private readonly TextBlock _msgLbl;
    private readonly MavButton _actionBtn;
    private readonly MavIconButton _closeBtn;

    public event EventHandler? ActionClicked;
    public event EventHandler? Closed;

    static MavInfoBar()
    {
        TitleProperty.Changed.AddClassHandler<MavInfoBar>((b, _) => b.ApplyTheme(MavTheme.Current));
        MessageProperty.Changed.AddClassHandler<MavInfoBar>((b, _) => b.ApplyTheme(MavTheme.Current));
        SeverityProperty.Changed.AddClassHandler<MavInfoBar>((b, _) => b.ApplyTheme(MavTheme.Current));
        IsClosableProperty.Changed.AddClassHandler<MavInfoBar>((b, _) => b.ApplyTheme(MavTheme.Current));
        ActionTextProperty.Changed.AddClassHandler<MavInfoBar>((b, _) => b.ApplyTheme(MavTheme.Current));
        ActionCommandProperty.Changed.AddClassHandler<MavInfoBar>((b, e) =>
            b._actionBtn.Command = e.GetNewValue<ICommand?>());
        ActionCommandParameterProperty.Changed.AddClassHandler<MavInfoBar>((b, e) =>
            b._actionBtn.CommandParameter = e.GetNewValue<object?>());
        IsOpenProperty.Changed.AddClassHandler<MavInfoBar>((b, e) =>
        {
            var open = e.GetNewValue<bool>();
            b.IsVisible = open;
            if (!open) b.Closed?.Invoke(b, EventArgs.Empty);
        });
    }

    public MavInfoBar() : this(string.Empty)
    {
    }

    public MavInfoBar(
        string title = "",
        string? message = null,
        MavInfoBarSeverity severity = MavInfoBarSeverity.Info,
        bool isClosable = true,
        string? actionText = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _icon = new MavIcon { Size = 22, UseThemeDefaultColor = false, VerticalAlignment = VerticalAlignment.Center };
        _titleLbl = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _msgLbl = new TextBlock { TextWrapping = TextWrapping.Wrap };

        var textCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0),
        };
        textCol.Children.Add(_titleLbl);
        textCol.Children.Add(_msgLbl);

        _actionBtn = new MavButton(string.Empty, MavButtonVariant.Text, size: MavButtonSize.ExtraSmall)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0),
        };
        _actionBtn.Clicked += (_, _) => ActionClicked?.Invoke(this, EventArgs.Empty);

        _closeBtn = new MavIconButton(MavIcons.Close)
        {
            ButtonSize = 32,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _closeBtn.Clicked += (_, _) => IsOpen = false;

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Add(_icon, 0, 0);
        row.Add(textCol, 1, 0);
        row.Add(_actionBtn, 2, 0);
        row.Add(_closeBtn, 3, 0);

        _card = new MavSurface
        {
            Radius = 14,
            BorderStrokeThickness = 1,
            Padding = new Thickness(16, 12),
            Content = row,
        };

        Content = _card;

        Title = title;
        Message = message;
        Severity = severity;
        IsClosable = isClosable;
        ActionText = actionText;

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public MavInfoBarSeverity Severity
    {
        get => GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public bool IsClosable
    {
        get => GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public ICommand? ActionCommand
    {
        get => GetValue(ActionCommandProperty);
        set => SetValue(ActionCommandProperty, value);
    }

    public object? ActionCommandParameter
    {
        get => GetValue(ActionCommandParameterProperty);
        set => SetValue(ActionCommandParameterProperty, value);
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _titleLbl.Text = Title;
        _titleLbl.IsVisible = !string.IsNullOrEmpty(Title);
        _titleLbl.Apply(MavTypeRole.TitleSmall, t.Typography);

        var hasMsg = !string.IsNullOrEmpty(Message);
        _msgLbl.Text = Message ?? string.Empty;
        _msgLbl.IsVisible = hasMsg;
        _msgLbl.Apply(MavTypeRole.BodyMedium, t.Typography);

        var hasAction = !string.IsNullOrEmpty(ActionText);
        _actionBtn.Text = ActionText ?? string.Empty;
        _actionBtn.IsVisible = hasAction;
        _closeBtn.IsVisible = IsClosable;

        var (bg, fg, border, glyph) = Severity switch
        {
            MavInfoBarSeverity.Success => (
                MavColorScheme.Blend(c.SurfaceContainerLow, Color.FromRgb(46, 125, 50), t.IsDark ? 0.25 : 0.14),
                t.IsDark ? Color.FromRgb(165, 214, 167) : Color.FromRgb(27, 94, 32),
                MavColorScheme.Blend(c.OutlineVariant, Color.FromRgb(46, 125, 50), 0.45),
                MavIcons.TaskAlt),
            MavInfoBarSeverity.Warning => (
                MavColorScheme.Blend(c.SurfaceContainerLow, Color.FromRgb(237, 108, 2), t.IsDark ? 0.25 : 0.15),
                t.IsDark ? Color.FromRgb(255, 204, 128) : Color.FromRgb(180, 75, 0),
                MavColorScheme.Blend(c.OutlineVariant, Color.FromRgb(237, 108, 2), 0.45),
                MavIcons.WarningAmber),
            MavInfoBarSeverity.Error => (
                c.ErrorContainer,
                c.OnErrorContainer,
                MavColorScheme.Blend(c.OutlineVariant, c.Error, 0.5),
                MavIcons.AlertCircleOutline),
            _ => (
                c.SecondaryContainer,
                c.OnSecondaryContainer,
                c.OutlineVariant,
                MavIcons.Announcement),
        };

        _card.Radius = t.Shape.Medium;
        _card.FillColor = bg;
        _card.BorderStrokeColor = border;
        _icon.Glyph = glyph;
        _icon.Color = fg;
        _titleLbl.Foreground = MavTheme.BrushOf(fg);
        _msgLbl.Foreground = MavTheme.BrushOf(fg.WithAlpha(0.88f));
    }
}
