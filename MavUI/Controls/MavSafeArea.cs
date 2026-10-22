using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Mav.Primitives;

namespace Mav.Controls;

[Flags]
public enum MavSafeAreaEdges
{
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8,
    Horizontal = Left | Right,
    Vertical = Top | Bottom,
    All = Left | Top | Right | Bottom,
}

/// <summary>
/// Insets its child content by the <see cref="TopLevel"/>'s <see cref="IInsetsManager.SafeAreaPadding"/>
/// (status bar, display cutout/notch, navigation bar) masked by <see cref="Edges"/>.
/// </summary>
public class MavSafeArea : MavContentView
{
    public static readonly StyledProperty<MavSafeAreaEdges> EdgesProperty =
        AvaloniaProperty.Register<MavSafeArea, MavSafeAreaEdges>(nameof(Edges), MavSafeAreaEdges.All);

    public static readonly StyledProperty<Thickness> BasePaddingProperty =
        AvaloniaProperty.Register<MavSafeArea, Thickness>(nameof(BasePadding));

    public static readonly StyledProperty<Thickness?> SimulatedInsetProperty =
        AvaloniaProperty.Register<MavSafeArea, Thickness?>(nameof(SimulatedInset));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MavSafeArea, Control?>(nameof(Body));

    private IInsetsManager? _insetsManager;
    private Thickness _appliedInset;

    public event EventHandler<Thickness>? InsetChanged;

    static MavSafeArea()
    {
        EdgesProperty.Changed.AddClassHandler<MavSafeArea>((s, _) => s.RefreshInsets());
        BasePaddingProperty.Changed.AddClassHandler<MavSafeArea>((s, _) => s.RefreshInsets());
        SimulatedInsetProperty.Changed.AddClassHandler<MavSafeArea>((s, _) => s.RefreshInsets());
        BodyProperty.Changed.AddClassHandler<MavSafeArea>((s, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(body);
            s.Content = body;
        });
    }

    public MavSafeArea() : this(null)
    {
    }

    public MavSafeArea(Control? content = null, MavSafeAreaEdges edges = MavSafeAreaEdges.All)
    {
        Edges = edges;
        if (content is not null)
            Body = content;
    }

    [Content]
    public Control? Body
    {
        get => GetValue(BodyProperty) ?? Content;
        set => SetValue(BodyProperty, value);
    }

    public MavSafeAreaEdges Edges
    {
        get => GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    public Thickness BasePadding
    {
        get => GetValue(BasePaddingProperty);
        set => SetValue(BasePaddingProperty, value);
    }

    public Thickness? SimulatedInset
    {
        get => GetValue(SimulatedInsetProperty);
        set => SetValue(SimulatedInsetProperty, value);
    }

    public Thickness AppliedInset => _appliedInset;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachInsetsManager();
        RefreshInsets();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachInsetsManager();
        base.OnDetachedFromVisualTree(e);
    }

    public void RefreshInsets()
    {
        var safe = SimulatedInset ?? _insetsManager?.SafeAreaPadding ?? default;
        var edges = Edges;
        var masked = new Thickness(
            edges.HasFlag(MavSafeAreaEdges.Left) ? safe.Left : 0,
            edges.HasFlag(MavSafeAreaEdges.Top) ? safe.Top : 0,
            edges.HasFlag(MavSafeAreaEdges.Right) ? safe.Right : 0,
            edges.HasFlag(MavSafeAreaEdges.Bottom) ? safe.Bottom : 0);

        _appliedInset = masked;
        var basePad = BasePadding;
        Padding = new Thickness(
            basePad.Left + masked.Left,
            basePad.Top + masked.Top,
            basePad.Right + masked.Right,
            basePad.Bottom + masked.Bottom);

        InsetChanged?.Invoke(this, _appliedInset);
    }

    private void AttachInsetsManager()
    {
        DetachInsetsManager();
        _insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insetsManager is not null)
            _insetsManager.SafeAreaChanged += OnSafeAreaChanged;
    }

    private void DetachInsetsManager()
    {
        if (_insetsManager is not null)
        {
            _insetsManager.SafeAreaChanged -= OnSafeAreaChanged;
            _insetsManager = null;
        }
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => RefreshInsets();
}
