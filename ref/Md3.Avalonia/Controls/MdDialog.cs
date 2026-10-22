using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 modal dialog (28dp rounded surface, optional hero icon, headline,
/// supporting text, custom body, and action buttons) plus fullscreen dialog helper.
/// </summary>
public class MdDialog : Control
{
    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<MdDialog, string?>(nameof(Icon));

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MdDialog, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<MdDialog, string?>(nameof(Message));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MdDialog, Control?>(nameof(Body));

    public static readonly StyledProperty<string?> ConfirmTextProperty =
        AvaloniaProperty.Register<MdDialog, string?>(nameof(ConfirmText));

    public static readonly StyledProperty<string?> DismissTextProperty =
        AvaloniaProperty.Register<MdDialog, string?>(nameof(DismissText));

    public static readonly StyledProperty<string?> NeutralTextProperty =
        AvaloniaProperty.Register<MdDialog, string?>(nameof(NeutralText));

    public static readonly StyledProperty<bool> DismissOnScrimTapProperty =
        AvaloniaProperty.Register<MdDialog, bool>(nameof(DismissOnScrimTap), true);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MdDialog, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> ConfirmCommandProperty =
        AvaloniaProperty.Register<MdDialog, ICommand?>(nameof(ConfirmCommand));

    public static readonly StyledProperty<object?> ConfirmCommandParameterProperty =
        AvaloniaProperty.Register<MdDialog, object?>(nameof(ConfirmCommandParameter));

    public static readonly StyledProperty<ICommand?> DismissCommandProperty =
        AvaloniaProperty.Register<MdDialog, ICommand?>(nameof(DismissCommand));

    public static readonly StyledProperty<object?> DismissCommandParameterProperty =
        AvaloniaProperty.Register<MdDialog, object?>(nameof(DismissCommandParameter));

    public static readonly StyledProperty<ICommand?> NeutralCommandProperty =
        AvaloniaProperty.Register<MdDialog, ICommand?>(nameof(NeutralCommand));

    public static readonly StyledProperty<object?> NeutralCommandParameterProperty =
        AvaloniaProperty.Register<MdDialog, object?>(nameof(NeutralCommandParameter));

    private IDisposable? _handle;
    private bool _syncingIsOpen;

    static MdDialog()
    {
        BodyProperty.Changed.AddClassHandler<MdDialog>((d, e) =>
            MdVisualTreeHelpers.AttachLogicalChild(d, e.GetNewValue<Control?>()));
        IsOpenProperty.Changed.AddClassHandler<MdDialog>((d, e) =>
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

    [Content]
    public Control? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
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

    protected override Size MeasureOverride(Size availableSize) => default;

    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MdTheme.Current;
        var c = t.Color;
        var confirmText = ConfirmText ?? MdLocalization.Ok;

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 16,
        };

        MdIcon? iconView = null;
        var hasIcon = !string.IsNullOrEmpty(Icon);
        if (hasIcon)
        {
            iconView = new MdIcon(Icon, 24)
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
            titleLbl.Apply(MdTypeRole.HeadlineSmall, t.Typography);
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
            msgLbl.Apply(MdTypeRole.BodyMedium, t.Typography);
            stack.Children.Add(msgLbl);
        }

        if (Body is not null)
        {
            MdVisualTreeHelpers.DetachFromParent(Body);
            stack.Children.Add(Body);
        }

        var actionsRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8,
            Margin = new Thickness(0, 8, 0, 0),
        };

        if (!string.IsNullOrEmpty(NeutralText))
        {
            var neutralBtn = new MdButton(NeutralText!, MdButtonVariant.Text);
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
            var dismissBtn = new MdButton(DismissText!, MdButtonVariant.Text);
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
            var confirmBtn = new MdButton(confirmText, MdButtonVariant.Text);
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

        var bodyScroll = new MdScrollViewer
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
        var card = new MdSurface
        {
            Radius = 28,
            Elevation = 3,
            Padding = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Content = content,
        };
        MdAutomation.SetName(card, Title);
        MdAutomation.SetHelpText(card, Message);

        void Reflow()
        {
            var overlaySize = MdOverlay.GetOverlaySize(effectiveAnchor);
            var insets = MdOverlay.SafeAreaOf(effectiveAnchor);

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

        MdTheme.Observe(card, (surf, theme) =>
        {
            var col = theme.Color;
            surf.FillColor = col.SurfaceContainerHigh;
            if (iconView is not null)
                iconView.Color = col.Secondary;
            if (titleLbl is not null)
                titleLbl.Foreground = MdTheme.BrushOf(col.OnSurface);
            if (msgLbl is not null)
                msgLbl.Foreground = MdTheme.BrushOf(col.OnSurfaceVariant);
        });

        _handle = MdOverlay.Show(
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
            onViewportChanged: Reflow);

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;

        MdMotion.ScaleIn(card, 0.90, MdMotion.Medium1);
    }

    public void Close()
    {
        if (_handle is not null)
        {
            _handle.Dispose();
            _handle = null;
            _syncingIsOpen = true;
            SetValue(IsOpenProperty, false);
            _syncingIsOpen = false;
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }

    public static Task ShowAlertAsync(
        string title,
        string message,
        string? confirmText = null,
        string? icon = null,
        Visual? anchor = null)
    {
        var tcs = new TaskCompletionSource();
        var dlg = new MdDialog
        {
            Title = title,
            Message = message,
            Icon = icon,
            ConfirmText = confirmText ?? MdLocalization.Ok,
        };
        dlg.Closed += (_, _) => tcs.TrySetResult();
        dlg.Show(anchor);
        return tcs.Task;
    }

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
        var dlg = new MdDialog
        {
            Title = title,
            Message = message,
            Icon = icon,
            ConfirmText = confirmText ?? MdLocalization.Ok,
            DismissText = dismissText ?? MdLocalization.Cancel,
        };
        dlg.Confirmed += (_, _) => result = true;
        dlg.Closed += (_, _) => tcs.TrySetResult(result);
        dlg.Show(anchor);
        return tcs.Task;
    }

    public static IDisposable ShowFullscreen(
        string title,
        Control content,
        string? actionText = null,
        Action? onAction = null,
        Visual? anchor = null)
    {
        var t = MdTheme.Current;
        var c = t.Color;
        IDisposable? handle = null;

        var closeBtn = new MdIconButton(MdIcons.Close);
        closeBtn.Clicked += (_, _) => handle?.Dispose();

        var titleLbl = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0),
        };
        titleLbl.Apply(MdTypeRole.TitleLarge, t.Typography);

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
            var actBtn = new MdButton(actionText!, MdButtonVariant.Text);
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
        rootGrid.Add(new MdDivider(), 0, 1);

        var scroll = new MdScrollViewer
        {
            Padding = new Thickness(20, 16),
            Content = content,
        };
        rootGrid.Add(scroll, 0, 2);

        var surface = new MdSurface
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = new MdSafeArea(rootGrid, MdSafeAreaEdges.All),
        };

        MdTheme.Observe(surface, (surf, theme) =>
        {
            surf.FillColor = theme.Color.Surface;
            titleLbl.Foreground = MdTheme.BrushOf(theme.Color.OnSurface);
        });

        handle = MdOverlay.Show(
            anchor,
            surface,
            c.Scrim.WithAlpha(0.32f),
            onScrimTap: () => handle?.Dispose());
        MdMotion.FadeIn(surface, MdMotion.Short4);
        return handle;
    }
}

/// <summary>
/// Injectable service for presenting Material Design 3 modal alerts and confirmations from ViewModels.
/// </summary>
public interface IMdDialogService
{
    Task ShowAlertAsync(string title, string message, string? confirmText = null, string? icon = null);
    Task<bool> ShowConfirmAsync(string title, string message, string? confirmText = null, string? dismissText = null, string? icon = null);
}

public sealed class MdDialogService : IMdDialogService
{
    public static MdDialogService Instance { get; } = new();

    public Task ShowAlertAsync(string title, string message, string? confirmText = null, string? icon = null) =>
        MdDialog.ShowAlertAsync(title, message, confirmText, icon);

    public Task<bool> ShowConfirmAsync(string title, string message, string? confirmText = null, string? dismissText = null, string? icon = null) =>
        MdDialog.ShowConfirmAsync(title, message, confirmText, dismissText, icon);
}
