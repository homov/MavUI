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

    /// <summary>Creates an empty state with the default message.</summary>
    public MavEmptyState() : this("Nothing here yet")
    {
    }

    /// <summary>Creates an empty state with an icon, title, description and optional action.</summary>
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
            Child = _iconView,
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

        Child = col;

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
