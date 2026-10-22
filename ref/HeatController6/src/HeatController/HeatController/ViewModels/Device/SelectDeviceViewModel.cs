using HeatController.Models;
using System.Linq;

namespace HeatController;

public partial class SelectDeviceViewModel: BaseViewModel
{
	HeatCeramService heatCeramService = ContainerService.Container.Resolve<HeatCeramService>();

	[ObservableProperty]
	List<UserDevice> devices;

	[ObservableProperty]
	string groupId;

	public SelectDeviceViewModel(string groupId)
	{
		this.groupId = groupId;
		var task = Task.Run(async () => await Load());
		Task.WaitAll(task);
    }

	async Task Load()
	{
		var devices = await heatCeramService.GetDevices();
        this.devices = devices
			.Where(it => it.Info.GroupWork == true && it.Info.GroupId != groupId)
			.OrderBy(it => it.Name)
			.ToList();
	}
}
