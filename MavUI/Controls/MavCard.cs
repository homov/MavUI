using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

public enum MavCardVariant
{
    Elevated,
    Filled,
    Outlined,
}

/// <summary>
/// Material Design 3 card container with Elevated, Filled, and Outlined styles and optional interactive ripple.
/// </summary>
public class MavCard : MavContentView
{
    public static readonly StyledProperty<MavCardVariant> VariantProperty =
        AvaloniaProperty.Register<MavCard, MavCardVariant>(nameof(Variant), MavCardVariant.Elevated);

    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<MavCard, bool>(nameof(IsInteractive));

    public static readonly StyledProperty<double> CardRadiusProperty =
        AvaloniaProperty.Register<MavCard, double>(nameof(CardRadius), 12.0);

    public static readonly StyledProperty<Thickness> CardPaddingProperty =
        AvaloniaProperty.Register<MavCard, Thickness>(nameof(CardPadding), new Thickness(16));

    public static readonly StyledProperty<Control?> BodyProperty =
        AvaloniaProperty.Register<MavCard, Control?>(nameof(Body));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavCard, ICommand?>(nameof(Command));

    private readonly MavInkSurface _surface;

    public event EventHandler? Clicked;

    static MavCard()
    {
        VariantProperty.Changed.AddClassHandler<MavCard>((c, _) => c.ApplyTheme(MavTheme.Current));
        IsInteractiveProperty.Changed.AddClassHandler<MavCard>((c, e) => c._surface.IsInteractive = e.GetNewValue<bool>());
        CardRadiusProperty.Changed.AddClassHandler<MavCard>((c, e) => c._surface.CornerRadius = e.GetNewValue<double>());
        CornerRadiusProperty.Changed.AddClassHandler<MavCard>((c, e) => c.CardRadius = e.GetNewValue<CornerRadius>().TopLeft);
        CardPaddingProperty.Changed.AddClassHandler<MavCard>((c, e) => c._surface.Padding = e.GetNewValue<Thickness>());
        BodyProperty.Changed.AddClassHandler<MavCard>((c, e) =>
        {
            var body = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(body);
            c._surface.Content = body;
        });
        CommandProperty.Changed.AddClassHandler<MavCard>((c, e) =>
        {
            var cmd = e.GetNewValue<ICommand?>();
            c._surface.Command = cmd;
            if (cmd is not null) c.IsInteractive = true;
        });
    }

    public MavCard() : this(MavCardVariant.Elevated)
    {
    }

    public MavCard(MavCardVariant variant = MavCardVariant.Elevated, Control? body = null, bool interactive = false)
    {
        _surface = new MavInkSurface
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

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public MavCardVariant Variant
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

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        var (bg, stroke, strokeW, elev) = Variant switch
        {
            MavCardVariant.Elevated => (c.SurfaceContainerLow, Colors.Transparent, 0.0, 1.0),
            MavCardVariant.Filled => (c.SurfaceContainerHighest, Colors.Transparent, 0.0, 0.0),
            MavCardVariant.Outlined => (c.Surface, c.OutlineVariant, 1.0, 0.0),
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
