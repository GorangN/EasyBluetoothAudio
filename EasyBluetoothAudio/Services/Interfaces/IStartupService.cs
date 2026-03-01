namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Defines the contract for managing the application's Windows startup entry.
/// </summary>
public interface IStartupService
{
    /// <summary>
    /// Gets a value indicating whether the application is registered to start with Windows.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Registers the application in the Windows startup registry key.
    /// </summary>
    void Enable();

    /// <summary>
    /// Removes the application from the Windows startup registry key.
    /// </summary>
    void Disable();
}
