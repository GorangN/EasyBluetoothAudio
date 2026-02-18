using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Defines the contract for loading and persisting application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads the current application settings. Returns defaults if no settings file exists.
    /// </summary>
    AppSettings Load();

    /// <summary>
    /// Persists the provided settings to storage.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    void Save(AppSettings settings);
}
