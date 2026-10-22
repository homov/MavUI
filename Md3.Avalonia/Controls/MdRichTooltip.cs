using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Rich Tooltip (12dp radius, SurfaceContainer, optional subhead, supporting body, and action button).
/// </summary>
public class MdRichTooltip : Control
{
    public static readonly StyledProperty<string?> SubheadProperty =
        AvaloniaProperty.Register<MdRichTooltip, string?>(nameof(Subhead));

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdRichTooltip, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> ActionTextProperty =
        AvaloniaProperty.Register<MdRichTooltip, string?>(nameof(ActionText));

    public static readonly StyledProperty<ICommand?> ActionCommandProperty =
        AvaloniaProperty.Register<MdRichTooltip, ICommand?>(nameof(ActionCommand));

    public static readonly StyledProperty<object?> ActionCommandParameterProperty =
        AvaloniaProperty.Register<MdRichTooltip, object?>(nameof(ActionCommandParameter));

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

    protected override Size MeasureOverride(Size availableSize) => default;

    public void Show(Control anchor)
    {
        Dismiss();

        var overlayRoot = MdOverlay.EnsureOverlayRoot(anchor);
        if (overlayRoot is null) return;

        var t = MdTheme.Current;
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
            subLbl.Apply(MdTypeRole.TitleSmall, t.Typography);
            subLbl.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
            stack.Children.Add(subLbl);
        }

        var bodyLbl = new TextBlock
        {
            Text = Text,
            TextWrapping = TextWrapping.Wrap,
        };
        bodyLbl.Apply(MdTypeRole.BodyMedium, t.Typography);
        bodyLbl.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);
        stack.Children.Add(bodyLbl);

        if (!string.IsNullOrEmpty(ActionText))
        {
            var actBtn = new MdButton(ActionText!, MdButtonVariant.Text, size: MdButtonSize.ExtraSmall)
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
        var card = new MdSurface
        {
            MaxWidth = maxW,
            Radius = 12,
            Elevation = 2,
            Padding = new Thickness(16, 12),
            FillColor = c.SurfaceContainer,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Content = stack,
        };

        void Place()
        {
            var currentBounds = MdOverlay.BoundsInOverlay(anchor, overlayRoot);
            var currentSize = MdOverlay.GetOverlaySize(anchor);
            var insets = MdOverlay.SafeAreaOf(anchor);

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
        _handle = MdOverlay.Show(anchor, card, Colors.Transparent, Dismiss, onViewportChanged: Place);
        MdMotion.ScaleIn(card, 0.94, MdMotion.Short3);
    }

    public void Dismiss()
    {
        if (_handle is not null)
        {
            _handle.Dispose();
            _handle = null;
        }
    }

    public static MdRichTooltip Show(
        Control anchor,
        string? subhead,
        string text,
        string? actionText = null,
        Action? onAction = null)
    {
        var tip = new MdRichTooltip
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
