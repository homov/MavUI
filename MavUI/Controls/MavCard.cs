using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 card container with Elevated, Filled, and Outlined styles and optional interactive ripple.
/// The card is an ink surface: its content is the <see cref="MavInkSurface.Child"/> (the [Content] property).
/// </summary>
public class MavCard : MavInkSurface
{
    public static readonly StyledProperty<MavCardVariant> VariantProperty =
        AvaloniaProperty.Register<MavCard, MavCardVariant>(nameof(Variant), MavCardVariant.Elevated);

    /// <summary>
    /// Corner radius. The default is <see cref="double.NaN"/>, which means "follow the theme":
    /// <see cref="MavShapeTokens.Medium"/> scaled by <see cref="MavTheme.ShapeScaleMode"/>. Set a number
    /// to pin the radius and opt out of the shape scale for this card.
    /// </summary>
    public static readonly StyledProperty<double> CardRadiusProperty =
        AvaloniaProperty.Register<MavCard, double>(nameof(CardRadius), double.NaN);

    public static readonly StyledProperty<Thickness> CardPaddingProperty =
        AvaloniaProperty.Register<MavCard, Thickness>(nameof(CardPadding), new Thickness(16));

    static MavCard()
    {
        VariantProperty.Changed.AddClassHandler<MavCard>((c, _) => c.ApplyTheme(MavTheme.Current));
        CardRadiusProperty.Changed.AddClassHandler<MavCard>((c, e) =>
        {
            var radius = e.GetNewValue<double>();
            if (!double.IsNaN(radius))
                c.CornerRadius = Math.Max(0, radius);
        });
        CardPaddingProperty.Changed.AddClassHandler<MavCard>((c, e) => c.Padding = e.GetNewValue<Thickness>());
    }

    /// <summary>Creates an elevated card with no content.</summary>
    public MavCard() : this(MavCardVariant.Elevated)
    {
    }

    /// <summary>Creates a card in the given <paramref name="variant"/>, with optional body content and interaction.</summary>
    public MavCard(MavCardVariant variant = MavCardVariant.Elevated, Control? body = null, bool interactive = false)
    {
        CornerRadius = MavTheme.Current.Shape.Medium;
        Padding = new Thickness(16);
        IsInteractive = interactive;
        if (body is not null)
            Child = body;

        Variant = variant;

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public MavCardVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
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

        FillColor = bg;
        StrokeColor = stroke;
        StrokeThickness = strokeW;
        RippleColor = c.OnSurface;
        Elevation = elev;
        // NaN means "follow the theme"; a number pins the radius (see CardRadius).
        CornerRadius = double.IsNaN(CardRadius) ? t.Shape.Medium : Math.Max(0, CardRadius);
        Padding = CardPadding;
    }
}
