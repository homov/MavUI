using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 inline banner with leading icon, message, and up to two action buttons.
/// </summary>
public class MdBanner : MdContentView
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<MdBanner, string>(nameof(Message), string.Empty);

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdBanner, string?>(nameof(Icon), MdIcons.Info);

    public static readonly StyledProperty<string?> PrimaryActionTextProperty =
        AvaloniaProperty.Register<MdBanner, string?>(nameof(PrimaryActionText));

    public static readonly StyledProperty<string?> SecondaryActionTextProperty =
        AvaloniaProperty.Register<MdBanner, string?>(nameof(SecondaryActionText));

    public static readonly StyledProperty<ICommand?> PrimaryActionCommandProperty =
        AvaloniaProperty.Register<MdBanner, ICommand?>(nameof(PrimaryActionCommand));

    public static readonly StyledProperty<object?> PrimaryActionCommandParameterProperty =
        AvaloniaProperty.Register<MdBanner, object?>(nameof(PrimaryActionCommandParameter));

    public static readonly StyledProperty<ICommand?> SecondaryActionCommandProperty =
        AvaloniaProperty.Register<MdBanner, ICommand?>(nameof(SecondaryActionCommand));

    public static readonly StyledProperty<object?> SecondaryActionCommandParameterProperty =
        AvaloniaProperty.Register<MdBanner, object?>(nameof(SecondaryActionCommandParameter));

    private readonly MdSurface _card;
    private readonly MdIcon _icon;
    private readonly Border _iconCircle;
    private readonly TextBlock _messageLabel;
    private readonly MdButton _primaryBtn;
    private readonly MdButton _secondaryBtn;
    private readonly StackPanel _actionsRow;

    public event EventHandler? PrimaryActionClicked;
    public event EventHandler? SecondaryActionClicked;

    static MdBanner()
    {
        MessageProperty.Changed.AddClassHandler<MdBanner>((b, e) => b._messageLabel.Text = e.GetNewValue<string>());
        IconProperty.Changed.AddClassHandler<MdBanner>((b, e) =>
        {
            var val = e.GetNewValue<string?>();
            b._icon.Glyph = val;
            b._iconCircle.IsVisible = !string.IsNullOrEmpty(val);
        });
        PrimaryActionTextProperty.Changed.AddClassHandler<MdBanner>((b, _) => b.SyncActions());
        SecondaryActionTextProperty.Changed.AddClassHandler<MdBanner>((b, _) => b.SyncActions());
        PrimaryActionCommandProperty.Changed.AddClassHandler<MdBanner>((b, e) =>
            b._primaryBtn.Command = e.GetNewValue<ICommand?>());
        PrimaryActionCommandParameterProperty.Changed.AddClassHandler<MdBanner>((b, e) =>
            b._primaryBtn.CommandParameter = e.GetNewValue<object?>());
        SecondaryActionCommandProperty.Changed.AddClassHandler<MdBanner>((b, e) =>
            b._secondaryBtn.Command = e.GetNewValue<ICommand?>());
        SecondaryActionCommandParameterProperty.Changed.AddClassHandler<MdBanner>((b, e) =>
            b._secondaryBtn.CommandParameter = e.GetNewValue<object?>());
    }

    public MdBanner() : this(string.Empty)
    {
    }

    public MdBanner(
        string message = "",
        string? icon = MdIcons.Info,
        string? primaryAction = null,
        string? secondaryAction = null)
    {
        _icon = new MdIcon(icon, 22) { UseThemeDefaultColor = false };
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

        _secondaryBtn = new MdButton(secondaryAction ?? string.Empty, MdButtonVariant.Text)
        {
            IsVisible = !string.IsNullOrEmpty(secondaryAction),
        };
        _secondaryBtn.Clicked += (_, _) => SecondaryActionClicked?.Invoke(this, EventArgs.Empty);

        _primaryBtn = new MdButton(primaryAction ?? string.Empty, MdButtonVariant.Text)
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

        _card = new MdSurface
        {
            Radius = 12,
            BorderStrokeThickness = 1,
            Padding = new Thickness(16, 14, 16, 10),
            Content = stack,
        };

        Content = _card;
        Message = message;
        Icon = icon;
        if (primaryAction is not null) PrimaryActionText = primaryAction;
        if (secondaryAction is not null) SecondaryActionText = secondaryAction;
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public void Dismiss() => IsVisible = false;
    public void Show() => IsVisible = true;

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _card.FillColor = c.SurfaceContainerLow;
        _card.BorderStrokeColor = c.OutlineVariant;
        _iconCircle.Background = MdTheme.BrushOf(c.SecondaryContainer);
        _icon.Color = c.OnSecondaryContainer;
        _messageLabel.Apply(MdTypeRole.BodyMedium, t.Typography);
        _messageLabel.Foreground = MdTheme.BrushOf(c.OnSurface);
    }
}
