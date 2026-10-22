using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Empty / Placeholder State component with expressive tonal icon container,
/// headline, supporting description, primary action button, and optional secondary action button.
/// </summary>
public class MdEmptyState : MdContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdEmptyState, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MdEmptyState, string?>(nameof(Description));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdEmptyState, string?>(nameof(Icon), MdIcons.AllInbox);

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<MdEmptyState, string?>(nameof(ActionText));

    public static readonly StyledProperty<string?> SecondaryActionTextProperty =
        AvaloniaProperty.Register<MdEmptyState, string?>(nameof(SecondaryActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<MdEmptyState, ICommand?>(nameof(ActionCommand));

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<MdEmptyState, object?>(nameof(ActionCommandParameter));

    public static readonly StyledProperty<ICommand?> SecondaryActionCommandProperty =
        AvaloniaProperty.Register<MdEmptyState, ICommand?>(nameof(SecondaryActionCommand));

    public static readonly StyledProperty<object?> SecondaryActionCommandParameterProperty =
        AvaloniaProperty.Register<MdEmptyState, object?>(nameof(SecondaryActionCommandParameter));

    private readonly MdSurface _iconBadge;
    private readonly MdIcon _iconView;
    private readonly TextBlock _titleLbl;
    private readonly TextBlock _descLbl;
    private readonly StackPanel _actionsRow;
    private readonly MdButton _primaryBtn;
    private readonly MdButton _secondaryBtn;

    public event EventHandler? ActionClicked;
    public event EventHandler? SecondaryActionClicked;

    static MdEmptyState()
    {
        TitleProperty.Changed.AddClassHandler<MdEmptyState>((s, _) => s.Sync());
        DescriptionProperty.Changed.AddClassHandler<MdEmptyState>((s, _) => s.Sync());
        IconProperty.Changed.AddClassHandler<MdEmptyState>((s, _) => s.Sync());
        ActionTextProperty.Changed.AddClassHandler<MdEmptyState>((s, _) => s.Sync());
        SecondaryActionTextProperty.Changed.AddClassHandler<MdEmptyState>((s, _) => s.Sync());
        ActionCommandProperty.Changed.AddClassHandler<MdEmptyState>((s, e) =>
            s._primaryBtn.Command = e.GetNewValue<ICommand?>());
        ActionCommandParameterProperty.Changed.AddClassHandler<MdEmptyState>((s, e) =>
            s._primaryBtn.CommandParameter = e.GetNewValue<object?>());
        SecondaryActionCommandProperty.Changed.AddClassHandler<MdEmptyState>((s, e) =>
            s._secondaryBtn.Command = e.GetNewValue<ICommand?>());
        SecondaryActionCommandParameterProperty.Changed.AddClassHandler<MdEmptyState>((s, e) =>
            s._secondaryBtn.CommandParameter = e.GetNewValue<object?>());
    }

    public MdEmptyState() : this("Nothing here yet")
    {
    }

    public MdEmptyState(
        string title = "Nothing here yet",
        string? description = null,
        string? icon = MdIcons.AllInbox,
        string? actionText = null,
        string? secondaryActionText = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(24, 32);

        _iconView = new MdIcon(icon, 36) { UseThemeDefaultColor = false };
        _iconBadge = new MdSurface
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

        _primaryBtn = new MdButton(string.Empty, MdButtonVariant.Filled);
        _primaryBtn.Clicked += (_, _) => ActionClicked?.Invoke(this, EventArgs.Empty);

        _secondaryBtn = new MdButton(string.Empty, MdButtonVariant.Text);
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
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    private void ApplyTheme(MdTheme t)
    {
        Sync();
        var c = t.Color;
        _titleLbl.Apply(MdTypeRole.TitleLarge, t.Typography);
        _descLbl.Apply(MdTypeRole.BodyMedium, t.Typography);

        _titleLbl.Foreground = MdTheme.BrushOf(c.OnSurface);
        _descLbl.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);

        _iconBadge.Radius = t.Shape.ExtraLarge;
        _iconBadge.FillColor = c.SecondaryContainer;
        _iconView.Color = c.OnSecondaryContainer;
    }
}

public enum MdInfoBarSeverity
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
public class MdInfoBar : MdContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdInfoBar, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<MdInfoBar, string?>(nameof(Message));

    public static readonly StyledProperty<MdInfoBarSeverity> SeverityProperty =
        AvaloniaProperty.Register<MdInfoBar, MdInfoBarSeverity>(nameof(Severity), MdInfoBarSeverity.Info);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MdInfoBar, bool>(nameof(IsOpen), true, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsClosableProperty =
        AvaloniaProperty.Register<MdInfoBar, bool>(nameof(IsClosable), true);

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<MdInfoBar, string?>(nameof(ActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<MdInfoBar, ICommand?>(nameof(ActionCommand));

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<MdInfoBar, object?>(nameof(ActionCommandParameter));

    private readonly MdSurface _card;
    private readonly MdIcon _icon;
    private readonly TextBlock _titleLbl;
    private readonly TextBlock _msgLbl;
    private readonly MdButton _actionBtn;
    private readonly MdIconButton _closeBtn;

    public event EventHandler? ActionClicked;
    public event EventHandler? Closed;

    static MdInfoBar()
    {
        TitleProperty.Changed.AddClassHandler<MdInfoBar>((b, _) => b.ApplyTheme(MdTheme.Current));
        MessageProperty.Changed.AddClassHandler<MdInfoBar>((b, _) => b.ApplyTheme(MdTheme.Current));
        SeverityProperty.Changed.AddClassHandler<MdInfoBar>((b, _) => b.ApplyTheme(MdTheme.Current));
        IsClosableProperty.Changed.AddClassHandler<MdInfoBar>((b, _) => b.ApplyTheme(MdTheme.Current));
        ActionTextProperty.Changed.AddClassHandler<MdInfoBar>((b, _) => b.ApplyTheme(MdTheme.Current));
        ActionCommandProperty.Changed.AddClassHandler<MdInfoBar>((b, e) =>
            b._actionBtn.Command = e.GetNewValue<ICommand?>());
        ActionCommandParameterProperty.Changed.AddClassHandler<MdInfoBar>((b, e) =>
            b._actionBtn.CommandParameter = e.GetNewValue<object?>());
        IsOpenProperty.Changed.AddClassHandler<MdInfoBar>((b, e) =>
        {
            var open = e.GetNewValue<bool>();
            b.IsVisible = open;
            if (!open) b.Closed?.Invoke(b, EventArgs.Empty);
        });
    }

    public MdInfoBar() : this(string.Empty)
    {
    }

    public MdInfoBar(
        string title = "",
        string? message = null,
        MdInfoBarSeverity severity = MdInfoBarSeverity.Info,
        bool isClosable = true,
        string? actionText = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _icon = new MdIcon { Size = 22, UseThemeDefaultColor = false, VerticalAlignment = VerticalAlignment.Center };
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

        _actionBtn = new MdButton(string.Empty, MdButtonVariant.Text, size: MdButtonSize.ExtraSmall)
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0),
        };
        _actionBtn.Clicked += (_, _) => ActionClicked?.Invoke(this, EventArgs.Empty);

        _closeBtn = new MdIconButton(MdIcons.Close)
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

        _card = new MdSurface
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

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public MdInfoBarSeverity Severity
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

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _titleLbl.Text = Title;
        _titleLbl.IsVisible = !string.IsNullOrEmpty(Title);
        _titleLbl.Apply(MdTypeRole.TitleSmall, t.Typography);

        var hasMsg = !string.IsNullOrEmpty(Message);
        _msgLbl.Text = Message ?? string.Empty;
        _msgLbl.IsVisible = hasMsg;
        _msgLbl.Apply(MdTypeRole.BodyMedium, t.Typography);

        var hasAction = !string.IsNullOrEmpty(ActionText);
        _actionBtn.Text = ActionText ?? string.Empty;
        _actionBtn.IsVisible = hasAction;
        _closeBtn.IsVisible = IsClosable;

        var (bg, fg, border, glyph) = Severity switch
        {
            MdInfoBarSeverity.Success => (
                MdColorScheme.Blend(c.SurfaceContainerLow, Color.FromRgb(46, 125, 50), t.IsDark ? 0.25 : 0.14),
                t.IsDark ? Color.FromRgb(165, 214, 167) : Color.FromRgb(27, 94, 32),
                MdColorScheme.Blend(c.OutlineVariant, Color.FromRgb(46, 125, 50), 0.45),
                MdIcons.TaskAlt),
            MdInfoBarSeverity.Warning => (
                MdColorScheme.Blend(c.SurfaceContainerLow, Color.FromRgb(237, 108, 2), t.IsDark ? 0.25 : 0.15),
                t.IsDark ? Color.FromRgb(255, 204, 128) : Color.FromRgb(180, 75, 0),
                MdColorScheme.Blend(c.OutlineVariant, Color.FromRgb(237, 108, 2), 0.45),
                MdIcons.WarningAmber),
            MdInfoBarSeverity.Error => (
                c.ErrorContainer,
                c.OnErrorContainer,
                MdColorScheme.Blend(c.OutlineVariant, c.Error, 0.5),
                MdIcons.AlertCircleOutline),
            _ => (
                c.SecondaryContainer,
                c.OnSecondaryContainer,
                c.OutlineVariant,
                MdIcons.Announcement),
        };

        _card.Radius = t.Shape.Medium;
        _card.FillColor = bg;
        _card.BorderStrokeColor = border;
        _icon.Glyph = glyph;
        _icon.Color = fg;
        _titleLbl.Foreground = MdTheme.BrushOf(fg);
        _msgLbl.Foreground = MdTheme.BrushOf(fg.WithAlpha(0.88f));
    }
}
