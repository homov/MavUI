namespace HeatController.Messages;

public class LocationListChangedMessage
{
    public LocationListChangedMessage(ChangeType changeType)
    {
        ChangeType = changeType;
    }

    public ChangeType ChangeType { get; }
}
