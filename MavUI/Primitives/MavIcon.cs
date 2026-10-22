using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Theming;

namespace Mav.Primitives;

/// <summary>
/// Renders a glyph from the bundled Material Design icon fonts
/// (Material Symbols Rounded, Material Icons, and Material Design Icons).
/// </summary>
public class MavIcon : TextBlock
{
    public static readonly StyledProperty<string?> GlyphProperty =
        AvaloniaProperty.Register<MavIcon, string?>(nameof(Glyph));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<MavIcon, double>(nameof(Size), 24.0);

    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<MavIcon, Color>(nameof(Color), Colors.Transparent);

    public static readonly StyledProperty<MavColorRole?> ColorRoleProperty =
        AvaloniaProperty.Register<MavIcon, MavColorRole?>(nameof(ColorRole));

    static MavIcon()
    {
        GlyphProperty.Changed.AddClassHandler<MavIcon>((icon, e) =>
        {
            var glyph = e.GetNewValue<string?>();
            icon.FontFamily = MavFonts.ResolveIconFont(glyph);
            icon.Text = glyph ?? string.Empty;
        });
        SizeProperty.Changed.AddClassHandler<MavIcon>((icon, e) =>
        {
            var sz = e.GetNewValue<double>();
            icon.FontSize = sz;
            icon.Width = sz;
            icon.Height = sz;
        });
        ColorProperty.Changed.AddClassHandler<MavIcon>((icon, e) =>
        {
            icon.UseThemeDefaultColor = false;
            icon.Foreground = MavTheme.BrushOf(e.GetNewValue<Color>());
        });
        ColorRoleProperty.Changed.AddClassHandler<MavIcon>((icon, e) =>
        {
            var role = e.GetNewValue<MavColorRole?>();
            if (role is not null)
                icon.Foreground = MavTheme.BrushOf(MavTheme.Current.Color.Get(role.Value));
            else if (icon.UseThemeDefaultColor)
                icon.Foreground = MavTheme.BrushOf(MavTheme.Current.Color.OnSurfaceVariant);
        });
    }

    /// <summary>Creates an empty 24 dp icon.</summary>
    public MavIcon() : this(null, 24, null)
    {
    }

    /// <summary>Creates an icon of the given <paramref name="size"/> that shows <paramref name="glyph"/> in the given color role.</summary>
    public MavIcon(string? glyph = null, double size = 24, MavColorRole? colorRole = null)
    {
        FontFamily = MavFonts.ResolveIconFont(glyph);
        TextAlignment = TextAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Center;
        IsHitTestVisible = false;
        Size = size;
        FontSize = size;
        Width = size;
        Height = size;
        if (glyph is not null)
        {
            Glyph = glyph;
            Text = glyph;
        }
        if (colorRole is not null)
            ColorRole = colorRole;

        MavTheme.Observe(this, (self, theme) =>
        {
            if (self.ColorRole is { } role)
                self.Foreground = MavTheme.BrushOf(theme.Color.Get(role));
            else if (self.UseThemeDefaultColor)
                self.Foreground = MavTheme.BrushOf(theme.Color.OnSurfaceVariant);
        });
    }

    public bool UseThemeDefaultColor { get; set; } = true;

    [Content]
    public string? Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public MavColorRole? ColorRole
    {
        get => GetValue(ColorRoleProperty);
        set => SetValue(ColorRoleProperty, value);
    }
}
