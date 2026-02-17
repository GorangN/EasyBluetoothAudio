using System.Collections.Generic;
using System.Threading.Tasks;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Defines the contract for audio service operations, including device discovery and connection management.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Retrieves a list of paired Bluetooth devices, including their connection status.
    /// </summary>
    /// <returns>A collection of <see cref="BluetoothDevice"/> information.</returns>
    Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync();

    /// <summary>
    /// Connects to a specific Bluetooth device as an A2DP sink.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the Bluetooth device.</param>
    /// <returns>True if the connection was successful; otherwise, false.</returns>
    Task<bool> ConnectBluetoothAudioAsync(string deviceId);

    /// <summary>
    /// Initiates audio routing. In the current implementation, this is a placeholder as the connection handles routing.
    /// </summary>
    /// <param name="captureDeviceFriendlyName">The friendly name of the capture device (unused).</param>
    /// <param name="bufferMs">The buffer size in milliseconds (unused).</param>
    /// <returns>A task representing the operation.</returns>
    Task StartRoutingAsync(string captureDeviceFriendlyName, int bufferMs);

    /// <summary>
    /// Stops the current audio routing session and disconnects the device.
    /// </summary>
    void StopRouting();

    /// <summary>
    /// Gets a value indicating whether audio routing is currently active.
    /// </summary>
    bool IsRouting { get; }
}
