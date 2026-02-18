namespace EasyBluetoothAudio.Models;

/// <summary>
/// Persisted application settings loaded and saved by <see cref="EasyBluetoothAudio.Services.ISettingsService"/>.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the application launches automatically on Windows startup.
    /// </summary>
    public bool AutoStartOnStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether device volume is synchronised with the system volume.
    /// </summary>
    public bool SyncVolume { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the application automatically connects to the last
    /// used device when it starts.
    /// </summary>
    public bool AutoConnect { get; set; }

    /// <summary>
    /// Gets or sets the audio buffer delay preset.
    /// </summary>
    public AudioDelay Delay { get; set; } = AudioDelay.Medium;

    /// <summary>
    /// Gets or sets the device identifier of the last successfully selected Bluetooth device.
    /// </summary>
    public string? LastDeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device identifier of the preferred audio output device.
    /// When null or empty, the system default is used.
    /// </summary>
    public string? OutputDeviceId { get; set; }
}
