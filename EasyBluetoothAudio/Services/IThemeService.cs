namespace EasyBluetoothAudio.Services;

/// <summary>
/// Defines the contract for managing runtime theme switching.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Registers Messenger subscriptions and applies the persisted theme.
    /// Must be called once during application startup.
    /// </summary>
    void Initialize();
}
