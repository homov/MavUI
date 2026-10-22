using Autofac;
using CommunityToolkit.Mvvm.Messaging;
using HeatController.Messages;
using HeatController.Models;
using System.Linq;
using HeatCeram.Api.Models;

namespace HeatController;

public partial class LocationPageViewModel : BaseViewModel
{
	private readonly HeatCeramService heatCeramService = ContainerService.Container.Resolve<HeatCeramService>();

	public static string[] Buttons = new[]
	{
		Icons.ContentSaveOutline,
		Icons.Plus,
		Icons.ContentSaveOutline
	};

	UserLocation userLocation;
	public UserLocation UserLocation
    {
		get => userLocation;
		set
		{
			if (SetProperty(ref userLocation, value))
			{
                ScheduleTypeList = UserLocation.ValidScheduleTypes;
                SelectedScheduleType = ScheduleTypeList.FirstOrDefault(it => it.Id == UserLocation.Schedule.ScheduleType);
				SelectedSensor = SensorList.FirstOrDefault(it => it.Info.DeviceId == UserLocation.Info.ExternalSensorId);
				LocationName = UserLocation.Info.Name;
				ScheduleModel = HeatController.Models.ScheduleModel.FromSchedule(UserLocation.Schedule);
            }
        }
	}

    [ObservableProperty]
    List<ScheduleItemType> scheduleTypeList;

    [ObservableProperty]
	ScheduleItemType selectedScheduleType;

    [ObservableProperty]
    List<UserDevice> sensorList;

	[ObservableProperty]
	UserDevice selectedSensor;

	[ObservableProperty]
	ScheduleModel scheduleModel;

    [ObservableProperty]
	string locationName;

    public LocationPageViewModel(UserLocation userLocation)
	{
		var task = Task.Run(async () => await Load());
		Task.WaitAll(task);

		UserLocation = userLocation;
    }

	async Task Load()
	{
		var devices = await heatCeramService.GetDevices();
		SensorList = devices.Where(it => it.Info.IsSensor).OrderBy(it=>it.Name).ToList();
		SensorList.Insert(0, new UserDevice(new UserDeviceInfo() {DeviceName = Resources.WithoutSensor, DeviceId = null}));
    }

	[RelayCommand]
	async Task DeleteLocation()
	{
        try
        {
            if (await messageBoxService.Prompt(Resources.Confirmation, Resources.ConfirmGroupDelete))
            {
                using var busy = Busy();

                await heatCeramService.RemoveLocation(UserLocation.Info.GroupId);
                await navigationService.PopAsync();
            }
        }
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }

    }

    [RelayCommand]
	async Task SaveLocation()
	{
		try
		{
            using var busy = Busy();

            var scheduleChanged = false;
			if (LocationName!= UserLocation.Info.Name)
			{
				await heatCeramService.UpdateLocationName(UserLocation.Info.GroupId, LocationName);
				UserLocation.Info.Name = LocationName;
            }
			if (SelectedSensor != null)
			{
				var sensorId = SelectedSensor.Info.DeviceId;
				if (sensorId != UserLocation.Info.ExternalSensorId)
				{
					await heatCeramService.UpdateLocationSensor(UserLocation.Info.GroupId, sensorId);
					UserLocation.Info.ExternalSensorId = sensorId;
				}
			}

			if (SelectedScheduleType != null)
			{
				var schType = SelectedScheduleType.Id;
				if (UserLocation.Schedule.ScheduleType != schType)
				{
					scheduleChanged = true;
					await heatCeramService.UpdateScheduleType(UserLocation.Schedule.ScheduleId, schType);
					UserLocation.Schedule.ScheduleType = schType;
				}
			}

			if (scheduleChanged)
			{
				var schedule = await heatCeramService.LoadSchedule(UserLocation.Schedule.ScheduleId);
				UserLocation.Schedule = schedule;
                ScheduleModel = HeatController.Models.ScheduleModel.FromSchedule(UserLocation.Schedule);
            }
			
			await messageBoxService.Information(Resources.Saved);
            WeakReferenceMessenger.Default.Send(new LocationListChangedMessage(ChangeType.Updated));
        }
		catch( Exception exception )
		{
			await messageBoxService.Alert(exception);
		}
	}

	[RelayCommand]
	async Task SaveSchedule()
	{
		try
		{
			using var busy = Busy();
			await heatCeramService.UpdateSchedule(ScheduleModel);
		}
        catch (Exception exception)
        {
            await messageBoxService.Alert(exception);
        }
    }

	[RelayCommand]
	async Task AddDevice()
	{
		try
		{
			var model = new SelectDeviceViewModel(UserLocation.Info.GroupId);
			var page = new SelectDevicePopup(model);
			var value = await navigationService.ShowPopup(page) as UserDevice;
			if (value!=null)
			{
				await heatCeramService.SetDeviceGroup(value.Info.DeviceId, UserLocation.Info.GroupId);
                WeakReferenceMessenger.Default.Send(new LocationListChangedMessage(ChangeType.Updated));
                WeakReferenceMessenger.Default.Send(new DeviceListChangedMessage(ChangeType.Updated));
            }
			
		}
		catch(Exception exception)
		{
			await messageBoxService.Alert(exception);
		}
	}
	
	[RelayCommand]
	async Task Action(int index)
	{
		switch (index)
		{
			case 0:
				await SaveSchedule();
				break;
			case 1:
				await AddDevice();
				break;
			case 2:
				await SaveLocation();
				break;
		}
	}
}
