using System.Collections.Generic;
using System.Threading.Tasks;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Defines the contract for Bluetooth audio operations including device discovery and connection management.
/// </summary>
public interface IAudioService
{
    /// <summary>
    /// Raised when the active audio connection is lost, for example when the device disconnects on the OS level.
    /// Subscribers can use this event to update UI state immediately without waiting for the next poll cycle.
    /// </summary>
    event EventHandler? ConnectionLost;
    /// <summary>
    /// Discovers Bluetooth devices that support the A2DP Source role.
    /// </summary>
    /// <returns>A collection of discovered <see cref="BluetoothDevice"/> instances.</returns>
    Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync();

    /// <summary>
    /// Establishes an A2DP Sink connection to the specified Bluetooth device.
    /// Audio routing begins automatically via Windows once connected.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the target device.</param>
    /// <returns><c>true</c> if the connection succeeded; otherwise <c>false</c>.</returns>
    Task<bool> ConnectBluetoothAudioAsync(string deviceId);

    /// <summary>
    /// Determines whether the specified Bluetooth device is currently connected.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device to check.</param>
    /// <returns><c>true</c> if the device is connected; otherwise <c>false</c>.</returns>
    Task<bool> IsBluetoothDeviceConnectedAsync(string deviceId);

    /// <summary>
    /// Probes the active audio connection. If audio is currently flowing the probe is skipped.
    /// Otherwise, if the device is physically connected, a full teardown and re-creation of the
    /// <see cref="Windows.Media.Audio.AudioPlaybackConnection"/> is performed to force end-to-end
    /// re-negotiation with the remote device (a simple <c>OpenAsync</c> on the existing object
    /// returns <c>Success</c> based on stale local state and does not actually refresh the stream).
    /// </summary>
    /// <param name="deviceId">The device identifier whose connection should be probed.</param>
    /// <returns>
    /// <see langword="true"/> if audio was already flowing and the probe was skipped;
    /// <see langword="null"/> if a full reconnect was performed and succeeded;
    /// <see langword="false"/> if the stream is stale, the device is not physically connected,
    /// or the reconnect failed — indicating the reconnect loop should take over.
    /// </returns>
    Task<bool?> ProbeConnectionAsync(string deviceId);

    /// <summary>
    /// Determines whether the specified Bluetooth device is physically paired and connected
    /// in Windows, independent of any active audio connection.
    /// Safe to call before connecting audio.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device to check.</param>
    /// <returns>
    /// <c>true</c> if <c>System.Devices.Aep.IsConnected</c> reports <see langword="true"/>;
    /// otherwise <c>false</c>.
    /// </returns>
    Task<bool> IsBluetoothPhysicallyConnectedAsync(string deviceId);

    /// <summary>
    /// Disconnects the active Bluetooth audio connection and releases resources.
    /// </summary>
    void Disconnect();
}
