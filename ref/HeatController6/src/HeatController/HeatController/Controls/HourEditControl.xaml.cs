using Syncfusion.SfGauge.XForms;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class HourEditControl : SfCircularGauge
{
    public double StartValue
    {
        get => Start.Value;
        set
        {
            Start.Value = value;
            UpdateText(Start.Value, End.Value);
        }
    }

    public double EndValue
    {
        get => End.Value;
        set
        {
            End.Value = value;
            UpdateText(Start.Value, End.Value);
        }
    }

    private void UpdateText(double start, double end)
    {
        Header.Text = $"{start:00}:00 - {end:00}:00";
    }

    public HourEditControl()
    {
        InitializeComponent();
    }

    private void Start_ValueChanging(object sender, PointerValueChangingEventArgs e)
    {
            if (e.NewValue >= End.Value)
            {
                e.Cancel = true;
                return;
            }
            UpdateText(e.NewValue, End.Value);
    }

    private void End_ValueChanging(object sender, PointerValueChangingEventArgs e)
    {
        if (e.NewValue <= Start.Value)
        {
            e.Cancel = true;
            return;
        }
        UpdateText(Start.Value, e.NewValue);
    }
}

