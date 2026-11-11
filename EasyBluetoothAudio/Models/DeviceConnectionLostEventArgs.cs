namespace EasyBluetoothAudio.Models;

/// <summary>
/// Identifies the remote device whose previously opened audio route changed to a closed state.
/// </summary>
/// <param name="deviceId">The identifier of the device that owns the lost audio route.</param>
public sealed class DeviceConnectionLostEventArgs(string deviceId) : EventArgs
{
    /// <summary>Gets the identifier of the device whose audio route was lost.</summary>
    public string DeviceId { get; } = deviceId;
}
