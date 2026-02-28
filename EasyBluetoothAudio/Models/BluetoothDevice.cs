using CommunityToolkit.Mvvm.ComponentModel;

namespace EasyBluetoothAudio.Models;

/// <summary>
/// Represents a discovered Bluetooth device with its connection metadata.
/// </summary>
public partial class BluetoothDevice : ObservableObject
{
    /// <summary>
    /// Gets or sets the friendly display name of the device.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the device.
    /// </summary>
    [ObservableProperty]
    private string _id = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the device is currently connected via Bluetooth.
    /// </summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>
    /// Gets or sets a value indicating whether the device is an audio source (phone or computer).
    /// </summary>
    [ObservableProperty]
    private bool _isPhoneOrComputer;

    /// <summary>
    /// Returns the friendly name of the device.
    /// </summary>
    /// <returns>The device name as a string.</returns>
    public override string ToString()
    {
        return Name;
    }
}
