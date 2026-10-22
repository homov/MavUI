using HeatController.Models;
using System.Windows.Input;

namespace HeatController;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ScheduleDayView : ContentView
{

    public ICommand EditHourRangeCommand { get; }
    public ICommand EditTemperatureCommand { get; }

    public NavigationService navigationService = ContainerService.Container.Resolve<NavigationService>();

    public ScheduleDayView()
    {
        EditHourRangeCommand = new AsyncRelayCommand<ScheduleHourModel>(EditHourRange);
        EditTemperatureCommand = new AsyncRelayCommand<ScheduleHourModel>(EditHourValue);
        InitializeComponent();
    }


    public async Task EditHourRange(ScheduleHourModel model)
    {
        if (!IsHourEditEnabled)
            return;

        var hourRange = new HourRange()
        {
            Start = model.StartHour,
            End = model.EndHour
        };
        var newRange =  (HourRange)await navigationService.ShowPopup(new HourEditPopup(hourRange));
        if (newRange!=null && (newRange.Start != model.StartHour || newRange.End != model.EndHour) && newRange.Start!=newRange.End)
        {
            Day.UpdateHours(newRange.Start, newRange.End, model);
        }
    }


    public async Task EditHourValue(ScheduleHourModel model)
    {
        double? oldValue = model.Value;
        if (oldValue.HasValue)
        {
            var defaultMin = ScheduleModel.GetMinimumValue( model.ScheduleModel.ScheduleType );
            var defaultMax = ScheduleModel.GetMaximumValue( model.ScheduleModel.ScheduleType );
            double? newValue =  
                (double?) await navigationService.ShowPopup(
                    new TemperatureEditPopup(oldValue.Value,
                    model.ScheduleModel.MinValue.HasValue ? model.ScheduleModel.MinValue.Value : defaultMin,
                    model.ScheduleModel.MaxValue.HasValue ? model.ScheduleModel.MaxValue.Value : defaultMax,
                    model.ScheduleModel.ControlValueName,
                    model.ScheduleModel.ControlValueFormat
                    ));
            if (newValue != null && newValue != oldValue)
                model.Value = (int?)newValue;
        }
    }

    public static readonly BindableProperty DayProperty = BindableProperty.Create(nameof(Day), typeof(ScheduleWeekDayModel), typeof(ScheduleDayView), null);

    public ScheduleWeekDayModel Day
    {
        get => (ScheduleWeekDayModel)GetValue(DayProperty);
        set =>SetValue(DayProperty, value);
    }

    public static readonly BindableProperty IsValueControlProperty = BindableProperty.Create(nameof(IsValueControl), typeof(bool), typeof(ScheduleDayView), true);

    public bool IsValueControl
    {
        get => (bool)GetValue(IsValueControlProperty);
        set => SetValue(IsValueControlProperty, value);
    }

    public static readonly BindableProperty ControlValueNameProperty = BindableProperty.Create(nameof(ControlValueName), typeof(string), typeof(ScheduleDayView), null);

    public string ControlValueName
    {
        get => (string)GetValue(ControlValueNameProperty);
        set => SetValue(ControlValueNameProperty, value);
    }

    public static readonly BindableProperty IsHourEditEnabledProperty = BindableProperty.Create(nameof(IsHourEditEnabled), typeof(bool), typeof(ScheduleDayView), true);

    public bool IsHourEditEnabled
    {
        get => (bool)GetValue(IsHourEditEnabledProperty);
        set => SetValue(IsHourEditEnabledProperty, value);
    }
}