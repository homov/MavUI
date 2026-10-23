using Hc.Models.Cloud;
using Hc.Pages.Devices;
using Xunit;

namespace Hc.Tests;

public class DevicePageRulesTests
{
    private static UserDeviceInfo Device(bool isSensor = false, bool groupWork = false, string? groupId = null,
        params string[] controlTypes) => new()
    {
        IsSensor = isSensor,
        GroupWork = groupWork,
        GroupId = groupId,
        ControlTypes = controlTypes,
    };

    [Fact]
    public void CanControl_PlainDevice_Yes()
    {
        Assert.True(DevicePage.CanControl(Device()));
    }

    [Fact]
    public void CanControl_Sensor_No()
    {
        Assert.False(DevicePage.CanControl(Device(isSensor: true)));
    }

    [Fact]
    public void CanControl_InGroup_No()
    {
        Assert.False(DevicePage.CanControl(Device(groupWork: true, groupId: "g-1")));
    }

    [Fact]
    public void CanControl_GroupWorkWithoutGroup_Yes()
    {
        Assert.True(DevicePage.CanControl(Device(groupWork: true, groupId: null)));
        Assert.True(DevicePage.CanControl(Device(groupWork: true, groupId: "")));
    }

    [Fact]
    public void CanControl_GroupIdWithoutGroupWork_Yes()
    {
        // As in the old app: the group only counts when the group work flag is on.
        Assert.True(DevicePage.CanControl(Device(groupWork: false, groupId: "g-1")));
    }

    [Fact]
    public void AllowedTypes_ManualAlwaysFirst_OthersOnlyWhenSupported()
    {
        Assert.Equal(new[] { "manual", "temperature", "humidity" },
            DevicePage.AllowedTypes(Device(controlTypes: new[] { "temperature", "humidity" })));
    }

    [Fact]
    public void AllowedTypes_NoControlTypes_ManualOnly()
    {
        Assert.Equal(new[] { "manual" }, DevicePage.AllowedTypes(Device()));
    }
}
