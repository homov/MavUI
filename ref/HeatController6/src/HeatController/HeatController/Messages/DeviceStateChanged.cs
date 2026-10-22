namespace HeatController.Messages;

public class DeviceStateChanged
{
    public DeviceStateChanged(string deviceId, bool newStata)
    {
        DeviceId = deviceId;
        NewState = newStata;
    }

    public string DeviceId { get; }
    public bool NewState { get; }
}
