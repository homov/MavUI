using HeatCeram.Api.Models;

namespace HeatController.Models;

public partial class DeviceType: ObservableObject
{
    [ObservableProperty] DeviceTypeInfo deviceTypeInfo;

    public DeviceType(DeviceTypeInfo deviceTypeInfo)
    {
        DeviceTypeInfo = deviceTypeInfo;
    }
}
