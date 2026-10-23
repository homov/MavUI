using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 inline banner with leading icon, message, and up to two action buttons.
/// </summary>
public class MavBanner : MavContentView
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<MavBanner, string>(nameof(Message), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavBanner, string?>(nameof(Icon), MavIcons.Info);

    public static readonly StyledProperty<string?> PrimaryActionTextProperty =
        AvaloniaProperty.Register<MavBanner, string?>(nameof(PrimaryActionText));

    public static readonly StyledProperty<string?> SecondaryActionTextProperty =
        AvaloniaProperty.Register<MavBanner, string?>(nameof(SecondaryActionText));

    public static readonly StyledProperty<ICommand?> PrimaryActionCommandProperty =
        AvaloniaProperty.Register<MavBanner, ICommand?>(nameof(PrimaryActionCommand));

    public static readonly StyledProperty<object?> PrimaryActionCommandParameterProperty =
        AvaloniaProperty.Register<MavBanner, object?>(nameof(PrimaryActionCommandParameter));

    public static readonly StyledProperty<ICommand?> SecondaryActionCommandProperty =
        AvaloniaProperty.Register<MavBanner, ICommand?>(nameof(SecondaryActionCommand));

    public static readonly StyledProperty<object?> SecondaryActionCommandParameterProperty =
        AvaloniaProperty.Register<MavBanner, object?>(nameof(SecondaryActionCommandParameter));

    private readonly MavSurface _card;
    private readonly MavIcon _icon;
    private readonly Border _iconCircle;
    private readonly TextBlock _messageLabel;
    private readonly MavButton _primaryBtn;
    private readonly MavButton _secondaryBtn;
    private readonly StackPanel _actionsRow;

    public event EventHandler? PrimaryActionClicked;
    public event EventHandler? SecondaryActionClicked;

    static MavBanner()
    {
        MessageProperty.Changed.AddClassHandler<MavBanner>((b, e) => b._messageLabel.Text = e.GetNewValue<string>());
        IconProperty.Changed.AddClassHandler<MavBanner>((b, e) =>
        {
            var val = e.GetNewValue<string?>();
            b._icon.Glyph = val;
            b._iconCircle.IsVisible = !string.IsNullOrEmpty(val);
        });
        PrimaryActionTextProperty.Changed.AddClassHandler<MavBanner>((b, _) => b.SyncActions());
        SecondaryActionTextProperty.Changed.AddClassHandler<MavBanner>((b, _) => b.SyncActions());
        PrimaryActionCommandProperty.Changed.AddClassHandler<MavBanner>((b, e) =>
            b._primaryBtn.Command = e.GetNewValue<ICommand?>());
        PrimaryActionCommandParameterProperty.Changed.AddClassHandler<MavBanner>((b, e) =>
            b._primaryBtn.CommandParameter = e.GetNewValue<object?>());
        SecondaryActionCommandProperty.Changed.AddClassHandler<MavBanner>((b, e) =>
            b._secondaryBtn.Command = e.GetNewValue<ICommand?>());
        SecondaryActionCommandParameterProperty.Changed.AddClassHandler<MavBanner>((b, e) =>
            b._secondaryBtn.CommandParameter = e.GetNewValue<object?>());
    }

    /// <summary>Creates an empty banner.</summary>
    public MavBanner() : this(string.Empty)
    {
    }

    /// <summary>Creates a banner with the given message and optional actions.</summary>
    public MavBanner(
        string message = "",
        string? icon = MavIcons.Info,
        string? primaryAction = null,
        string? secondaryAction = null)
    {
        _icon = new MavIcon(icon, 22) { UseThemeDefaultColor = false };
        _iconCircle = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 16, 0),
            IsVisible = !string.IsNullOrEmpty(icon),
            Child = _icon,
        };

        _messageLabel = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
        };
        topRow.Add(_iconCircle, 0, 0);
        topRow.Add(_messageLabel, 1, 0);

        _secondaryBtn = new MavButton(secondaryAction ?? string.Empty, MavButtonVariant.Text)
        {
            IsVisible = !string.IsNullOrEmpty(secondaryAction),
        };
        _secondaryBtn.Clicked += (_, _) => SecondaryActionClicked?.Invoke(this, EventArgs.Empty);

        _primaryBtn = new MavButton(primaryAction ?? string.Empty, MavButtonVariant.Text)
        {
            IsVisible = !string.IsNullOrEmpty(primaryAction),
        };
        _primaryBtn.Clicked += (_, _) => PrimaryActionClicked?.Invoke(this, EventArgs.Empty);

        _actionsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            IsVisible = !string.IsNullOrEmpty(primaryAction) || !string.IsNullOrEmpty(secondaryAction),
        };
        _actionsRow.Children.Add(_secondaryBtn);
        _actionsRow.Children.Add(_primaryBtn);

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
        };
        stack.Children.Add(topRow);
        stack.Children.Add(_actionsRow);

        _card = new MavSurface
        {
            Radius = 12,
            BorderStrokeThickness = 1,
            Padding = new Thickness(16, 14, 16, 10),
            Child = stack,
        };

        Child = _card;
        Message = message;
        Icon = icon;
        if (primaryAction is not null) PrimaryActionText = primaryAction;
        if (secondaryAction is not null) SecondaryActionText = secondaryAction;
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string Text
    {
        get => Message;
        set => Message = value;
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

    public string? PrimaryActionText
    {
        get => GetValue(PrimaryActionTextProperty);
        set => SetValue(PrimaryActionTextProperty, value);
    }

    public string? SecondaryActionText
    {
        get => GetValue(SecondaryActionTextProperty);
        set => SetValue(SecondaryActionTextProperty, value);
    }

    public ICommand? PrimaryActionCommand
    {
        get => GetValue(PrimaryActionCommandProperty);
        set => SetValue(PrimaryActionCommandProperty, value);
    }

    public object? PrimaryActionCommandParameter
    {
        get => GetValue(PrimaryActionCommandParameterProperty);
        set => SetValue(PrimaryActionCommandParameterProperty, value);
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

    private void SyncActions()
    {
        _primaryBtn.Text = PrimaryActionText ?? string.Empty;
        _primaryBtn.IsVisible = !string.IsNullOrEmpty(PrimaryActionText);
        _secondaryBtn.Text = SecondaryActionText ?? string.Empty;
        _secondaryBtn.IsVisible = !string.IsNullOrEmpty(SecondaryActionText);
        _actionsRow.IsVisible = _primaryBtn.IsVisible || _secondaryBtn.IsVisible;
    }

    /// <summary>
    /// Hides the banner.
    /// </summary>
    public void Dismiss() => IsVisible = false;
    /// <summary>
    /// Shows the banner.
    /// </summary>
    public void Show() => IsVisible = true;

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _card.FillColor = c.SurfaceContainerLow;
        _card.BorderStrokeColor = c.OutlineVariant;
        _iconCircle.Background = MavTheme.BrushOf(c.SecondaryContainer);
        _icon.Color = c.OnSecondaryContainer;
        _messageLabel.Apply(MavTypeRole.BodyMedium, t.Typography);
        _messageLabel.Foreground = MavTheme.BrushOf(c.OnSurface);
    }
}
