using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Tests for <see cref="BluetoothDevice"/> property notifications and behavior.
/// </summary>
public class BluetoothDeviceTests
{
    [Fact]
    public void Setting_Name_RaisesPropertyChanged()
    {
        var device = new BluetoothDevice();
        string? raisedProperty = null;
        device.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        device.Name = "iPhone";

        Assert.Equal("Name", raisedProperty);
    }

    [Fact]
    public void Setting_Id_RaisesPropertyChanged()
    {
        var device = new BluetoothDevice();
        string? raisedProperty = null;
        device.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        device.Id = "device-123";

        Assert.Equal("Id", raisedProperty);
    }

    [Fact]
    public void Setting_IsConnected_RaisesPropertyChanged()
    {
        var device = new BluetoothDevice();
        string? raisedProperty = null;
        device.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        device.IsConnected = true;

        Assert.Equal("IsConnected", raisedProperty);
    }

    [Fact]
    public void Setting_IsPhoneOrComputer_RaisesPropertyChanged()
    {
        var device = new BluetoothDevice();
        string? raisedProperty = null;
        device.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        device.IsPhoneOrComputer = true;

        Assert.Equal("IsPhoneOrComputer", raisedProperty);
    }

    [Fact]
    public void Setting_SameValue_DoesNotRaisePropertyChanged()
    {
        var device = new BluetoothDevice { Name = "iPhone" };
        bool raised = false;
        device.PropertyChanged += (s, e) => raised = true;

        device.Name = "iPhone";

        Assert.False(raised);
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var device = new BluetoothDevice { Name = "My Device" };

        Assert.Equal("My Device", device.ToString());
    }

    [Fact]
    public void DefaultValues_AreEmpty()
    {
        var device = new BluetoothDevice();

        Assert.Equal(string.Empty, device.Name);
        Assert.Equal(string.Empty, device.Id);
        Assert.False(device.IsConnected);
        Assert.False(device.IsPhoneOrComputer);
    }
}
