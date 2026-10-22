using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Platform;

public enum MdTextContextMenuMode
{
    Auto,
    Desktop,
    Mobile,
}

/// <summary>
/// Attaches a Material Design 3 adaptive text editing popup menu (<see cref="MdTextContextFlyout"/>)
/// to any <see cref="TextBox"/>:
/// <list type="bullet">
///   <item><description><b>Mobile / Touch (Android &amp; iOS)</b>: compact horizontal floating pill toolbar above the selection/caret with context-filtered actions (Cut, Copy, Paste, Select all, Undo, Redo), horizontal scroll on narrow screens, and non-focus-stealing transient behavior that preserves selection handles and soft keyboard.</description></item>
///   <item><description><b>Desktop (Mouse &amp; Keyboard)</b>: vertical Material 3 menu at the pointer or caret with leading icons, localized labels, and platform keyboard shortcut hints.</description></item>
/// </list>
/// </summary>
public static class MdTextContextMenu
{
    public static MdTextContextFlyout Attach(TextBox textBox, MdTextContextMenuMode mode = MdTextContextMenuMode.Auto)
    {
        ArgumentNullException.ThrowIfNull(textBox);

        if (textBox.Text is null)
            textBox.SetCurrentValue(TextBox.TextProperty, string.Empty);

        if (textBox.ContextFlyout is MdTextContextFlyout existing)
        {
            existing.Mode = mode;
            return existing;
        }

        textBox.ContextMenu = null;
        var flyout = new MdTextContextFlyout(textBox)
        {
            Mode = mode,
        };
        textBox.ContextFlyout = flyout;
        return flyout;
    }
}

/// <summary>
/// Material Design 3 adaptive <see cref="PopupFlyoutBase"/> for <see cref="TextBox"/> editing commands.
/// Inherits <see cref="PopupFlyoutBase"/> so Avalonia's internal <c>TextSelectionHandleCanvas</c>
/// automatically triggers and dismisses it during touch word selection and selection-handle dragging.
/// </summary>
public sealed class MdTextContextFlyout : PopupFlyoutBase
{
    private readonly TextBox _textBox;
    private Border? _presenterRoot;
    private MdSurface? _surface;
    private ScrollViewer? _scroll;
    private StackPanel? _itemsPanel;
    private PointerType _lastPointerType = (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        ? PointerType.Touch
        : PointerType.Mouse;
    private bool _showAtPointer;
    private TopLevel? _dismissRoot;
    private IDisposable? _overlayDismissHandle;

    public MdTextContextMenuMode Mode { get; set; } = MdTextContextMenuMode.Auto;

    public bool IsMobileEffective => Mode switch
    {
        MdTextContextMenuMode.Mobile => true,
        MdTextContextMenuMode.Desktop => false,
        _ => OperatingSystem.IsAndroid()
             || OperatingSystem.IsIOS()
             || _textBox.Classes.Contains(":touch-mode")
             || _lastPointerType != PointerType.Mouse,
    };

    public MdTextContextFlyout(TextBox textBox)
    {
        _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
        ShowMode = FlyoutShowMode.Transient;
        OverlayDismissEventPassThrough = false;
        Placement = PlacementMode.Custom;
        CustomPopupPlacementCallback = PlacePopup;

        _textBox.AddHandler(
            InputElement.PointerPressedEvent,
            (_, e) => _lastPointerType = e.Pointer.Type,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        _textBox.AddHandler(
            InputElement.HoldingEvent,
            (_, _) => _lastPointerType = PointerType.Touch,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        _textBox.LostFocus += (_, _) =>
        {
            if (IsOpen)
                Hide();
        };

        Popup.Opened += OnFlyoutPopupOpened;
        Popup.Closed += OnFlyoutPopupClosed;
    }

    protected override Control CreatePresenter()
    {
        _itemsPanel = new StackPanel();
        _scroll = new MdScrollViewer
        {
            Content = _itemsPanel,
        };
        _surface = new MdSurface
        {
            Content = _scroll,
        };
        _presenterRoot = new Border
        {
            Background = Brushes.Transparent,
            Padding = new Thickness(8),
            Child = _surface,
        };

        MdTheme.Observe(_presenterRoot, (_, _) =>
        {
            if (IsOpen)
                RebuildItems();
        });

        return _presenterRoot;
    }

    protected override bool ShowAtCore(Control placementTarget, bool showAtPointer = false)
    {
        _showAtPointer = showAtPointer;

        if (_presenterRoot is null)
            _ = CreatePresenter();

        if (!RebuildItems())
            return false;

        ShowMode = FlyoutShowMode.Transient;

        var mobile = IsMobileEffective;
        if (!mobile && showAtPointer)
        {
            Placement = PlacementMode.Pointer;
            PlacementConstraintAdjustment =
                PopupPositionerConstraintAdjustment.FlipX |
                PopupPositionerConstraintAdjustment.FlipY |
                PopupPositionerConstraintAdjustment.SlideX |
                PopupPositionerConstraintAdjustment.SlideY;
            CustomPopupPlacementCallback = null;
            return base.ShowAtCore(placementTarget, showAtPointer: true);
        }

        Placement = PlacementMode.Custom;
        CustomPopupPlacementCallback = PlacePopup;
        return base.ShowAtCore(placementTarget, showAtPointer: false);
    }

    protected override bool HideCore(bool canCancel = true)
    {
        DetachDismissRoot();
        _overlayDismissHandle?.Dispose();
        _overlayDismissHandle = null;
        return base.HideCore(canCancel);
    }

    private void OnFlyoutPopupOpened(object? sender, EventArgs e)
    {
        if (_presenterRoot is not null && TopLevel.GetTopLevel(_presenterRoot) is PopupRoot popupRoot)
        {
            popupRoot.Background = Brushes.Transparent;
            popupRoot.TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
            popupRoot.TransparencyBackgroundFallback = Brushes.Transparent;
        }

        DetachDismissRoot();
        _dismissRoot = TopLevel.GetTopLevel(_textBox);
        _dismissRoot?.AddHandler(
            InputElement.PointerPressedEvent,
            OnRootPointerPressed,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        _overlayDismissHandle?.Dispose();
        _overlayDismissHandle = MdOverlay.PushDismissable(Hide);
    }

    private void OnFlyoutPopupClosed(object? sender, EventArgs e)
    {
        DetachDismissRoot();
        _overlayDismissHandle?.Dispose();
        _overlayDismissHandle = null;
    }

    private void DetachDismissRoot()
    {
        if (_dismissRoot is not null)
        {
            _dismissRoot.RemoveHandler(InputElement.PointerPressedEvent, OnRootPointerPressed);
            _dismissRoot = null;
        }
    }

    private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsOpen || _presenterRoot is null)
            return;

        if (e.Source is Visual src && (ReferenceEquals(_presenterRoot, src) || _presenterRoot.IsVisualAncestorOf(src)))
            return;

        if (_presenterRoot.IsPointerOver)
            return;

        Hide();
    }

    private bool RebuildItems()
    {
        if (_itemsPanel is null || _scroll is null || _surface is null || _presenterRoot is null)
            return false;

        var theme = MdTheme.Current;
        var c = theme.Color;
        var mobile = IsMobileEffective;

        var text = _textBox.Text ?? string.Empty;
        var selStart = Math.Min(_textBox.SelectionStart, _textBox.SelectionEnd);
        var selEnd = Math.Max(_textBox.SelectionStart, _textBox.SelectionEnd);
        var selLength = Math.Max(0, selEnd - selStart);
        var hasSelection = selLength > 0;
        var isReadOnly = _textBox.IsReadOnly;
        var isPassword = _textBox.PasswordChar != '\0' && !_textBox.RevealPassword;

        var canCut = !isReadOnly && !isPassword && hasSelection;
        var canCopy = !isPassword && hasSelection;
        var canPaste = !isReadOnly && _textBox.CanPaste;
        var canSelectAll = text.Length > 0 && (!mobile || selLength < text.Length);
        var canUndo = !isReadOnly && _textBox.IsUndoEnabled && _textBox.CanUndo;
        var canRedo = !isReadOnly && _textBox.IsUndoEnabled && _textBox.CanRedo;

        _itemsPanel.Children.Clear();

        if (mobile)
        {
            var actions = new List<(string Label, Action Execute, bool StayOpen)>();
            if (canCut)
                actions.Add((MdLocalization.Cut, () => _textBox.Cut(), false));
            if (canCopy)
                actions.Add((MdLocalization.Copy, () => _textBox.Copy(), false));
            if (canPaste)
                actions.Add((MdLocalization.Paste, () => _textBox.Paste(), false));
            if (canSelectAll)
                actions.Add((MdLocalization.SelectAll, () => _textBox.SelectAll(), true));
            if (canUndo)
                actions.Add((MdLocalization.Undo, () => _textBox.Undo(), false));
            if (canRedo)
                actions.Add((MdLocalization.Redo, () => _textBox.Redo(), false));

            if (actions.Count == 0)
                return false;

            _itemsPanel.Orientation = Orientation.Horizontal;
            _itemsPanel.Spacing = 0;

            for (int i = 0; i < actions.Count; i++)
            {
                if (i > 0)
                {
                    _itemsPanel.Children.Add(new Border
                    {
                        Width = 1,
                        Height = 18,
                        VerticalAlignment = VerticalAlignment.Center,
                        Background = MdTheme.BrushOf(c.OutlineVariant.WithAlpha(0.45f)),
                    });
                }

                var (label, exec, stayOpen) = actions[i];
                _itemsPanel.Children.Add(BuildMobileItem(label, () =>
                {
                    if (!_textBox.IsFocused && _textBox.IsEnabled)
                        _textBox.Focus();

                    exec();

                    if (stayOpen && IsOpen)
                    {
                        if (RebuildItems())
                        {
                            _presenterRoot.InvalidateMeasure();
                            return;
                        }
                    }

                    Hide();
                }, theme));
            }

            _scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            var topLevel = TopLevel.GetTopLevel(_textBox);
            var safeInsets = topLevel?.InsetsManager?.SafeAreaPadding ?? default;
            var viewportWidth = topLevel is not null && topLevel.ClientSize.Width > 0
                ? topLevel.ClientSize.Width - safeInsets.Left - safeInsets.Right - 20
                : 340;

            _scroll.MaxWidth = Math.Max(160, viewportWidth);
            _scroll.MaxHeight = 52;

            _surface.MinWidth = 0;
            _surface.Radius = 22;
            _surface.Elevation = 3;
            _surface.Padding = new Thickness(4, 2);
            _surface.FillColor = c.SurfaceContainerHigh;
            _surface.BorderStrokeColor = c.OutlineVariant.WithAlpha(0.45f);
            _surface.BorderStrokeThickness = 1;
        }
        else
        {
            var isMac = OperatingSystem.IsMacOS();
            var mod = isMac ? "\u2318" : "Ctrl+";
            var redoShortcut = isMac ? "\u21E7\u2318Z" : "Ctrl+Y";

            _itemsPanel.Orientation = Orientation.Vertical;
            _itemsPanel.Spacing = 0;

            _itemsPanel.Children.Add(BuildDesktopItem(MdIcons.Undo, MdLocalization.Undo, $"{mod}Z", canUndo, () => _textBox.Undo(), theme));
            _itemsPanel.Children.Add(BuildDesktopItem(MdIcons.Redo, MdLocalization.Redo, redoShortcut, canRedo, () => _textBox.Redo(), theme));
            _itemsPanel.Children.Add(BuildDesktopDivider(c));
            _itemsPanel.Children.Add(BuildDesktopItem(MdIcons.ContentCut, MdLocalization.Cut, $"{mod}X", canCut, () => _textBox.Cut(), theme));
            _itemsPanel.Children.Add(BuildDesktopItem(MdIcons.ContentCopy, MdLocalization.Copy, $"{mod}C", canCopy, () => _textBox.Copy(), theme));
            _itemsPanel.Children.Add(BuildDesktopItem(MdIcons.ContentPaste, MdLocalization.Paste, $"{mod}V", canPaste, () => _textBox.Paste(), theme));
            _itemsPanel.Children.Add(BuildDesktopDivider(c));
            _itemsPanel.Children.Add(BuildDesktopItem(MdIcons.SelectAll, MdLocalization.SelectAll, $"{mod}A", canSelectAll, () => _textBox.SelectAll(), theme));

            _scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _scroll.MaxWidth = 320;
            _scroll.MaxHeight = 360;

            _surface.MinWidth = 216;
            _surface.Radius = 8;
            _surface.Elevation = 2;
            _surface.Padding = new Thickness(0, 6);
            _surface.FillColor = c.SurfaceContainer;
            _surface.BorderStrokeColor = c.OutlineVariant.WithAlpha(0.45f);
            _surface.BorderStrokeThickness = 1;
        }

        _presenterRoot.InvalidateMeasure();
        return true;
    }

    private static Control BuildMobileItem(string text, Action onTap, MdTheme theme)
    {
        var c = theme.Color;
        var label = new TextBlock
        {
            Text = text,
            Foreground = MdTheme.BrushOf(c.OnSurface),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        label.Apply(MdTypeRole.LabelLarge, theme.Typography);

        var ink = new MdInkSurface
        {
            MinHeight = 36,
            CornerRadius = 18,
            FillColor = Colors.Transparent,
            RippleColor = c.OnSurface,
            Focusable = false,
            Content = new Border
            {
                Padding = new Thickness(13, 6),
                Child = label,
            },
        };
        ink.Clicked += (_, _) => onTap();
        return ink;
    }

    private Control BuildDesktopItem(
        string iconGlyph,
        string text,
        string shortcut,
        bool enabled,
        Action onInvoke,
        MdTheme theme)
    {
        var c = theme.Color;
        var fg = enabled ? c.OnSurface : c.OnSurface.WithAlpha(0.38f);
        var subFg = enabled ? c.OnSurfaceVariant : c.OnSurfaceVariant.WithAlpha(0.38f);

        var icon = new MdIcon(iconGlyph, 18)
        {
            UseThemeDefaultColor = false,
            Color = subFg,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var label = new TextBlock
        {
            Text = text,
            Foreground = MdTheme.BrushOf(fg),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 16, 0),
        };
        label.Apply(MdTypeRole.BodyMedium, theme.Typography);

        var gestureLabel = new TextBlock
        {
            Text = shortcut,
            Foreground = MdTheme.BrushOf(subFg),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        gestureLabel.Apply(MdTypeRole.LabelSmall, theme.Typography);

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(14, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Add(icon, 0, 0);
        row.Add(label, 1, 0);
        row.Add(gestureLabel, 2, 0);

        var ink = new MdInkSurface
        {
            Height = 38,
            CornerRadius = 0,
            FillColor = Colors.Transparent,
            RippleColor = c.OnSurface,
            IsInteractive = enabled,
            Focusable = false,
            Content = row,
        };
        ink.Clicked += (_, _) =>
        {
            if (!enabled) return;
            Hide();
            if (!_textBox.IsFocused && _textBox.IsEnabled)
                _textBox.Focus();
            onInvoke();
        };
        return ink;
    }

    private static Control BuildDesktopDivider(MdColorScheme c) =>
        new Border
        {
            Height = 1,
            Margin = new Thickness(0, 4),
            Background = MdTheme.BrushOf(c.OutlineVariant.WithAlpha(0.5f)),
        };

    private void PlacePopup(CustomPopupPlacement parameters)
    {
        var textBoxOrigin = parameters.AnchorRectangle.TopLeft;
        var localRect = GetSelectionOrCaretRectInTextBox();

        if (IsMobileEffective)
        {
            // Expand 6dp above the text line and 30dp below the text line (past selection handles)
            // so FlipY places the toolbar cleanly below the teardrop handles when near the top of the screen.
            var top = localRect.Top - 6;
            var bottom = Math.Max(top + 24, localRect.Bottom + 30);
            var mobileRect = new Rect(
                textBoxOrigin.X + localRect.X,
                textBoxOrigin.Y + top,
                Math.Max(2, localRect.Width),
                Math.Max(24, bottom - top));

            parameters.AnchorRectangle = mobileRect;
            parameters.Anchor = PopupAnchor.Top;
            parameters.Gravity = PopupGravity.Top;
            parameters.Offset = default;
            parameters.ConstraintAdjustment =
                PopupPositionerConstraintAdjustment.SlideX |
                PopupPositionerConstraintAdjustment.FlipY |
                PopupPositionerConstraintAdjustment.SlideY |
                PopupPositionerConstraintAdjustment.ResizeX;
        }
        else
        {
            var caretRect = new Rect(
                textBoxOrigin.X + localRect.X,
                textBoxOrigin.Y + localRect.Y,
                Math.Max(2, localRect.Width),
                Math.Max(18, localRect.Height));

            parameters.AnchorRectangle = caretRect;
            parameters.Anchor = PopupAnchor.BottomLeft;
            parameters.Gravity = PopupGravity.BottomRight;
            parameters.Offset = new Point(0, 4);
            parameters.ConstraintAdjustment =
                PopupPositionerConstraintAdjustment.FlipY |
                PopupPositionerConstraintAdjustment.FlipX |
                PopupPositionerConstraintAdjustment.SlideX |
                PopupPositionerConstraintAdjustment.SlideY;
        }
    }

    private Rect GetSelectionOrCaretRectInTextBox()
    {
        var boxW = Math.Max(1, _textBox.Bounds.Width);
        var boxH = Math.Max(1, _textBox.Bounds.Height);
        var presenter = _textBox.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();

        if (presenter is not null)
        {
            var offset = presenter.TranslatePoint(default, _textBox) ?? default;
            var selStart = Math.Min(_textBox.SelectionStart, _textBox.SelectionEnd);
            var selEnd = Math.Max(_textBox.SelectionStart, _textBox.SelectionEnd);

            if (selEnd > selStart)
            {
                try
                {
                    var rects = presenter.TextLayout.HitTestTextRange(selStart, selEnd - selStart).ToList();
                    if (rects.Count > 0)
                    {
                        var minX = rects.Min(r => r.Left) + offset.X;
                        var maxX = rects.Max(r => r.Right) + offset.X;
                        var minY = rects.Min(r => r.Top) + offset.Y;
                        var maxY = rects.Max(r => r.Bottom) + offset.Y;

                        minX = Math.Clamp(minX, 0, boxW);
                        maxX = Math.Clamp(maxX, minX, boxW);
                        minY = Math.Clamp(minY, 0, boxH);
                        maxY = Math.Clamp(maxY, minY, boxH);

                        return new Rect(minX, minY, Math.Max(2, maxX - minX), Math.Max(16, maxY - minY));
                    }
                }
                catch
                {
                    // Fallback to CursorRectangle below.
                }
            }

            try
            {
                var cursor = presenter.TextLayout.HitTestTextPosition(Math.Max(0, _textBox.CaretIndex));
                if (cursor.Height > 0 || cursor.X > 0 || cursor.Y > 0)
                {
                    var cx = Math.Clamp(cursor.X + offset.X, 0, boxW);
                    var cy = Math.Clamp(cursor.Y + offset.Y, 0, boxH);
                    var ch = cursor.Height > 0 ? cursor.Height : Math.Max(18, _textBox.FontSize * 1.25);
                    return new Rect(cx, cy, 2, ch);
                }
            }
            catch
            {
                // Fallback to center rect below.
            }
        }

        return new Rect(Math.Min(24, boxW / 2), Math.Max(0, (boxH - 20) / 2), 2, Math.Min(24, boxH));
    }
}

/// <summary>
/// Chromeless single-line <see cref="TextBox"/> used inside Material 3 text fields,
/// search bars, combo boxes, pin inputs, and date/time pickers so the enclosing
/// Material container owns the border, background, and state indicators.
/// </summary>
public class MdNativeEntry : TextBox
{
    private static readonly IControlTemplate ChromelessEntryTemplate =
        new FuncControlTemplate<TextBox>((box, scope) =>
        {
            var placeholder = new TextBlock
            {
                Name = "PART_Placeholder",
                IsHitTestVisible = false,
                [!TextBlock.TextProperty] = new TemplateBinding(PlaceholderTextProperty),
                [!TextBlock.ForegroundProperty] = new TemplateBinding(PlaceholderForegroundProperty),
                [!TextBlock.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
                [!TextBlock.TextWrappingProperty] = new TemplateBinding(TextWrappingProperty),
                [!Layoutable.HorizontalAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [!Layoutable.VerticalAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty),
            };
            placeholder.Bind(
                Visual.IsVisibleProperty,
                box.GetBindingObservable(TextProperty, static text => string.IsNullOrEmpty(text)));

            var presenter = new TextPresenter
            {
                Name = "PART_TextPresenter",
                [!TextPresenter.TextProperty] = new TemplateBinding(TextProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.CaretBlinkIntervalProperty] = new TemplateBinding(CaretBlinkIntervalProperty),
                [!TextPresenter.CaretIndexProperty] = new TemplateBinding(CaretIndexProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionStartProperty] = new TemplateBinding(SelectionStartProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionEndProperty] = new TemplateBinding(SelectionEndProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
                [!TextPresenter.TextWrappingProperty] = new TemplateBinding(TextWrappingProperty),
                [!TextPresenter.LineHeightProperty] = new TemplateBinding(LineHeightProperty),
                [!TextPresenter.LetterSpacingProperty] = new TemplateBinding(LetterSpacingProperty),
                [!TextPresenter.PasswordCharProperty] = new TemplateBinding(PasswordCharProperty),
                [!TextPresenter.RevealPasswordProperty] = new TemplateBinding(RevealPasswordProperty),
                [!TextPresenter.SelectionBrushProperty] = new TemplateBinding(SelectionBrushProperty),
                [!TextPresenter.SelectionForegroundBrushProperty] = new TemplateBinding(SelectionForegroundBrushProperty),
                [!TextPresenter.CaretBrushProperty] = new TemplateBinding(CaretBrushProperty),
                [!Layoutable.HorizontalAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [!Layoutable.VerticalAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty),
            };
            scope.Register("PART_TextPresenter", presenter);

            var panel = new Panel
            {
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Ibeam),
            };
            panel.Children.Add(placeholder);
            panel.Children.Add(presenter);

            var scroll = new MdScrollViewer
            {
                Name = "PART_ScrollViewer",
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsScrollChainingEnabled = true,
                Content = panel,
            };
            scope.Register("PART_ScrollViewer", scroll);
            return scroll;
        });

    public event EventHandler? Completed;

    public MdNativeEntry()
    {
        Theme = null;
        Template = ChromelessEntryTemplate;
        Text = string.Empty;
        Background = Brushes.Transparent;
        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        MinHeight = 0;
        MinWidth = 0;
        FocusAdorner = null;
        AcceptsReturn = false;
        TextWrapping = TextWrapping.NoWrap;
        VerticalAlignment = VerticalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Center;
        FontFamily = MdFonts.RobotoFont;

        MdTextContextMenu.Attach(this);
    }

    public Color TextColor
    {
        get => (Foreground as ISolidColorBrush)?.Color ?? Colors.Black;
        set
        {
            var primary = MdTheme.Current.Color.Primary;
            var fgBrush = MdTheme.BrushOf(value);
            Foreground = fgBrush;
            SelectionForegroundBrush = fgBrush;
            CaretBrush = MdTheme.BrushOf(primary);
            SelectionBrush = MdTheme.BrushOf(primary.WithAlpha(0.30f));
        }
    }

    public string? Placeholder
    {
        get => PlaceholderText;
        set => PlaceholderText = value;
    }

    public Color PlaceholderColor
    {
        get => (PlaceholderForeground as ISolidColorBrush)?.Color ?? Colors.Gray;
        set => PlaceholderForeground = MdTheme.BrushOf(value);
    }

    public TextAlignment HorizontalTextAlignment
    {
        get => TextAlignment;
        set => TextAlignment = value;
    }

    public bool IsPassword
    {
        get => PasswordChar != '\0';
        set => PasswordChar = value ? '\u2022' : '\0';
    }

    public int CursorPosition
    {
        get => CaretIndex;
        set => CaretIndex = Math.Max(0, value);
    }

    public int SelectionLength
    {
        get => Math.Abs(SelectionEnd - SelectionStart);
        set => SelectionEnd = SelectionStart + Math.Max(0, value);
    }

    public void Unfocus()
    {
        if (IsFocused)
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (IsEnabled && !IsFocused && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            Focus();
        base.OnPointerPressed(e);
    }

    protected override void OnHolding(HoldingRoutedEventArgs e)
    {
        if (Text is null)
            SetCurrentValue(TextProperty, string.Empty);
        if (IsEnabled && !IsFocused)
            Focus();
        base.OnHolding(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!AcceptsReturn && e.Key == Key.Enter)
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}

/// <summary>
/// Chromeless multi-line <see cref="TextBox"/> used inside <c>MdTextArea</c>.
/// </summary>
public class MdNativeEditor : TextBox
{
    private static readonly IControlTemplate ChromelessEditorTemplate =
        new FuncControlTemplate<TextBox>((box, scope) =>
        {
            var placeholder = new TextBlock
            {
                Name = "PART_Placeholder",
                IsHitTestVisible = false,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                [!TextBlock.TextProperty] = new TemplateBinding(PlaceholderTextProperty),
                [!TextBlock.ForegroundProperty] = new TemplateBinding(PlaceholderForegroundProperty),
                [!TextBlock.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
            };
            placeholder.Bind(
                Visual.IsVisibleProperty,
                box.GetBindingObservable(TextProperty, static text => string.IsNullOrEmpty(text)));

            var presenter = new TextPresenter
            {
                Name = "PART_TextPresenter",
                VerticalAlignment = VerticalAlignment.Top,
                [!TextPresenter.TextProperty] = new TemplateBinding(TextProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.CaretBlinkIntervalProperty] = new TemplateBinding(CaretBlinkIntervalProperty),
                [!TextPresenter.CaretIndexProperty] = new TemplateBinding(CaretIndexProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionStartProperty] = new TemplateBinding(SelectionStartProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionEndProperty] = new TemplateBinding(SelectionEndProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
                [!TextPresenter.TextWrappingProperty] = new TemplateBinding(TextWrappingProperty),
                [!TextPresenter.LineHeightProperty] = new TemplateBinding(LineHeightProperty),
                [!TextPresenter.LetterSpacingProperty] = new TemplateBinding(LetterSpacingProperty),
                [!TextPresenter.SelectionBrushProperty] = new TemplateBinding(SelectionBrushProperty),
                [!TextPresenter.SelectionForegroundBrushProperty] = new TemplateBinding(SelectionForegroundBrushProperty),
                [!TextPresenter.CaretBrushProperty] = new TemplateBinding(CaretBrushProperty),
            };
            scope.Register("PART_TextPresenter", presenter);

            var panel = new Panel
            {
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Ibeam),
            };
            panel.Children.Add(placeholder);
            panel.Children.Add(presenter);

            var scroll = new MdScrollViewer
            {
                Name = "PART_ScrollViewer",
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsScrollChainingEnabled = true,
                Content = panel,
            };
            scope.Register("PART_ScrollViewer", scroll);
            return scroll;
        });

    public MdNativeEditor()
    {
        Theme = null;
        Template = ChromelessEditorTemplate;
        Text = string.Empty;
        Background = Brushes.Transparent;
        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        MinHeight = 0;
        MinWidth = 0;
        FocusAdorner = null;
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
        VerticalAlignment = VerticalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Top;
        FontFamily = MdFonts.RobotoFont;

        MdTextContextMenu.Attach(this);
    }

    public Color TextColor
    {
        get => (Foreground as ISolidColorBrush)?.Color ?? Colors.Black;
        set
        {
            var primary = MdTheme.Current.Color.Primary;
            var fgBrush = MdTheme.BrushOf(value);
            Foreground = fgBrush;
            SelectionForegroundBrush = fgBrush;
            CaretBrush = MdTheme.BrushOf(primary);
            SelectionBrush = MdTheme.BrushOf(primary.WithAlpha(0.30f));
        }
    }

    public string? Placeholder
    {
        get => PlaceholderText;
        set => PlaceholderText = value;
    }

    public Color PlaceholderColor
    {
        get => (PlaceholderForeground as ISolidColorBrush)?.Color ?? Colors.Gray;
        set => PlaceholderForeground = MdTheme.BrushOf(value);
    }

    public void Unfocus()
    {
        if (IsFocused)
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (IsEnabled && !IsFocused && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            Focus();
        base.OnPointerPressed(e);
    }

    protected override void OnHolding(HoldingRoutedEventArgs e)
    {
        if (Text is null)
            SetCurrentValue(TextProperty, string.Empty);
        if (IsEnabled && !IsFocused)
            Focus();
        base.OnHolding(e);
    }
}
