using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Insets bottom padding when the soft keyboard (<see cref="IInputPane"/>) occludes the host
/// and automatically scrolls the focused input field into view inside any descendant <see cref="ScrollViewer"/>.
/// </summary>
public class MavKeyboardAvoidingHost : MavContentView
{
    public static readonly StyledProperty<double> ExtraBottomSpacingProperty =
        AvaloniaProperty.Register<MavKeyboardAvoidingHost, double>(nameof(ExtraBottomSpacing), 16.0);

    public static readonly StyledProperty<bool> AnimateInsetProperty =
        AvaloniaProperty.Register<MavKeyboardAvoidingHost, bool>(nameof(AnimateInset), true);

    public static readonly StyledProperty<bool> AutoScrollFocusedElementProperty =
        AvaloniaProperty.Register<MavKeyboardAvoidingHost, bool>(nameof(AutoScrollFocusedElement), true);

    public static readonly StyledProperty<double> SimulatedKeyboardHeightProperty =
        AvaloniaProperty.Register<MavKeyboardAvoidingHost, double>(nameof(SimulatedKeyboardHeight), 0.0);

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MavKeyboardAvoidingHost, Control?>(nameof(Body));

    private IInputPane? _inputPane;
    private WeakReference<Control>? _lastFocused;
    private double _keyboardHeight;
    private double _animatedBottom;

    // Previews set SimulatedKeyboardHeight so overlays can be tested on desktop. The
    // value is global to MavOverlay, so it must only be contributed while the host is
    // actually on screen; otherwise leaving a preview page keeps dialogs/sheets lifted
    // by that page's simulated keyboard.
    private static readonly List<WeakReference<MavKeyboardAvoidingHost>> ActiveHosts = [];

    public event EventHandler<double>? KeyboardHeightChanged;

    static MavKeyboardAvoidingHost()
    {
        ExtraBottomSpacingProperty.Changed.AddClassHandler<MavKeyboardAvoidingHost>((h, _) => h.EnsureFocusedVisible());
        SimulatedKeyboardHeightProperty.Changed.AddClassHandler<MavKeyboardAvoidingHost>((h, _) =>
        {
            h.UpdateKeyboardInset();
            // Also lift overlays (dialogs, sheets, popovers, search view) in previews.
            SyncOverlayKeyboardHeight();
        });
        BodyProperty.Changed.AddClassHandler<MavKeyboardAvoidingHost>((h, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(body);
            h.Content = body;
        });
    }

    public MavKeyboardAvoidingHost() : this(null)
    {
    }

    public MavKeyboardAvoidingHost(Control? content = null)
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
        RegisterActiveHost(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        MavMotion.Cancel(this, "kb_inset");
        DetachInputPane();
        _lastFocused = null;
        UnregisterActiveHost(this);
        base.OnDetachedFromVisualTree(e);
    }

    private static void RegisterActiveHost(MavKeyboardAvoidingHost host)
    {
        for (var i = ActiveHosts.Count - 1; i >= 0; i--)
        {
            if (!ActiveHosts[i].TryGetTarget(out var existing))
                ActiveHosts.RemoveAt(i);
            else if (ReferenceEquals(existing, host))
                return;
        }

        ActiveHosts.Add(new WeakReference<MavKeyboardAvoidingHost>(host));
        SyncOverlayKeyboardHeight();
    }

    private static void UnregisterActiveHost(MavKeyboardAvoidingHost host)
    {
        for (var i = ActiveHosts.Count - 1; i >= 0; i--)
        {
            if (!ActiveHosts[i].TryGetTarget(out var existing) || ReferenceEquals(existing, host))
                ActiveHosts.RemoveAt(i);
        }

        SyncOverlayKeyboardHeight();
    }

    /// <summary>
    /// Publishes the largest simulated keyboard inset among the hosts that are
    /// currently attached to a visual tree. Detached preview pages contribute 0.
    /// </summary>
    private static void SyncOverlayKeyboardHeight()
    {
        var height = 0.0;
        for (var i = ActiveHosts.Count - 1; i >= 0; i--)
        {
            if (!ActiveHosts[i].TryGetTarget(out var host))
            {
                ActiveHosts.RemoveAt(i);
                continue;
            }

            if (TopLevel.GetTopLevel(host) is not null)
                height = Math.Max(height, host.SimulatedKeyboardHeight);
        }

        Platform.MavOverlay.SetSimulatedKeyboardHeight(height);
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
            _lastFocused = new WeakReference<Control>(ResolveInputContainer(control));
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
            MavMotion.Animate(this, "kb_inset", _animatedBottom, targetBottom, MavMotion.Medium1, MavMotion.CubicOut, v =>
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
        var focused = topLevel?.FocusManager?.GetFocusedElement() as Control;
        if (focused is null && _lastFocused is not null && _lastFocused.TryGetTarget(out var last))
            focused = last;
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
            if (ancestor is MavTextField or MavTextArea or MavSearchBar or MavPinInput or MavComboBox)
                return ancestor;
        }
        return control;
    }
}
