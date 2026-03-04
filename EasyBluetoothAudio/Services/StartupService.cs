using System.Diagnostics;
using EasyBluetoothAudio.Services.Interfaces;
using Microsoft.Win32;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Manages the application's Windows startup entry via the HKCU Run registry key.
/// </summary>
public class StartupService : IStartupService
{
    private readonly string _registryKeyPath;
    private readonly string _appName;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupService"/> class.
    /// </summary>
    public StartupService() : this(@"Software\Microsoft\Windows\CurrentVersion\Run", "EasyBluetoothAudio")
    {
    }

    internal StartupService(string registryKeyPath, string appName)
    {
        _registryKeyPath = registryKeyPath;
        _appName = appName;
    }

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(_registryKeyPath, writable: false);
            return key?.GetValue(_appName) != null;
        }
    }

    /// <inheritdoc />
    public void Enable()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath == null)
        {
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(_registryKeyPath, writable: true);
        key?.SetValue(_appName, $"\"{exePath}\" --silent");
    }

    /// <inheritdoc />
    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_registryKeyPath, writable: true);
        key?.DeleteValue(_appName, throwOnMissingValue: false);
    }
}
