using CommunityToolkit.Mvvm.Messaging;
using HeatCeram.Api.Models;
using HeatController.Messages;
using HeatController.Models;
using System.Linq;

namespace HeatController;

public partial class DevicePageViewModel : BaseViewModel
{
    HeatCeramService heatCeramService = ContainerService.Container.Resolve<HeatCeramService>();

    public DevicePageViewModel(UserDevice userDevice)
    {
        UserDevice = userDevice;
    }


    private UserDevice userDevice;
    public UserDevice UserDevice
    {
        get => userDevice;
        set
        {
            if (SetProperty(ref userDevice, value))
            {
                DeviceName = userDevice.Info.DeviceName;
                Hysteresis = userDevice.Info.Gistereses.HasValue ? userDevice.Info.Gistereses.Value : 0;
                AntiFreezeMode = userDevice.Info.AntifreezeBit.HasValue ? (userDevice.Info.AntifreezeBit.Value == 1 ? true : false) : false;
                HumiMode = userDevice.Info.HumiBit.HasValue ? (userDevice.Info.HumiBit.Value == 1 ? true : false) : false;
                PanelTempCoff = userDevice.Info.PanelTempCoff.HasValue ? (int)userDevice.Info.PanelTempCoff.Value : 0;
                RoomTempCoff = userDevice.Info.RoomTempCoff.HasValue ? (int)userDevice.Info.RoomTempCoff.Value : 0;
                var temperature = userDevice.Info.NeedTempPanel.HasValue ? (int)userDevice.Info.NeedTempPanel.Value : 0;
                PanelTemp = temperature;

                var task = Task.Run(async () => await Load());
                Task.WaitAll(task);
            }
        }
    }

    [ObservableProperty] int minValue;
    [ObservableProperty] int maxValue;


    public bool CanSetControl =>
        UserDevice != null &&
        UserDevice.Info.IsNotSensor &&
        (SelectedLocation == null || (SelectedLocation != null && SelectedLocation.Info.GroupId == null));
        
    public bool CanSetSensor =>
        UserDevice != null 
        && UserDevice.Info.IsNotSensor  
        && (SelectedLocation == null || (SelectedLocation != null && SelectedLocation.Info.GroupId== null))
        && (SelectedScheduleType == null || SelectedScheduleType.Id != ScheduleTypes.Humidity);

    public bool CanSetHysteresis => UserDevice != null && UserDevice.Info.ControlType != null && UserDevice.Info.Sensors!=null && UserDevice.Info.Sensors.Contains("t_room");
    
    public bool CanSetPanelTemperatureCoefficient => 
        UserDevice != null 
        && UserDevice.Info.Sensors!=null 
        && UserDevice.Info.Sensors.Contains("t_panel")
        && (SelectedScheduleType == null || SelectedScheduleType.Id != ScheduleTypes.Humidity);

    public bool CanSetRoomTemperatureCoefficient => 
        UserDevice != null 
        && UserDevice.Info.Sensors!=null 
        && UserDevice.Info.Sensors.Contains("t_room")
        && (SelectedScheduleType == null || SelectedScheduleType.Id != ScheduleTypes.Humidity);

    public bool CanSetPanelTemperature =>
        UserDevice != null 
        && UserDevice.Info.Sensors!=null 
        && UserDevice.Info.Sensors.Contains("t_panel")
        && (SelectedScheduleType == null || SelectedScheduleType.Id != ScheduleTypes.Humidity);

    public bool CanSetAntiFreezeMode => 
        UserDevice != null 
        && UserDevice.Info.Sensors != null
        && UserDevice.Info.IsNotSensor
        && (UserDevice.Info.ControlType!=null && UserDevice.Info.ControlType.Contains("temperature"))
        && UserDevice.Info.Sensors.Contains("t_room")
        && (SelectedScheduleType == null || SelectedScheduleType.Id!=ScheduleTypes.Humidity);

    public bool CanSetHumiMode =>
        UserDevice != null
        && UserDevice.Info.Sensors != null
        && UserDevice.Info.IsNotSensor
        && (UserDevice.Info.ControlType != null && UserDevice.Info.ControlType.Contains("humidity"))
        && (SelectedScheduleType == null || SelectedScheduleType.Id == ScheduleTypes.Humidity);



    [ObservableProperty] string deviceName;

    [ObservableProperty] int hysteresis;
    [ObservableProperty] int minHysteresis=1;
    [ObservableProperty] int maxHysteresis=10;
    public string HysteresisFormat => 
        (SelectedScheduleType != null && SelectedScheduleType.Id == ScheduleTypes.Humidity) ? "{0}%": "{0}°C";

    public string ManualFormat =>
    (SelectedScheduleType != null && SelectedScheduleType.Id == ScheduleTypes.Humidity) ? "{0}%" : "{0}°C";

    [ObservableProperty] bool antiFreezeMode;
    [ObservableProperty] bool humiMode;

    [ObservableProperty] int panelTempCoff;
    [ObservableProperty] int roomTempCoff;
    
    [ObservableProperty] int panelTemp=0;
    [ObservableProperty] bool showAdvacedTemperatureControls;

    [ObservableProperty] List<ScheduleItemType> scheduleTypeList;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSetSensor))]
    [NotifyPropertyChangedFor(nameof(CanSetPanelTemperatureCoefficient))]
    [NotifyPropertyChangedFor(nameof(CanSetRoomTemperatureCoefficient))]
    [NotifyPropertyChangedFor(nameof(CanSetPanelTemperature))]
    [NotifyPropertyChangedFor(nameof(CanSetAntiFreezeMode))]
    [NotifyPropertyChangedFor(nameof(CanSetHumiMode))]
    [NotifyPropertyChangedFor(nameof(HysteresisFormat))]
    ScheduleItemType selectedScheduleType;

    [ObservableProperty] List<UserDevice> sensorList;

    [ObservableProperty] UserDevice selectedSensor;

    [ObservableProperty] List<UserLocation> locationList;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSetControl))]
    [NotifyPropertyChangedFor(nameof(CanSetSensor))]
    UserLocation selectedLocation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HysteresisFormat))]
    ScheduleModel scheduleModel;

    [ObservableProperty]
    int selectedPage = 0;


    async Task Load()
    {
        if ( UserDevice.Info.IsNotSensor )
        {
            var devices = await heatCeramService.GetDevices();
            SensorList = devices.Where(it => it.Info.IsSensor).OrderBy(it => it.Name).ToList();
            SensorList.Insert(0, new UserDevice(new UserDeviceInfo() { DeviceName = Resources.WithoutSensor, DeviceId = null }));
            SelectedSensor = SensorList.FirstOrDefault(it => it.Id == UserDevice.Info.ExternalSensorId);
        }
        if ( UserDevice.Info.ScheduleId != null )
        {
            UserDevice.Schedule = await heatCeramService.LoadSchedule(UserDevice.Info.ScheduleId);
            var model = ScheduleModel.FromSchedule(userDevice.Schedule);
            model.MaxValue = ScheduleModel.GetMaximumValue(model.ScheduleType);
            model.MinValue = ScheduleModel.GetMinimumValue(model.ScheduleType);
            ScheduleTypeList = ScheduleItemType.GetScheduleItemTypes(userDevice.Info.ControlType);
            SelectedScheduleType = ScheduleTypeList.FirstOrDefault(it => it.Id == userDevice.Schedule.ScheduleType);
            ScheduleModel = model;
        }
        if (userDevice.DeviceType != null)
        {
            var info = UserDevice.DeviceType.DeviceTypeInfo;
            if (UserDevice.Info.ScheduleId != null)
            {
                
                if (ScheduleModel.ScheduleType == ScheduleTypes.Humidity)
                {
                    ScheduleModel.MinValue = this.MinValue = info.MinHumidity.HasValue ? 
                        info.MinHumidity.Value : ScheduleModel.GetMinimumValue(ScheduleModel.ScheduleType);
                    ScheduleModel.MaxValue = this.MaxValue = info.MaxHumidity.HasValue ?
                        info.MaxHumidity.Value : ScheduleModel.GetMaximumValue(ScheduleModel.ScheduleType);
                }
                else
                {
                    ScheduleModel.MinValue = this.MinValue = info.MinTemperature.HasValue ?
                       info.MinTemperature.Value : ScheduleModel.GetMinimumValue(ScheduleModel.ScheduleType);
                    ScheduleModel.MaxValue = this.MaxValue = info.MaxTemperature.HasValue ?
                        info.MaxTemperature.Value : ScheduleModel.GetMaximumValue(ScheduleModel.ScheduleType);
                }
            }
            else
            {
                MinValue = ScheduleModel.GetMinimumValue("");
                MaxValue = ScheduleModel.GetMinimumValue("");
            
            }
            MinHysteresis = info.MinHysteresis.HasValue ? info.MinHysteresis.Value : 1;
            MaxHysteresis = info.MaxHysteresis.HasValue ? info.MaxHysteresis.Value : 10;
        }
        if ( UserDevice.Info.GroupWork )
        {
            LocationList = (await heatCeramService.GetLocations()).OrderBy(it=>it.Name).ToList();
            LocationList.Insert(0, new UserLocation(new UserGroupInfo() { Name = Resources.WithoutLocation }));
            SelectedLocation = LocationList.FirstOrDefault(it => it.Info.GroupId == UserDevice.Info.GroupId);
        }
    }

    [RelayCommand]
    async Task Action(int index)
    {
        switch( index )
        {
            case 0:
                if (CanSetControl)
                    await SaveSchedule();
                break;
            case 1:
                await SaveInfo();
                break;
        }
    }

    [RelayCommand]
    async Task SaveSchedule()
    {
        try
        {
            using var busy = Busy();
            await heatCeramService.UpdateSchedule(ScheduleModel);
            await messageBoxService.Information(Resources.Saved);
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

    [RelayCommand]
    async Task SaveInfo()
    {
        try
        {
            using var busy = Busy();
            bool isAnySaved = false;
            
            if (DeviceName != UserDevice.Info.DeviceName)
            {
                if (String.IsNullOrEmpty(DeviceName))
                {
                    throw new ValidationException(Resources.NameNotSpecified);
                }
                await heatCeramService.SetDeviceName(UserDevice.Info.DeviceId, DeviceName);
                UserDevice.Info.DeviceName = DeviceName;
                isAnySaved = true;
            }
            if (UserDevice.Info.GroupWork && SelectedLocation!=null && SelectedLocation.Info.GroupId!= UserDevice.Info.GroupId)
            {
                await heatCeramService.SetDeviceGroup(UserDevice.Info.DeviceId, SelectedLocation.Info.GroupId);
                SelectedLocation.Info.GroupId = SelectedLocation.Info.GroupId;
                isAnySaved = true;
            }
            if (UserDevice.Info.IsNotSensor 
                && ( (SelectedLocation != null && SelectedLocation.Info.GroupId == null) || (SelectedLocation==null) )
                && SelectedSensor != null
                && SelectedSensor.Id != UserDevice.Info.ExternalSensorId
                )
            {
                await heatCeramService.SetDeviceSensor(UserDevice.Info.DeviceId, SelectedSensor.Id);
                UserDevice.Info.ExternalSensorId = SelectedSensor.Id;
                isAnySaved = true;
            }
            if (CanSetHysteresis && Hysteresis!= UserDevice.Info.Gistereses)
            {
                await heatCeramService.SetHisteresis(UserDevice.Info.DeviceId, Hysteresis);
                UserDevice.Info.Gistereses = Hysteresis;
                isAnySaved = true;
            }
            if (CanSetAntiFreezeMode && AntiFreezeMode != (UserDevice.Info.AntifreezeBit==1) )
            {
                await heatCeramService.SetAntifreezeMode(UserDevice.Info.DeviceId, AntiFreezeMode);
                UserDevice.Info.AntifreezeBit = AntiFreezeMode ? 1 : 0;
                isAnySaved = true;
            }
            if (CanSetHumiMode && HumiMode != (UserDevice.Info.HumiBit == 1))
            {
                await heatCeramService.SetHumiMode(UserDevice.Info.DeviceId, HumiMode);
                UserDevice.Info.HumiBit = HumiMode ? 1 : 0;
                isAnySaved = true;
            }
            if (CanSetRoomTemperatureCoefficient && RoomTempCoff != UserDevice.Info.RoomTempCoff )
            {
                await heatCeramService.SetRoomCoeff(UserDevice.Info.DeviceId, RoomTempCoff);
                UserDevice.Info.RoomTempCoff = (float)RoomTempCoff;
                isAnySaved = true;
            }
            if (CanSetPanelTemperatureCoefficient && PanelTempCoff != UserDevice.Info.PanelTempCoff)
            {
                await heatCeramService.SetPanelCoeff(UserDevice.Info.DeviceId, PanelTempCoff);
                UserDevice.Info.PanelTempCoff = (float)PanelTempCoff;
                isAnySaved = true;
            }
            if (CanSetPanelTemperature && PanelTemp != UserDevice.Info.NeedTempPanel)
            {
                await heatCeramService.SetPanelTemp(UserDevice.Info.DeviceId, PanelTemp);
                UserDevice.Info.NeedTempPanel = (float)PanelTemp;
                isAnySaved = true;
            }
            if (SelectedScheduleType != null && ScheduleModel!=null && ScheduleModel.ScheduleType != SelectedScheduleType.Id)
            {
                var schType = SelectedScheduleType.Id;
                await heatCeramService.UpdateScheduleType(ScheduleModel.ScheduleId, schType);
                var schedule = await heatCeramService.LoadSchedule(ScheduleModel.ScheduleId);
                UserDevice.Schedule = schedule;

                var model = HeatController.Models.ScheduleModel.FromSchedule(schedule);
                model.MaxValue = ScheduleModel.GetMaximumValue(model.ScheduleType);
                model.MinValue = ScheduleModel.GetMinimumValue(model.ScheduleType);
               
                var info = UserDevice.DeviceType.DeviceTypeInfo;
                if (model.ScheduleType == ScheduleTypes.Humidity)
                {
                    model.MinValue = this.MinValue = info.MinHumidity.HasValue ?
                        info.MinHumidity.Value : ScheduleModel.GetMinimumValue(model.ScheduleType);
                    model.MaxValue = this.MaxValue = info.MaxHumidity.HasValue ?
                        info.MaxHumidity.Value : ScheduleModel.GetMaximumValue(model.ScheduleType);
                }
                else
                {
                    model.MinValue = this.MinValue = info.MinTemperature.HasValue ?
                       info.MinTemperature.Value : ScheduleModel.GetMinimumValue(model.ScheduleType);
                    model.MaxValue = this.MaxValue = info.MaxTemperature.HasValue ?
                        info.MaxTemperature.Value : ScheduleModel.GetMaximumValue(model.ScheduleType);
                }
                ScheduleModel = model;
                isAnySaved = true;
            }
            if (isAnySaved)
            {
                await messageBoxService.Information(Resources.Saved);
                WeakReferenceMessenger.Default.Send(new DeviceListChangedMessage(ChangeType.Updated));
            }
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

    [RelayCommand]
    async Task DeleteDevice()
    {
        try
        {
            if (await messageBoxService.Prompt(Resources.Confirmation, Resources.ConfirmDeviceDelete))
            {
                await heatCeramService.DeleteDevice(UserDevice.Info.DeviceId);
                WeakReferenceMessenger.Default.Send(new DeviceListChangedMessage(ChangeType.Deleted));
                await navigationService.PopAsync();
            }
        }
        catch( Exception exception )
        {
            await messageBoxService.Alert(exception);
        }
    }

}
