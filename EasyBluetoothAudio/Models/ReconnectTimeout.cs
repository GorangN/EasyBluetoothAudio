namespace EasyBluetoothAudio.Models;

/// <summary>
/// Defines the supported reconnection timeout intervals in seconds.
/// A value of zero represents an infinite (no timeout) reconnection strategy.
/// </summary>
public enum ReconnectTimeout
{
    /// <summary>
    /// Reconnect attempt every 30 seconds.
    /// </summary>
    ThirtySeconds = 30,

    /// <summary>
    /// Reconnect attempt every 60 seconds.
    /// </summary>
    SixtySeconds = 60,

    /// <summary>
    /// No automatic timeout; reconnect indefinitely.
    /// </summary>
    Infinite = 0
}
