using System.Collections.Generic;
using System.Threading.Tasks;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Defines the contract for audio service operations including device discovery, connection, and routing.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Gets a value indicating whether audio routing is currently active.
    /// </summary>
    bool IsRouting { get; }

    /// <summary>
    /// Discovers Bluetooth devices that support the A2DP Source role.
    /// </summary>
    /// <returns>A collection of discovered <see cref="BluetoothDevice"/> instances.</returns>
    Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync();

    /// <summary>
    /// Enumerates available audio output devices.
    /// </summary>
    /// <returns>A collection of available <see cref="AudioDevice"/> instances.</returns>
    IEnumerable<AudioDevice> GetOutputDevices();

    /// <summary>
    /// Establishes an A2DP Sink connection to the specified Bluetooth device.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the target device.</param>
    /// <returns><c>true</c> if the connection succeeded; otherwise <c>false</c>.</returns>
    Task<bool> ConnectBluetoothAudioAsync(string deviceId);

    /// <summary>
    /// Begins audio routing from the specified capture device to the specified output device.
    /// </summary>
    /// <param name="captureDeviceFriendlyName">The friendly name of the capture device.</param>
    /// <param name="outputDeviceId">The ID of the output device to route audio to (or null for default).</param>
    /// <param name="bufferMs">The buffer size in milliseconds.</param>
    Task StartRoutingAsync(string captureDeviceFriendlyName, string? outputDeviceId, int bufferMs);

    /// <summary>
    /// Stops the current audio routing session and releases the connection.
    /// </summary>
    void StopRouting();

    /// <summary>
    /// Enables or disables synchronisation of the Bluetooth capture device volume
    /// with the application's output session volume.
    /// </summary>
    /// <param name="enabled"><c>true</c> to mirror capture volume changes to the output session; <c>false</c> to stop.</param>
    void SetSyncVolume(bool enabled);

    /// <summary>
    /// Changes the audio output device while routing is active, restarting the audio pipeline.
    /// If routing is not active, this is a no-op.
    /// </summary>
    /// <param name="outputDeviceId">The ID of the new output device (or null for default).</param>
    Task ChangeOutputDeviceAsync(string? outputDeviceId);
}
