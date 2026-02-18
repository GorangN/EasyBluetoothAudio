namespace EasyBluetoothAudio.Models;

/// <summary>
/// Represents an audio output device.
/// </summary>
public class AudioDevice
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
    /// Returns the friendly name of the device.
    /// </summary>
    public override string ToString() => Name;
}
