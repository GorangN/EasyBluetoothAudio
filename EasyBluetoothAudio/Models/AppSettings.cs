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
    /// Gets or sets a value indicating whether the application automatically connects to the last
    /// used device when it starts.
    /// </summary>
    public bool AutoConnect { get; set; }

    /// <summary>
    /// Gets or sets the device identifier of the last successfully selected Bluetooth device.
    /// </summary>
    public string? LastDeviceId { get; set; }

    /// <summary>
    /// Gets or sets the active color theme for the application.
    /// </summary>
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Dark;

    /// <summary>
    /// Gets or sets the device identifier of the user's preferred Bluetooth audio device.
    /// </summary>
    public string? PreferredDeviceId { get; set; }

    /// <summary>
    /// Gets or sets the timeout strategy used when attempting to reconnect to a lost device.
    /// </summary>
    public ReconnectTimeout ReconnectTimeout { get; set; } = ReconnectTimeout.ThirtySeconds;

    /// <summary>
    /// Gets or sets a value indicating whether toast notifications are shown for connection events.
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether an audible sound plays when a connection is established.
    /// </summary>
    public bool PlayConnectionSound { get; set; }
}
