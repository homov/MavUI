using HeatController.Models;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ScheduleView : ContentView
{
    public ScheduleView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty ScheduleModelProperty = BindableProperty.Create(nameof(ScheduleModel), typeof(ScheduleModel), typeof(ScheduleView), null,
        propertyChanged: OnScheduleModelPropertyChanged);

    private static void OnScheduleModelPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ScheduleView scheduleView)
        {
            scheduleView.RootGrid.BindingContext = newValue;
        }
    }

    public ScheduleModel ScheduleModel
    {
        get => (ScheduleModel)GetValue(ScheduleModelProperty);
        set => SetValue(ScheduleModelProperty, value);
    }

}