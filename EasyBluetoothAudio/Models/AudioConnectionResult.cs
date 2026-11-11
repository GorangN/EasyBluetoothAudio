namespace EasyBluetoothAudio.Models;

/// <summary>
/// Describes the outcome of opening one remote Bluetooth audio route without exposing Windows API types.
/// </summary>
public enum AudioConnectionResult
{
    /// <summary>The remote audio route opened successfully.</summary>
    Connected,

    /// <summary>Windows does not currently expose a supported audio route for the device.</summary>
    Unavailable,

    /// <summary>Windows did not complete the connection request within its timeout.</summary>
    TimedOut,

    /// <summary>Windows denied the audio connection request.</summary>
    DeniedBySystem,

    /// <summary>The connection failed or was superseded by another operation for the same device.</summary>
    Failed
}
