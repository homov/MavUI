using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 modal dialog (28dp rounded surface, optional hero icon, headline,
/// supporting text, custom body, and action buttons) plus fullscreen dialog helper.
/// </summary>
public class MavDialog : Control
{
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MavDialog, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MavDialog, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<MavDialog, string?>(nameof(Message));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<MavDialog, object?>(nameof(Content));

    public static readonly StyledProperty<string?> ConfirmTextProperty =
        AvaloniaProperty.Register<MavDialog, string?>(nameof(ConfirmText));

    public static readonly StyledProperty<string?> DismissTextProperty =
        AvaloniaProperty.Register<MavDialog, string?>(nameof(DismissText));

    public static readonly StyledProperty<string?> NeutralTextProperty =
        AvaloniaProperty.Register<MavDialog, string?>(nameof(NeutralText));

    public static readonly StyledProperty<bool> DismissOnScrimTapProperty =
        AvaloniaProperty.Register<MavDialog, bool>(nameof(DismissOnScrimTap), true);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavDialog, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> ConfirmCommandProperty =
        AvaloniaProperty.Register<MavDialog, ICommand?>(nameof(ConfirmCommand));

    public static readonly StyledProperty<object?> ConfirmCommandParameterProperty =
        AvaloniaProperty.Register<MavDialog, object?>(nameof(ConfirmCommandParameter));

    public static readonly StyledProperty<ICommand?> DismissCommandProperty =
        AvaloniaProperty.Register<MavDialog, ICommand?>(nameof(DismissCommand));

    public static readonly StyledProperty<object?> DismissCommandParameterProperty =
        AvaloniaProperty.Register<MavDialog, object?>(nameof(DismissCommandParameter));

    public static readonly StyledProperty<ICommand?> NeutralCommandProperty =
        AvaloniaProperty.Register<MavDialog, ICommand?>(nameof(NeutralCommand));

    public static readonly StyledProperty<object?> NeutralCommandParameterProperty =
        AvaloniaProperty.Register<MavDialog, object?>(nameof(NeutralCommandParameter));

    private IDisposable? _handle;
    private bool _syncingIsOpen;

    static MavDialog()
    {
        ContentProperty.Changed.AddClassHandler<MavDialog>((d, e) =>
            MavVisualTreeHelpers.AttachLogicalChild(d, e.GetNewValue<object?>() as Control));
        IsOpenProperty.Changed.AddClassHandler<MavDialog>((d, e) =>
        {
            if (d._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                d.Show(d.Parent as Visual ?? d);
            else
                d.Close();
        });
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string? HeroIcon
    {
        get => Icon;
        set => Icon = value;
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Headline
    {
        get => Title;
        set => Title = value;
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string? SupportingText
    {
        get => Message;
        set => Message = value;
    }

    /// <summary>The body content. Any object is accepted; non-control values are shown through a ContentPresenter.</summary>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public string? ConfirmText
    {
        get => GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    public string? DismissText
    {
        get => GetValue(DismissTextProperty);
        set => SetValue(DismissTextProperty, value);
    }

    public string? NeutralText
    {
        get => GetValue(NeutralTextProperty);
        set => SetValue(NeutralTextProperty, value);
    }

    public bool DismissOnScrimTap
    {
        get => GetValue(DismissOnScrimTapProperty);
        set => SetValue(DismissOnScrimTapProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public ICommand? ConfirmCommand
    {
        get => GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    public object? ConfirmCommandParameter
    {
        get => GetValue(ConfirmCommandParameterProperty);
        set => SetValue(ConfirmCommandParameterProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    public object? DismissCommandParameter
    {
        get => GetValue(DismissCommandParameterProperty);
        set => SetValue(DismissCommandParameterProperty, value);
    }

    public ICommand? NeutralCommand
    {
        get => GetValue(NeutralCommandProperty);
        set => SetValue(NeutralCommandProperty, value);
    }

    public object? NeutralCommandParameter
    {
        get => GetValue(NeutralCommandParameterProperty);
        set => SetValue(NeutralCommandParameterProperty, value);
    }

    public event EventHandler? Confirmed;
    public event EventHandler? Dismissed;
    public event EventHandler? NeutralClicked;
    public event EventHandler? Closed;

    /// <summary>
    /// Measures the dialog during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Shows the dialog over <paramref name="anchor"/>.</summary>
    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MavTheme.Current;
        var c = t.Color;
        var confirmText = ConfirmText ?? MavLocalization.Ok;

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 16,
        };

        MavIcon? iconView = null;
        var hasIcon = !string.IsNullOrEmpty(Icon);
        if (hasIcon)
        {
            iconView = new MavIcon(Icon, 24)
            {
                UseThemeDefaultColor = false,
                Color = c.Secondary,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            stack.Children.Add(iconView);
        }

        TextBlock? titleLbl = null;
        if (!string.IsNullOrEmpty(Title))
        {
            titleLbl = new TextBlock
            {
                Text = Title,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = hasIcon ? TextAlignment.Center : TextAlignment.Left,
                HorizontalAlignment = hasIcon ? HorizontalAlignment.Center : HorizontalAlignment.Left,
            };
            titleLbl.Apply(MavTypeRole.HeadlineSmall, t.Typography);
            stack.Children.Add(titleLbl);
        }

        TextBlock? msgLbl = null;
        if (!string.IsNullOrEmpty(Message))
        {
            msgLbl = new TextBlock
            {
                Text = Message,
                TextWrapping = TextWrapping.Wrap,
            };
            msgLbl.Apply(MavTypeRole.BodyMedium, t.Typography);
            stack.Children.Add(msgLbl);
        }

        if (MavVisualTreeHelpers.ToView(Content) is { } bodyView)
        {
            MavVisualTreeHelpers.DetachFromParent(bodyView);
            stack.Children.Add(bodyView);
        }

        var actionsRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };

        if (!string.IsNullOrEmpty(NeutralText))
        {
            var neutralBtn = new MavButton(NeutralText!, MavButtonVariant.Text);
            neutralBtn.Clicked += (_, _) =>
            {
                NeutralClicked?.Invoke(this, EventArgs.Empty);
                if (NeutralCommand is { } nCmd && nCmd.CanExecute(NeutralCommandParameter))
                    nCmd.Execute(NeutralCommandParameter);
                Close();
            };
            actionsRow.Add(neutralBtn, 0, 0);
        }

        if (!string.IsNullOrEmpty(DismissText))
        {
            var dismissBtn = new MavButton(DismissText!, MavButtonVariant.Text);
            dismissBtn.Clicked += (_, _) =>
            {
                Dismissed?.Invoke(this, EventArgs.Empty);
                if (DismissCommand is { } dCmd && dCmd.CanExecute(DismissCommandParameter))
                    dCmd.Execute(DismissCommandParameter);
                Close();
            };
            actionsRow.Add(dismissBtn, 2, 0);
        }

        if (!string.IsNullOrEmpty(confirmText))
        {
            var confirmBtn = new MavButton(confirmText, MavButtonVariant.Text);
            confirmBtn.Clicked += (_, _) =>
            {
                Confirmed?.Invoke(this, EventArgs.Empty);
                if (ConfirmCommand is { } cCmd && cCmd.CanExecute(ConfirmCommandParameter))
                    cCmd.Execute(ConfirmCommandParameter);
                Close();
            };
            actionsRow.Add(confirmBtn, 3, 0);
        }

        var effectiveAnchor = anchor ?? (this.Parent as Visual) ?? this;

        var bodyScroll = new MavScrollViewer
        {
            Content = stack,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var content = new DockPanel();
        DockPanel.SetDock(actionsRow, Dock.Bottom);
        content.Children.Add(actionsRow);
        content.Children.Add(bodyScroll);

        // The card is centred and stretched, so the layout adapts by itself; only the
        // size caps and the safe-area margins have to be recomputed on resize/rotation.
        var card = new MavSurface
        {
            Radius = 28,
            Elevation = 3,
            Padding = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Child = content,
        };
        MavAutomation.SetName(card, Title);
        MavAutomation.SetHelpText(card, Message);

        void Reflow()
        {
            var overlaySize = MavOverlay.GetOverlaySize(effectiveAnchor);
            var insets = MavOverlay.SafeAreaOf(effectiveAnchor);

            // Full-width on a phone, capped at 560dp on larger screens.
            var maxWidth = Math.Max(
                0,
                Math.Min(560, overlaySize.Width - insets.Left - insets.Right - 48));
            var maxCardHeight = Math.Max(
                160,
                overlaySize.Height - insets.Top - insets.Bottom - 48);

            card.MinWidth = Math.Min(280, maxWidth);
            card.MaxWidth = maxWidth;
            card.MaxHeight = maxCardHeight;
            card.Margin = new Thickness(
                24 + insets.Left,
                24 + insets.Top,
                24 + insets.Right,
                24 + insets.Bottom);
        }

        Reflow();

        MavTheme.Observe(card, (surf, theme) =>
        {
            var col = theme.Color;
            surf.FillColor = col.SurfaceContainerHigh;
            if (iconView is not null)
                iconView.Color = col.Secondary;
            if (titleLbl is not null)
                titleLbl.Foreground = MavTheme.BrushOf(col.OnSurface);
            if (msgLbl is not null)
                msgLbl.Foreground = MavTheme.BrushOf(col.OnSurfaceVariant);
        });

        IDisposable? handle = null;
        handle = MavOverlay.Show(
            anchor ?? (this.Parent as Visual) ?? this,
            card,
            scrimColor: c.Scrim.WithAlpha(0.32f),
            onScrimTap: () =>
            {
                if (DismissOnScrimTap)
                {
                    Dismissed?.Invoke(this, EventArgs.Empty);
                    if (DismissCommand is { } dCmd && dCmd.CanExecute(DismissCommandParameter))
                        dCmd.Execute(DismissCommandParameter);
                    Close();
                }
            },
            onViewportChanged: Reflow,
            onClosed: () => OnOverlayClosed(handle));
        _handle = handle;

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;

        MavMotion.ScaleIn(card, 0.90, MavMotion.Medium1);
    }

    /// <summary>Hides the dialog.</summary>
    public void Close()
    {
        var handle = _handle;
        if (handle is null)
            return;

        _handle = null;
        handle.Dispose();
        FinishClose();
    }

    private void OnOverlayClosed(IDisposable? handle)
    {
        if (handle is null || !ReferenceEquals(_handle, handle))
            return;

        _handle = null;
        FinishClose();
    }

    private void FinishClose()
    {
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Shows an alert with a single button and completes when the user closes it.</summary>
    public static Task ShowAlertAsync(
        string title,
        string message,
        string? confirmText = null,
        string? icon = null,
        Visual? anchor = null)
    {
        var tcs = new TaskCompletionSource();
        var dlg = new MavDialog
        {
            Title = title,
            Message = message,
            Icon = icon,
            ConfirmText = confirmText ?? MavLocalization.Ok,
        };
        dlg.Closed += (_, _) => tcs.TrySetResult();
        dlg.Show(anchor);
        return tcs.Task;
    }

    /// <summary>Shows a dialog with confirm and cancel buttons and returns <c>true</c> if the user confirmed.</summary>
    public static Task<bool> ShowConfirmAsync(
        string title,
        string message,
        string? confirmText = null,
        string? dismissText = null,
        string? icon = null,
        Visual? anchor = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var result = false;
        var dlg = new MavDialog
        {
            Title = title,
            Message = message,
            Icon = icon,
            ConfirmText = confirmText ?? MavLocalization.Ok,
            DismissText = dismissText ?? MavLocalization.Cancel,
        };
        dlg.Confirmed += (_, _) => result = true;
        dlg.Closed += (_, _) => tcs.TrySetResult(result);
        dlg.Show(anchor);
        return tcs.Task;
    }

    /// <summary>Shows content in a full-screen dialog and returns a handle that closes it when disposed.</summary>
    public static IDisposable ShowFullscreen(
        string title,
        Control content,
        string? actionText = null,
        Action? onAction = null,
        Visual? anchor = null)
    {
        var t = MavTheme.Current;
        var c = t.Color;
        IDisposable? handle = null;

        var closeBtn = new MavIconButton(MavIcons.Close);
        closeBtn.Clicked += (_, _) => handle?.Dispose();

        var titleLbl = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0),
        };
        titleLbl.Apply(MavTypeRole.TitleLarge, t.Typography);

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            MinHeight = 56,
            Margin = new Thickness(8, 4, 12, 4),
        };
        header.Add(closeBtn, 0, 0);
        header.Add(titleLbl, 1, 0);

        if (!string.IsNullOrEmpty(actionText))
        {
            var actBtn = new MavButton(actionText!, MavButtonVariant.Text);
            actBtn.Clicked += (_, _) =>
            {
                onAction?.Invoke();
                handle?.Dispose();
            };
            header.Add(actBtn, 2, 0);
        }

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
        };
        rootGrid.Add(header, 0, 0);
        rootGrid.Add(new MavDivider(), 0, 1);

        var scroll = new MavScrollViewer
        {
            Padding = new Thickness(20, 16),
            Content = content,
        };
        rootGrid.Add(scroll, 0, 2);

        var surface = new MavSurface
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = new MavSafeArea(rootGrid, MavSafeAreaEdges.All),
        };

        MavTheme.Observe(surface, (surf, theme) =>
        {
            surf.FillColor = theme.Color.Surface;
            titleLbl.Foreground = MavTheme.BrushOf(theme.Color.OnSurface);
        });

        handle = MavOverlay.Show(
            anchor,
            surface,
            c.Scrim.WithAlpha(0.32f),
            onScrimTap: () => handle?.Dispose());
        MavMotion.FadeIn(surface, MavMotion.Short4);
        return handle;
    }
}
