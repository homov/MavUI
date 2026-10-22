using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Insets bottom padding when the soft keyboard (<see cref="IInputPane"/>) occludes the host
/// and automatically scrolls the focused input field into view inside any descendant <see cref="ScrollViewer"/>.
/// </summary>
public class MdKeyboardAvoidingHost : MdContentView
{
    public static readonly StyledProperty<double> ExtraBottomSpacingProperty =
        AvaloniaProperty.Register<MdKeyboardAvoidingHost, double>(nameof(ExtraBottomSpacing), 16.0);

    public static readonly StyledProperty<bool> AnimateInsetProperty =
        AvaloniaProperty.Register<MdKeyboardAvoidingHost, bool>(nameof(AnimateInset), true);

    public static readonly StyledProperty<bool> AutoScrollFocusedElementProperty =
        AvaloniaProperty.Register<MdKeyboardAvoidingHost, bool>(nameof(AutoScrollFocusedElement), true);

    public static readonly StyledProperty<double> SimulatedKeyboardHeightProperty =
        AvaloniaProperty.Register<MdKeyboardAvoidingHost, double>(nameof(SimulatedKeyboardHeight), 0.0);

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MdKeyboardAvoidingHost, Control?>(nameof(Body));

    private IInputPane? _inputPane;
    private Control? _lastFocused;
    private double _keyboardHeight;
    private double _animatedBottom;

    public event EventHandler<double>? KeyboardHeightChanged;

    static MdKeyboardAvoidingHost()
    {
        ExtraBottomSpacingProperty.Changed.AddClassHandler<MdKeyboardAvoidingHost>((h, _) => h.EnsureFocusedVisible());
        SimulatedKeyboardHeightProperty.Changed.AddClassHandler<MdKeyboardAvoidingHost>((h, _) =>
        {
            h.UpdateKeyboardInset();
            // Also lift overlays (dialogs, sheets, popovers, search view) in previews.
            Platform.MdOverlay.SetSimulatedKeyboardHeight(h.SimulatedKeyboardHeight);
        });
        BodyProperty.Changed.AddClassHandler<MdKeyboardAvoidingHost>((h, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MdVisualTreeHelpers.DetachFromParent(body);
            h.Content = body;
        });
    }

    public MdKeyboardAvoidingHost() : this(null)
    {
    }

    public MdKeyboardAvoidingHost(Control? content = null)
    {
        if (content is not null)
            Body = content;

        AddHandler(GotFocusEvent, OnDescendantGotFocus, RoutingStrategies.Bubble, true);
    }

    [Content]
    public Control? Body
    {
        get => GetValue(BodyProperty) ?? Content;
        set => SetValue(BodyProperty, value);
    }

    public double ExtraBottomSpacing
    {
        get => GetValue(ExtraBottomSpacingProperty);
        set => SetValue(ExtraBottomSpacingProperty, Math.Max(0, value));
    }

    public bool AnimateInset
    {
        get => GetValue(AnimateInsetProperty);
        set => SetValue(AnimateInsetProperty, value);
    }

    public bool AutoScrollFocusedElement
    {
        get => GetValue(AutoScrollFocusedElementProperty);
        set => SetValue(AutoScrollFocusedElementProperty, value);
    }

    public double SimulatedKeyboardHeight
    {
        get => GetValue(SimulatedKeyboardHeightProperty);
        set => SetValue(SimulatedKeyboardHeightProperty, Math.Max(0, value));
    }

    public double KeyboardHeight => _keyboardHeight;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachInputPane();
        UpdateKeyboardInset();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        MdMotion.Cancel(this, "kb_inset");
        DetachInputPane();
        base.OnDetachedFromVisualTree(e);
    }

    private void AttachInputPane()
    {
        DetachInputPane();
        _inputPane = TopLevel.GetTopLevel(this)?.InputPane;
        if (_inputPane is not null)
            _inputPane.StateChanged += OnInputPaneStateChanged;
    }

    private void DetachInputPane()
    {
        if (_inputPane is not null)
        {
            _inputPane.StateChanged -= OnInputPaneStateChanged;
            _inputPane = null;
        }
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e) =>
        UpdateKeyboardInset();

    private void OnDescendantGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is Control control)
        {
            _lastFocused = ResolveInputContainer(control);
            Dispatcher.UIThread.Post(EnsureFocusedVisible, DispatcherPriority.Background);
        }
    }

    public void UpdateKeyboardInset()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        var targetBottom = Math.Max(SimulatedKeyboardHeight, ComputeKeyboardBottomPadding(topLevel));
        var changed = Math.Abs(_keyboardHeight - targetBottom) > 0.5;
        _keyboardHeight = targetBottom;
        if (changed)
            KeyboardHeightChanged?.Invoke(this, _keyboardHeight);

        if (Math.Abs(targetBottom - _animatedBottom) <= 0.5 && Math.Abs(Padding.Bottom - targetBottom) <= 0.5)
            return;

        if (AnimateInset)
        {
            MdMotion.Animate(this, "kb_inset", _animatedBottom, targetBottom, MdMotion.Medium1, MdMotion.CubicOut, v =>
            {
                _animatedBottom = v;
                var cur = Padding;
                Padding = new Thickness(cur.Left, cur.Top, cur.Right, v);
            }, EnsureFocusedVisible);
        }
        else
        {
            _animatedBottom = targetBottom;
            var cur = Padding;
            Padding = new Thickness(cur.Left, cur.Top, cur.Right, targetBottom);
            Dispatcher.UIThread.Post(EnsureFocusedVisible, DispatcherPriority.Background);
        }
    }

    private double ComputeKeyboardBottomPadding(TopLevel? topLevel)
    {
        if (topLevel is null || _inputPane is null)
            return 0.0;

        if (_inputPane.State != InputPaneState.Open)
            return 0.0;

        var occluded = _inputPane.OccludedRect;
        if (occluded.Height <= 0)
            return 0.0;

        var origin = this.TranslatePoint(default, topLevel);
        if (origin is null)
            return occluded.Height;

        var hostBottom = origin.Value.Y + Bounds.Height;
        return Math.Max(0.0, hostBottom - occluded.Top);
    }

    private void EnsureFocusedVisible()
    {
        if (!AutoScrollFocusedElement) return;

        var topLevel = TopLevel.GetTopLevel(this);
        var focused = (topLevel?.FocusManager?.GetFocusedElement() as Control) ?? _lastFocused;
        if (focused is null || !this.IsVisualAncestorOf(focused))
            return;

        focused = ResolveInputContainer(focused);

        var scroll = focused.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();
        if (scroll is null)
        {
            focused.BringIntoView();
            return;
        }

        var ptInScroll = focused.TranslatePoint(default, scroll);
        if (ptInScroll is null)
            return;

        var elementTop = ptInScroll.Value.Y;
        var elementBottom = elementTop + focused.Bounds.Height + ExtraBottomSpacing;
        var viewportHeight = scroll.Viewport.Height > 0 ? scroll.Viewport.Height : scroll.Bounds.Height;
        if (viewportHeight <= 0) return;

        if (elementBottom > viewportHeight)
        {
            var delta = elementBottom - viewportHeight;
            scroll.Offset = new Vector(scroll.Offset.X, Math.Max(0, scroll.Offset.Y + delta));
        }
        else if (elementTop < ExtraBottomSpacing)
        {
            var delta = ExtraBottomSpacing - elementTop;
            scroll.Offset = new Vector(scroll.Offset.X, Math.Max(0, scroll.Offset.Y - delta));
        }
    }

    private Control ResolveInputContainer(Control control)
    {
        foreach (var ancestor in control.GetSelfAndVisualAncestors().OfType<Control>())
        {
            if (ReferenceEquals(ancestor, this))
                break;
            if (ancestor is MdTextField or MdTextArea or MdSearchBar or MdPinInput or MdComboBox)
                return ancestor;
        }
        return control;
    }
}
