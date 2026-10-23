using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Rich Tooltip (12dp radius, SurfaceContainer, optional subhead, supporting body, and action button).
/// </summary>
public class MavRichTooltip : Control
{
    public static readonly StyledProperty<string?> SubheadProperty =
        AvaloniaProperty.Register<MavRichTooltip, string?>(nameof(Subhead));

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavRichTooltip, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<MavRichTooltip, string?>(nameof(ActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<MavRichTooltip, ICommand?>(nameof(ActionCommand));

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<MavRichTooltip, object?>(nameof(ActionCommandParameter));

    private IDisposable? _handle;

    public string? Subhead
    {
        get => GetValue(SubheadProperty);
        set => SetValue(SubheadProperty, value);
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string SupportingText
    {
        get => Text;
        set => Text = value;
    }

    public string? ActionText
    {
        get => GetValue(ActionTextProperty);
        set => SetValue(ActionTextProperty, value);
    }

    public Action? OnAction { get; set; }
    public event EventHandler? ActionClicked;

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

    /// <summary>
    /// Measures the tooltip during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Shows the tooltip next to <paramref name="anchor"/>.</summary>
    public void Show(Control anchor)
    {
        Dismiss();

        var overlayRoot = MavOverlay.EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return;

        var t = MavTheme.Current;
        var c = t.Color;

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
        };

        if (!string.IsNullOrEmpty(Subhead))
        {
            var subLbl = new TextBlock
            {
                Text = Subhead,
                TextWrapping = TextWrapping.Wrap,
            };
            subLbl.Apply(MavTypeRole.TitleSmall, t.Typography);
            subLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
            stack.Children.Add(subLbl);
        }

        var bodyLbl = new TextBlock
        {
            Text = Text,
            TextWrapping = TextWrapping.Wrap,
        };
        bodyLbl.Apply(MavTypeRole.BodyMedium, t.Typography);
        bodyLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        stack.Children.Add(bodyLbl);

        if (!string.IsNullOrEmpty(ActionText))
        {
            var actBtn = new MavButton(ActionText!, MavButtonVariant.Text, size: MavButtonSize.ExtraSmall)
            {
                Margin = new Thickness(-8, 4, 0, -4),
            };
            actBtn.Clicked += (_, _) =>
            {
                Dismiss();
                OnAction?.Invoke();
                ActionClicked?.Invoke(this, EventArgs.Empty);
                if (ActionCommand is { } cmd && cmd.CanExecute(ActionCommandParameter))
                    cmd.Execute(ActionCommandParameter);
            };
            stack.Children.Add(actBtn);
        }

        const double maxW = 312;
        var card = new MavSurface
        {
            MaxWidth = maxW,
            Radius = 12,
            Elevation = 2,
            Padding = new Thickness(16, 12),
            FillColor = c.SurfaceContainer,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Child = stack,
        };

        void Place()
        {
            var currentBounds = MavOverlay.BoundsInOverlay(anchor, overlayRoot);
            var currentSize = MavOverlay.GetOverlaySize(anchor);
            var insets = MavOverlay.SafeAreaOf(anchor);

            var viewportWidth = Math.Max(0, currentSize.Width - insets.Left - insets.Right);
            var maxCardWidth = Math.Max(120, viewportWidth - 24);
            card.MaxWidth = Math.Min(maxW, maxCardWidth);

            var x = Math.Clamp(
                currentBounds.X,
                insets.Left + 12,
                Math.Max(insets.Left + 12, currentSize.Width - insets.Right - card.MaxWidth - 12));
            var y = currentBounds.Bottom + 130 <= currentSize.Height - insets.Bottom
                ? currentBounds.Bottom + 8
                : Math.Max(insets.Top + 12, currentBounds.Y - 128);

            y = Math.Min(y, Math.Max(insets.Top + 12, currentSize.Height - insets.Bottom - 140));
            card.Margin = new Thickness(x, y, 0, 0);
        }

        Place();
        _handle = MavOverlay.Show(anchor, card, Colors.Transparent, Dismiss, onViewportChanged: Place);
        MavMotion.ScaleIn(card, 0.94, MavMotion.Short3);
    }

    /// <summary>Hides the tooltip.</summary>
    public void Dismiss()
    {
        if (_handle is not null)
        {
            _handle.Dispose();
            _handle = null;
        }
    }

    /// <summary>Creates a rich tooltip with the given content, shows it next to <paramref name="anchor"/> and returns it.</summary>
    public static MavRichTooltip Show(
        Control anchor,
        string? subhead,
        string text,
        string? actionText = null,
        Action? onAction = null)
    {
        var tip = new MavRichTooltip
        {
            Subhead = subhead,
            Text = text,
            ActionText = actionText,
            OnAction = onAction,
        };
        tip.Show(anchor);
        return tip;
    }
}
