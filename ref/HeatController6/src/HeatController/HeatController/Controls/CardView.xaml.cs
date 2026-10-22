namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]

[ContentProperty("Body")]
public partial class CardView : ContentView
{
    public CardView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty HeaderBackgroundColorProperty = BindableProperty.Create(nameof(HeaderBackgroundColor), typeof(Color), typeof(CardView), Color.Gray);
    public static readonly BindableProperty BodyBackgroundColorProperty = BindableProperty.Create(nameof(BodyBackgroundColor), typeof(Color), typeof(CardView), Color.White);

    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(View), typeof(CardView), null);
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(CardView), null);

    public static readonly BindableProperty HeaderHeightProperty = BindableProperty.Create(nameof(HeaderHeight), typeof(double), typeof(CardView), 38.0);

    public double HeaderHeight
    {
        set => SetValue(HeaderHeightProperty, value);
        get => (double)GetValue(HeaderHeightProperty);
    }

    public Color HeaderBackgroundColor
    {
        set => SetValue(HeaderBackgroundColorProperty, value);
        get=> (Color)GetValue(HeaderBackgroundColorProperty);
    }

    public Color BodyBackgroundColor
    {
        set => SetValue(BodyBackgroundColorProperty, value);
        get => (Color)GetValue(BodyBackgroundColorProperty);
    }

    public View Header
    {
        set => SetValue(HeaderProperty, value);
        get => (View)GetValue(HeaderProperty);
    }

    public View Body
    {
        set => SetValue(BodyProperty, value);
        get => (View)GetValue(BodyProperty);
    }

}