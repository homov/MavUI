using System.Windows.Input;
using Xamarin.CommunityToolkit.Markup;

namespace HeatController;

public class ToggleImageIcon : Image
{
    ICommand TapCommand;

    private void Tap()
    {
        IsToggled =  !IsToggled;
    }

    public ToggleImageIcon()
    {
        TapCommand = new Command(Tap);
        this.BindingContext = this;

        this.HorizontalOptions = LayoutOptions.Start;
        this.VerticalOptions = LayoutOptions.Center;

        this
            .Bind(FontImageIcon.WidthRequestProperty, nameof(Size), source: this)
            .Bind(FontImageIcon.HeightRequestProperty, nameof(Size), source: this);

        this.Source = new FontImageSource()
            .Bind(FontImageSource.FontFamilyProperty, nameof(FontFamily), source: this)
            .Bind(FontImageSource.GlyphProperty, nameof(CurrentIcon), source: this)
            .Bind(FontImageSource.SizeProperty, nameof(Size), source: this)
            .Bind(FontImageSource.ColorProperty, nameof(Color), source: this);

        this.GestureRecognizers.Add(new TapGestureRecognizer() {  NumberOfTapsRequired=1, Command=TapCommand });
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(ToggleImageIcon), Fonts.MaterialIconsRegular);
    public static readonly BindableProperty CurrentIconProperty = BindableProperty.Create(nameof(OnIcon), typeof(string), typeof(ToggleImageIcon), Icons.EyeOffOutline);
    public static readonly BindableProperty OnIconProperty = BindableProperty.Create(nameof(OnIcon), typeof(string), typeof(ToggleImageIcon), Icons.EyeOffOutline, propertyChanged: OnOnIconPropertyChanged);
    public static readonly BindableProperty OffIconProperty = BindableProperty.Create(nameof(OffIcon), typeof(string), typeof(ToggleImageIcon), Icons.EyeOutline, propertyChanged: OffIconPropertyChanged);
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(double), typeof(ToggleImageIcon), 20.0);
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(Color), typeof(ToggleImageIcon), Color.Black);
    public static readonly BindableProperty IsToggledPropery = BindableProperty.Create(nameof(IsToggled), typeof(bool), typeof(ToggleImageIcon), true, propertyChanged: OnIsToggledProperyChanged);

    private static void OnOnIconPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ToggleImageIcon toggle)
        {
            if (toggle.IsToggled)
                toggle.SetValue(CurrentIconProperty, newValue);
        }
    }

    private static void OffIconPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ToggleImageIcon toggle)
        {
            if (!toggle.IsToggled)
                toggle.SetValue(CurrentIconProperty, newValue);
        }
    }
    private static void OnIsToggledProperyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ToggleImageIcon toggle)
        {
            var isToggled = (bool)newValue;
            toggle.CurrentIcon =  isToggled ? toggle.OnIcon : toggle.OffIcon;
            ((FontImageSource)toggle.Source).Glyph = toggle.CurrentIcon;
        }
    }

    public bool IsToggled
    {
        get => (bool)GetValue(IsToggledPropery);
        set => SetValue(IsToggledPropery, value);
    }

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public string CurrentIcon
    {
        get => (string)GetValue(CurrentIconProperty);
        set => SetValue(CurrentIconProperty, value);
    }

    public string OffIcon
    {
        get => (string)GetValue(OffIconProperty);
        set => SetValue(OffIconProperty, value);
    }

    public string OnIcon
    {
        get => (string)GetValue(OnIconProperty);
        set => SetValue(OnIconProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}
