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
    public bool AutoConnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the device identifier of the last successfully selected Bluetooth device.
    /// </summary>
    public string? LastDeviceId { get; set; }

    /// <summary>
    /// Gets or sets the active color theme for the application.
    /// </summary>
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Dark;

    /// <summary>
    /// Gets or sets a value indicating whether toast notifications are shown for connection events.
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether an audible sound plays when a connection is established.
    /// </summary>
    public bool PlayConnectionSound { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether low-end hardware mode is enabled.
    /// When active, the Windows Bluetooth SBC bitpool is reduced to lower A2DP stream bandwidth,
    /// improving stability on congested radios at the cost of audio quality.
    /// </summary>
    public bool LowEndHardwareMode { get; set; }
}
