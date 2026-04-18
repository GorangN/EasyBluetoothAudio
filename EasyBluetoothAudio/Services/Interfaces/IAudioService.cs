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
    /// <c>true</c> if <c>System.Devices.Aep.IsConnected</c> reports <see langword="true"/>;
    /// otherwise <c>false</c>.
    /// </returns>
    Task<bool> IsBluetoothPhysicallyConnectedAsync(string deviceId);

    /// <summary>
    /// Checks whether audio is currently flowing through the default system render endpoint
    /// using the Core Audio peak meter. May return false negatives for Bluetooth audio
    /// (Windows does not always reflect A2DP audio in the peak meter), but a positive result
    /// reliably indicates an active audio stream. Intended as a keepalive suppression signal.
    /// </summary>
    /// <returns><c>true</c> if audio activity was detected; otherwise <c>false</c>.</returns>
    bool IsAudioCurrentlyPlaying();

    /// <summary>
    /// Disconnects the active Bluetooth audio connection and releases resources.
    /// </summary>
    void Disconnect();
}
