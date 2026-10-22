namespace HeatController;

public class FontIcon : Label
{
    public FontIcon()
    {
        this.FontFamily = Fonts.MaterialIconsRegular;
        this.FontSize = 24;
        this.Text = Icons.Image;
    }

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(FontIcon), Icons.Image, propertyChanged: OnIconPropertyChanged);

    private static void OnIconPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FontIcon fontIcon)
        {
            fontIcon.Text = (string)newValue;
        }
    }

    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }
}