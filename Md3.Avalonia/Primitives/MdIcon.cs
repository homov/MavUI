using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Primitives;

/// <summary>
/// Renders a glyph from the bundled Material Design icon fonts
/// (Material Symbols Rounded, Material Icons, and Material Design Icons).
/// </summary>
public class MdIcon : TextBlock
{
    public static readonly StyledProperty<string?> GlyphProperty =
        AvaloniaProperty.Register<MdIcon, string?>(nameof(Glyph));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<MdIcon, double>(nameof(Size), 24.0);

    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<MdIcon, Color>(nameof(Color), Colors.Transparent);

    public static readonly StyledProperty<MdColorRole?> ColorRoleProperty =
        AvaloniaProperty.Register<MdIcon, MdColorRole?>(nameof(ColorRole));

    static MdIcon()
    {
        GlyphProperty.Changed.AddClassHandler<MdIcon>((icon, e) =>
        {
            var glyph = e.GetNewValue<string?>();
            icon.FontFamily = MdFonts.ResolveIconFont(glyph);
            icon.Text = glyph ?? string.Empty;
        });
        SizeProperty.Changed.AddClassHandler<MdIcon>((icon, e) =>
        {
            var sz = e.GetNewValue<double>();
            icon.FontSize = sz;
            icon.Width = sz;
            icon.Height = sz;
        });
        ColorProperty.Changed.AddClassHandler<MdIcon>((icon, e) =>
        {
            icon.UseThemeDefaultColor = false;
            icon.Foreground = MdTheme.BrushOf(e.GetNewValue<Color>());
        });
        ColorRoleProperty.Changed.AddClassHandler<MdIcon>((icon, e) =>
        {
            var role = e.GetNewValue<MdColorRole?>();
            if (role is not null)
                icon.Foreground = MdTheme.BrushOf(MdTheme.Current.Color.Get(role.Value));
            else if (icon.UseThemeDefaultColor)
                icon.Foreground = MdTheme.BrushOf(MdTheme.Current.Color.OnSurfaceVariant);
        });
    }

    public MdIcon() : this(null, 24, null)
    {
    }

    public MdIcon(string? glyph = null, double size = 24, MdColorRole? colorRole = null)
    {
        FontFamily = MdFonts.ResolveIconFont(glyph);
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

        MdTheme.Observe(this, (self, theme) =>
        {
            if (self.ColorRole is { } role)
                self.Foreground = MdTheme.BrushOf(theme.Color.Get(role));
            else if (self.UseThemeDefaultColor)
                self.Foreground = MdTheme.BrushOf(theme.Color.OnSurfaceVariant);
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

    public MdColorRole? ColorRole
    {
        get => GetValue(ColorRoleProperty);
        set => SetValue(ColorRoleProperty, value);
    }
}
