namespace HeatController.Messages;

public class DeviceListChangedMessage
{
    public DeviceListChangedMessage(ChangeType changeType)
    {
        ChangeType = changeType;
    }

    public ChangeType ChangeType { get; }
}
