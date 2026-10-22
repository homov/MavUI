using Xamarin.CommunityToolkit.Markup;

namespace HeatController;

public class FontImageIcon : Image
{
    public FontImageIcon()
    {
        this.BindingContext = this;

        this.HorizontalOptions = LayoutOptions.Start;
        this.VerticalOptions = LayoutOptions.Center;

        this
            .Bind(FontImageIcon.WidthRequestProperty, nameof(Size), source: this)
            .Bind(FontImageIcon.HeightRequestProperty, nameof(Size), source: this);

        this.Source = new FontImageSource()
            .Bind(FontImageSource.FontFamilyProperty, nameof(FontFamily), source: this)
            .Bind(FontImageSource.GlyphProperty, nameof(Icon), source: this)
            .Bind(FontImageSource.SizeProperty, nameof(Size), source: this)
            .Bind(FontImageSource.ColorProperty, nameof(Color), source: this);
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(FontImageIcon), Fonts.MaterialIconsRegular);
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(FontImageIcon), Icons.Image, propertyChanged: OnIconPropertyChanged);

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double), typeof(FontImageIcon), 24.0);
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(FontImageIcon), Color.Black);

    private static void OnIconPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FontImageIcon icon)
        {
            ((FontImageSource)icon.Source).Glyph = (string)newValue;
        }
    }

    public string FontFamily
    {
        get { return (string)GetValue(FontFamilyProperty); }
        set { SetValue(FontFamilyProperty, value); }
    }

    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    public double Size
    {
        get { return (double)GetValue(SizeProperty); }
        set { SetValue(SizeProperty, value); }
    }

    public Color Color
    {
        get { return (Color)GetValue(ColorProperty); }
        set { SetValue(ColorProperty, value); }
    }
}
