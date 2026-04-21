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
    /// Determines whether the specified Bluetooth device is physically paired and connected
    /// in Windows, independent of any active audio connection.
    /// Safe to call before connecting audio.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device to check.</param>
    /// <returns>
    /// <c>true</c> if the device appears in the current
    /// <see cref="Windows.Media.Audio.AudioPlaybackConnection"/> selector enumeration; otherwise
    /// <c>false</c>. This avoids the unreliable <c>System.Devices.Aep.IsConnected</c> property on
    /// WinRT <c>\SNK</c> endpoints.
    /// </returns>
    Task<bool> IsBluetoothPhysicallyConnectedAsync(string deviceId);

    /// <summary>
    /// Disconnects the active Bluetooth audio connection and releases resources.
    /// </summary>
    /// <param name="reason">
    /// Short tag identifying the caller ("user", "monitor-detected-loss", "dispose", "manual-recover").
    /// Logged alongside the teardown to make the debug trace diagnosable when the reconnect flow runs.
    /// </param>
    /// <param name="preserveDisconnectTimestamp">
    /// <see langword="true"/> when the caller is tearing down the audio endpoint for an internal
    /// recycle (e.g. manual reconnect while the phone stays Bluetooth-connected) and the
    /// last-disconnect bookkeeping that drives the settle delay should not be reset to "now";
    /// otherwise <see langword="false"/> (the default), which treats the teardown as a real disconnect.
    /// </param>
    void Disconnect(string reason = "unspecified", bool preserveDisconnectTimestamp = false);
}
