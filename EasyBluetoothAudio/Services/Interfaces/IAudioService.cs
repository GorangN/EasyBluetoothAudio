using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Discovers remote Bluetooth audio sources and manages one A2DP Sink route into Windows at a time.
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// Raised when a previously opened route is lost. The event identifies its device so other routes remain unaffected.
    /// </summary>
    event EventHandler<DeviceConnectionLostEventArgs>? ConnectionLost;

    /// <summary>Discovers remote devices currently exposed by the Windows audio-playback selector.</summary>
    /// <returns>A snapshot of available audio sources, separate from the app's opened audio routes.</returns>
    /// <exception cref="Exception">Windows device enumeration failed; callers should preserve the previous snapshot.</exception>
    Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync();

    /// <summary>Releases the previous device's route and pending attempt before opening the requested device's route.</summary>
    /// <param name="deviceId">The identifier of the remote audio source.</param>
    /// <param name="cancellationToken">Cancels this attempt and releases only the route owned by this attempt.</param>
    /// <returns>The outcome of opening the requested route.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was cancelled.</exception>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    Task<AudioConnectionResult> ConnectBluetoothAudioAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>Checks whether this app owns an opened audio route for the specified device.</summary>
    /// <param name="deviceId">The identifier of the remote audio source to check.</param>
    /// <returns><see langword="true"/> when this device's current route is open; otherwise <see langword="false"/>.</returns>
    Task<bool> IsBluetoothDeviceConnectedAsync(string deviceId);

    /// <summary>
    /// Checks whether another device owns a registered native connection, including connections
    /// still enabling or opening, so hidden route recycling can preserve peer lifetimes.
    /// </summary>
    /// <param name="deviceId">The source to exclude from the snapshot, using case-insensitive identity.</param>
    /// <returns><see langword="true"/> when another current native connection is owned, regardless of its open state.</returns>
    bool HasOtherAudioConnections(string deviceId);

    /// <summary>
    /// Checks whether a remote source is available through the Windows audio-playback selector,
    /// independently of this app's opened routes. Does not rely on the unreliable AEP connectivity property.
    /// </summary>
    /// <param name="deviceId">The identifier of the remote audio source to check.</param>
    /// <returns><see langword="true"/> when the route is open or the selector contains the device.</returns>
    Task<bool> IsBluetoothPhysicallyConnectedAsync(string deviceId);

    /// <summary>Releases one device's route and invalidates that device's pending native open operation.</summary>
    /// <param name="deviceId">The identifier of the device whose route should be released.</param>
    /// <param name="reason">A diagnostic tag identifying the disconnect's caller.</param>
    /// <param name="preserveDisconnectTimestamp">Preserves real-disconnect timing during an internal route recycle.</param>
    void Disconnect(string deviceId, string reason = "unspecified", bool preserveDisconnectTimestamp = false);

    /// <summary>Releases every owned route and invalidates every pending native open operation.</summary>
    /// <param name="reason">A diagnostic tag identifying the aggregate disconnect's caller.</param>
    void DisconnectAll(string reason = "unspecified");
}
