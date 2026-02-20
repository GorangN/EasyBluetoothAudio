using EasyBluetoothAudio.Core;

namespace EasyBluetoothAudio.Models;

/// <summary>
/// Represents a discovered Bluetooth device with its connection metadata.
/// </summary>
public class BluetoothDevice : ViewModelBase
{
    private string _name = string.Empty;
    private string _id = string.Empty;
    private bool _isConnected;
    private bool _isPhoneOrComputer;

    /// <summary>
    /// Gets or sets the friendly display name of the device.
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// Gets or sets the unique identifier of the device.
    /// </summary>
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the device is currently connected via Bluetooth.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the device is an audio source (phone or computer).
    /// </summary>
    public bool IsPhoneOrComputer
    {
        get => _isPhoneOrComputer;
        set => SetProperty(ref _isPhoneOrComputer, value);
    }

    /// <summary>
    /// Returns the friendly name of the device.
    /// </summary>
    public override string ToString()
    {
        return Name;
    }
}
