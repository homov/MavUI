using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;

namespace Md3.Avalonia.Controls;

[Flags]
public enum MdSafeAreaEdges
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
public class MdSafeArea : MdContentView
{
    public static readonly StyledProperty<MdSafeAreaEdges> EdgesProperty =
        AvaloniaProperty.Register<MdSafeArea, MdSafeAreaEdges>(nameof(Edges), MdSafeAreaEdges.All);

    public static readonly StyledProperty<Thickness> BasePaddingProperty =
        AvaloniaProperty.Register<MdSafeArea, Thickness>(nameof(BasePadding));

    public static readonly StyledProperty<Thickness?> SimulatedInsetProperty =
        AvaloniaProperty.Register<MdSafeArea, Thickness?>(nameof(SimulatedInset));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MdSafeArea, Control?>(nameof(Body));

    private IInsetsManager? _insetsManager;
    private Thickness _appliedInset;

    public event EventHandler<Thickness>? InsetChanged;

    static MdSafeArea()
    {
        EdgesProperty.Changed.AddClassHandler<MdSafeArea>((s, _) => s.RefreshInsets());
        BasePaddingProperty.Changed.AddClassHandler<MdSafeArea>((s, _) => s.RefreshInsets());
        SimulatedInsetProperty.Changed.AddClassHandler<MdSafeArea>((s, _) => s.RefreshInsets());
        BodyProperty.Changed.AddClassHandler<MdSafeArea>((s, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MdVisualTreeHelpers.DetachFromParent(body);
            s.Content = body;
        });
    }

    public MdSafeArea() : this(null)
    {
    }

    public MdSafeArea(Control? content = null, MdSafeAreaEdges edges = MdSafeAreaEdges.All)
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

    public MdSafeAreaEdges Edges
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
            edges.HasFlag(MdSafeAreaEdges.Left) ? safe.Left : 0,
            edges.HasFlag(MdSafeAreaEdges.Top) ? safe.Top : 0,
            edges.HasFlag(MdSafeAreaEdges.Right) ? safe.Right : 0,
            edges.HasFlag(MdSafeAreaEdges.Bottom) ? safe.Bottom : 0);

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
