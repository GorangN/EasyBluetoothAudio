using System.Diagnostics;
using EasyBluetoothAudio.Services.Interfaces;
using Microsoft.Win32;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Manages the application's Windows startup entry via the HKCU Run registry key.
/// </summary>
public class StartupService : IStartupService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "EasyBluetoothAudio";

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
            return key?.GetValue(AppName) != null;
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

        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        key?.SetValue(AppName, $"\"{exePath}\" --silent");
    }

    /// <inheritdoc />
    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: true);
        key?.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
