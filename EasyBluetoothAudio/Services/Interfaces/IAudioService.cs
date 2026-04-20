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
    /// Disconnects the active Bluetooth audio connection and releases resources.
    /// </summary>
    /// <param name="reason">
    /// Short tag identifying the caller ("user", "monitor-detected-loss", "dispose", "reconnect-request").
    /// Logged alongside the teardown to make the debug trace diagnosable when the reconnect loop fires.
    /// </param>
    void Disconnect(string reason = "unspecified");

    /// <summary>
    /// Returns the current peak audio level on the capture endpoint associated with the active
    /// Bluetooth connection. Used to detect the Windows A2DP zombie state where the connection
    /// reports <c>Opened</c> but no samples actually flow through Windows to the render stack.
    /// </summary>
    /// <returns>
    /// The master peak value in the range <c>[0.0, 1.0]</c> when a matching capture endpoint is
    /// found, or <see langword="null"/> when no active device is known, the endpoint cannot be
    /// matched by name, or the CoreAudio enumeration fails. Callers must treat <see langword="null"/>
    /// as "no judgment possible" and therefore not derive a zombie verdict from it.
    /// </returns>
    float? GetActiveDevicePeakLevel();
}
