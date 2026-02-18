namespace EasyBluetoothAudio.Models;

/// <summary>
/// Defines the audio buffer delay presets available in the application.
/// The integer value represents the buffer size in milliseconds.
/// </summary>
public enum AudioDelay
{
    /// <summary>Low latency — 25 ms buffer.</summary>
    Low = 25,

    /// <summary>Balanced latency — 45 ms buffer. Recommended for most devices.</summary>
    Medium = 45,

    /// <summary>High stability — 70 ms buffer.</summary>
    High = 70
}
