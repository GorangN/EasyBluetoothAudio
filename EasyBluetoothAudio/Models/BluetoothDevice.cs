namespace EasyBluetoothAudio.Models;

/// <summary>
/// Represents a discovered Bluetooth device with its connection status.
/// </summary>
public class BluetoothDevice
{
    /// <summary>
    /// Gets or sets the friendly name of the device.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the device.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the device is currently connected via Bluetooth.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the device is a phone or a computer (audio source).
    /// </summary>
    public bool IsPhoneOrComputer { get; set; }

    /// <summary>
    /// Returns the name of the device.
    /// </summary>
    public override string ToString() => Name;
}
