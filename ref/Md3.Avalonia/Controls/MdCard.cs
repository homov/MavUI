using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public enum MdCardVariant
{
    Elevated,
    Filled,
    Outlined,
}

/// <summary>
/// Material Design 3 card container with Elevated, Filled, and Outlined styles and optional interactive ripple.
/// </summary>
public class MdCard : MdContentView
{
    public static readonly StyledProperty<MdCardVariant> VariantProperty =
        AvaloniaProperty.Register<MdCard, MdCardVariant>(nameof(Variant), MdCardVariant.Elevated);

    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MdCard, bool>(nameof(IsInteractive));

    public static readonly StyledProperty<double> CardRadiusProperty =
        AvaloniaProperty.Register<MdCard, double>(nameof(CardRadius), 12.0);

    public static readonly StyledProperty<Thickness> CardPaddingProperty =
        AvaloniaProperty.Register<MdCard, Thickness>(nameof(CardPadding), new Thickness(16));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MdCard, Control?>(nameof(Body));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MdCard, ICommand?>(nameof(Command));

    private readonly MdInkSurface _surface;

    public event EventHandler? Clicked;

    static MdCard()
    {
        VariantProperty.Changed.AddClassHandler<MdCard>((c, _) => c.ApplyTheme(MdTheme.Current));
        IsInteractiveProperty.Changed.AddClassHandler<MdCard>((c, e) => c._surface.IsInteractive = e.GetNewValue<bool>());
        CardRadiusProperty.Changed.AddClassHandler<MdCard>((c, e) => c._surface.CornerRadius = e.GetNewValue<double>());
        CornerRadiusProperty.Changed.AddClassHandler<MdCard>((c, e) => c.CardRadius = e.GetNewValue<CornerRadius>().TopLeft);
        CardPaddingProperty.Changed.AddClassHandler<MdCard>((c, e) => c._surface.Padding = e.GetNewValue<Thickness>());
        BodyProperty.Changed.AddClassHandler<MdCard>((c, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MdVisualTreeHelpers.DetachFromParent(body);
            c._surface.Content = body;
        });
        CommandProperty.Changed.AddClassHandler<MdCard>((c, e) =>
        {
            var cmd = e.GetNewValue<ICommand?>();
            c._surface.Command = cmd;
            if (cmd is not null) c.IsInteractive = true;
        });
    }

    public MdCard() : this(MdCardVariant.Elevated)
    {
    }

    public MdCard(MdCardVariant variant = MdCardVariant.Elevated, Control? body = null, bool interactive = false)
    {
        _surface = new MdInkSurface
        {
            CornerRadius = 12,
            Padding = new Thickness(16),
            IsInteractive = interactive,
            Content = body,
        };
        _surface.Clicked += (_, _) =>
        {
            if (IsInteractive)
                Clicked?.Invoke(this, EventArgs.Empty);
        };

        base.Content = _surface;
        Variant = variant;
        IsInteractive = interactive;
        if (body is not null) Body = body;

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public MdCardVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    public bool IsClickable
    {
        get => IsInteractive;
        set => IsInteractive = value;
    }

    public double CardRadius
    {
        get => GetValue(CardRadiusProperty);
        set => SetValue(CardRadiusProperty, value);
    }

    public Thickness CardPadding
    {
        get => GetValue(CardPaddingProperty);
        set => SetValue(CardPaddingProperty, value);
    }

    [Content]
    public Control? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public new Control? Content
    {
        get => Body;
        set => Body = value;
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        var (bg, stroke, strokeW, elev) = Variant switch
        {
            MdCardVariant.Elevated => (c.SurfaceContainerLow, Colors.Transparent, 0.0, 1.0),
            MdCardVariant.Filled => (c.SurfaceContainerHighest, Colors.Transparent, 0.0, 0.0),
            MdCardVariant.Outlined => (c.Surface, c.OutlineVariant, 1.0, 0.0),
            _ => (c.SurfaceContainerLow, Colors.Transparent, 0.0, 1.0),
        };

        _surface.FillColor = bg;
        _surface.StrokeColor = stroke;
        _surface.StrokeThickness = strokeW;
        _surface.RippleColor = c.OnSurface;
        _surface.Elevation = elev;
        _surface.CornerRadius = CardRadius;
        _surface.Padding = CardPadding;
    }
}
