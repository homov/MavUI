using Avalonia.Controls;
using Hc.Models.Cloud;
using Mav;
using Mav.Controls;

namespace Hc.Pages.Groups;

/// <summary>
/// The properties tab of a group: name, sensor, and deleting the group. The name is required, checked with
/// <see cref="MavFormValidator"/>; no other client rule is invented, the server decides.
/// </summary>
public partial class GroupPropertiesTab : UserControl
{
    private readonly MavFormValidator _validator = new();

    /// <summary>Sensor id of each select option; null is "without sensor". Same order as the options.</summary>
    private readonly List<string?> _sensorIds = [];

    public GroupPropertiesTab()
    {
        InitializeComponent();
        _validator.Field(NameField).Required(MavLocalization.Get("validation.required", "This field is required"));
    }

    public MavButton SaveButton => Save;

    public MavButton DeleteButton => Delete;

    /// <summary>The name typed by the user, trimmed.</summary>
    public string GroupName => NameField.Text?.Trim() ?? string.Empty;

    /// <summary>The selected sensor id, or null for "without sensor".</summary>
    public string? SensorId =>
        SensorSelect.SelectedIndex >= 0 && SensorSelect.SelectedIndex < _sensorIds.Count
            ? _sensorIds[SensorSelect.SelectedIndex]
            : null;

    /// <summary>Checks the fields and shows the errors under them.</summary>
    public bool Validate() => _validator.Validate();

    /// <summary>Shows the group's name and sensors. The sensors are the devices that are sensors.</summary>
    public void Show(UserGroupInfo group, IReadOnlyList<UserDeviceInfo> sensors)
    {
        NameField.Text = group.Name;
        _sensorIds.Clear();
        SensorSelect.Options.Clear();

        var current = string.IsNullOrEmpty(group.ExternalSensorId) ? null : group.ExternalSensorId;
        AddOption(null, MavLocalization.Get("group.sensor.none", "Without sensor"));
        foreach (var sensor in sensors.OrderBy(s => s.DeviceName))
            AddOption(sensor.DeviceId, sensor.DeviceName);

        // A sensor that is not in the list stays selected, so saving does not drop it silently.
        if (current is not null && !_sensorIds.Contains(current))
            AddOption(current, current);

        SensorSelect.SelectedIndex = Math.Max(0, _sensorIds.IndexOf(current));
    }

    private void AddOption(string? sensorId, string text)
    {
        _sensorIds.Add(sensorId);
        SensorSelect.AddOption(text);
    }
}
