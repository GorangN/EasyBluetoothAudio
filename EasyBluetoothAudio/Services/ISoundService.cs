namespace EasyBluetoothAudio.Services;

/// <summary>
/// Defines the contract for managing audible connection feedback.
/// </summary>
public interface ISoundService
{
    /// <summary>
    /// Registers Messenger subscriptions and loads the initial sound preference.
    /// Must be called once during application startup.
    /// </summary>
    void Initialize();
}
