namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class VisualSlider : ContentView
{
    public VisualSlider()
    {
        InitializeComponent();
        //Minimum = -50;
        //Maximum = 50;
        //Value = 0;
        this.PropertyChanged += VisualSlider_PropertyChanged;
    }

    private void VisualSlider_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Minimum))
        {
            //Slider.Minimum = Minimum;
            this.MinimumLabel.Text = String.Format(StringFormat, Minimum);
        }
        else if (e.PropertyName == nameof(Maximum))
        {
            //Slider.Maximum = Maximum;
            this.MaximumLabel.Text = String.Format(StringFormat, Maximum);
        }
        else if (e.PropertyName == nameof(Value))
        {
            //Slider.Value = Value;
            this.ValueLabel.Text = String.Format(StringFormat, Value);
        }
        else if (e.PropertyName == nameof(StringFormat))
        {
            this.MinimumLabel.Text = String.Format(StringFormat, Minimum);
            this.MaximumLabel.Text = String.Format(StringFormat, Maximum);
            this.ValueLabel.Text = String.Format(StringFormat, Value);
        }
    }

    private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        Value = (int)Math.Round(e.NewValue / 1);
    }

    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(nameof(Minimum), typeof(int), typeof(VisualSlider), -10000);
    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(nameof(Maximum), typeof(int), typeof(VisualSlider), 10000);
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(int), typeof(VisualSlider), 0, BindingMode.TwoWay);
    public static readonly BindableProperty StringFormatProperty = BindableProperty.Create(nameof(StringFormat), typeof(string), typeof(VisualSlider), "{0}");

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set=>SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string StringFormat
    {
        get => (string)GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }

}