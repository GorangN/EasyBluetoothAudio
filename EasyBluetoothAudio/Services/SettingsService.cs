using System;
using System.IO;
using System.Text.Json;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Persists application settings as JSON in the user's AppData folder.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class using the default AppData path.
    /// </summary>
    public SettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasyBluetoothAudio"))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class with a custom directory.
    /// Intended for use in unit tests.
    /// </summary>
    /// <param name="directory">The directory in which to store the settings file.</param>
    public SettingsService(string directory)
    {
        _filePath = Path.Combine(directory, "settings.json");
    }

    /// <inheritdoc />
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <inheritdoc />
    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath)!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
