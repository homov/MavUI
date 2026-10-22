
using System.Runtime.CompilerServices;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class TemperatureEdit : ContentView
{
    public TemperatureEdit()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty ShowButtonsProperty = BindableProperty.Create(
       nameof(ShowButtons),
       typeof(bool),
       typeof(TemperatureEdit),
       true);

    public bool ShowButtons
    {
        get => (bool)GetValue(ShowButtonsProperty);
        set => SetValue(ShowButtonsProperty, value);
    }

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), 
        typeof(double), 
        typeof(TemperatureEdit), 
        0.0, 
        BindingMode.TwoWay,
        propertyChanged: OnValuePropertyChanged);

    public static readonly BindableProperty MinValueProperty = BindableProperty.Create(
       nameof(MinValue),
       typeof(double),
       typeof(TemperatureEdit),
       8.0,
       BindingMode.OneWay);

    public static readonly BindableProperty MaxValueProperty = BindableProperty.Create(
      nameof(MaxValue),
      typeof(double),
      typeof(TemperatureEdit),
      32.0,
      BindingMode.OneWay);

    public static readonly BindableProperty ValueNameProperty = BindableProperty.Create(
        nameof(ValueName), 
        typeof(string), 
        typeof(TemperatureEdit),
        null,
        BindingMode.OneWay
        );

    public static readonly BindableProperty ValueFormatProperty = BindableProperty.Create(
        nameof(ValueFormat),
        typeof(string),
        typeof(TemperatureEdit),
        "{0}°C",
        BindingMode.OneWay,
        propertyChanged: OnValueFormatPropertyChanged
        );


    public static readonly BindableProperty DisplayValueProperty = BindableProperty.Create(
       nameof(DisplayValue),
       typeof(string),
       typeof(TemperatureEdit),
       null,
       BindingMode.OneWay
       );

    public string DisplayValue
    {
        get => (string)GetValue(DisplayValueProperty);
        set => SetValue(DisplayValueProperty, value);
    }

    public string ValueFormat
    {
        get => (string)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    public string ValueName
    {
        get => (string)GetValue(ValueNameProperty);
        set => SetValue(ValueNameProperty, value);
    }


    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double MinValue
    {
        get => (double)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    private static void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureEdit edit)
        {
            edit._pointer.Value = (double)newValue;
            if (edit.ValueFormat != null)
            {
                edit.DisplayValue = String.Format(edit.ValueFormat, newValue);
            }
            else
            {
                edit.DisplayValue = String.Format("{0}", newValue);
            }
        }
    }

    private static void OnValueFormatPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TemperatureEdit edit)
        {
            if (edit.DisplayValue != null)
            {
                edit.DisplayValue = String.Format(edit.ValueFormat, edit.Value);
            }
            else
            {
                edit.DisplayValue = String.Format("{0}", edit.Value);
            }
        }
    }

    private void DecreseButton_Clicked(object sender, EventArgs e)
    {
        if (Value > _scale.StartValue)
            Value -= 1;
    }

    private void IncreseButton_Clicked(object sender, EventArgs e)
    {
        if (Value < _scale.EndValue)
            Value += 1;
    }
}