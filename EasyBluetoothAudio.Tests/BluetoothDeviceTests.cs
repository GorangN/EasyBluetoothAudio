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

    [Theory]
    [InlineData(nameof(BluetoothDevice.Name), "iPhone")]
    [InlineData(nameof(BluetoothDevice.Id), "device-123")]
    [InlineData(nameof(BluetoothDevice.IsConnected), true)]
    [InlineData(nameof(BluetoothDevice.IsPhoneOrComputer), true)]
    public void Setting_SameValue_DoesNotRaisePropertyChanged(string propertyName, object value)
    {
        var device = new BluetoothDevice();
        var property = typeof(BluetoothDevice).GetProperty(propertyName)!;

        // Set initial value
        property.SetValue(device, value);

        bool raised = false;
        device.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == propertyName)
            {
                raised = true;
            }
        };

        // Set same value again
        property.SetValue(device, value);

        Assert.False(raised, $"Property {propertyName} should not raise PropertyChanged when set to the same value.");
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
