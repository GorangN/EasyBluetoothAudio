using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Implements audio services using the Windows AudioPlaybackConnection API for Bluetooth A2DP Sink connectivity.
/// Windows handles all audio routing natively once the connection is established.
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    /// <summary>
    /// Milliseconds to wait between <c>Start()</c> and <c>OpenAsync()</c> to allow Windows
    /// to complete teardown of the previous <see cref="AudioPlaybackConnection"/> before the
    /// new audio endpoint negotiates A2DP with the remote device.
    /// Applied on the first connect or after a real disconnect. Internal route recycles keep
    /// their separate timing semantics because the physical Bluetooth link remains available.
    /// </summary>
    internal const int SettleDelayMs = 5_000;

    private readonly IDispatcherService _dispatcherService;
    private readonly object _connectionSync = new();
    private AudioPlaybackConnection? _audioConnection;
    private string? _activeDeviceId;
    private bool _hasConnectedBefore;
    private DateTime _lastDisconnectTime = DateTime.UtcNow;
    private int _connectionGeneration;

    /// <inheritdoc />
    public event EventHandler? ConnectionLost;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioService"/> class.
    /// </summary>
    /// <param name="dispatcherService">The dispatcher service for UI thread operations.</param>
    public AudioService(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
    }

    /// <summary>
    /// Enumerates the remote devices currently exposed through the
    /// <see cref="AudioPlaybackConnection"/> selector.
    /// Presence in this selector is the app's authoritative signal that Windows still sees the
    /// remote source as an available Bluetooth audio-playback device, so callers must not
    /// reinterpret these results through the unreliable AEP connectivity property.
    /// </summary>
    /// <returns>A snapshot of connected audio-playback device identifiers and display names.</returns>
    internal virtual async Task<IReadOnlyList<(string Id, string Name)>> GetConnectedAudioPlaybackDevicesAsync()
    {
        var selector = AudioPlaybackConnection.GetDeviceSelector();
        var devices = await DeviceInformation.FindAllAsync(selector);
        return devices.Select(device => (device.Id, device.Name)).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync()
    {
        var result = new List<BluetoothDevice>();
        try
        {
            var devices = await GetConnectedAudioPlaybackDevicesAsync();

            foreach (var device in devices)
            {
                result.Add(new BluetoothDevice
                {
                    Name = device.Name,
                    Id = device.Id,
                    IsConnected = true,
                    IsPhoneOrComputer = true
                });

                Debug.WriteLine($"[DeviceDiscover] Found Source: {device.Name} (ID: {device.Id}, Connected: true via selector)");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GetBluetoothDevices] Error: {ex.Message}");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBluetoothAudioAsync(string deviceId)
    {
        AudioPlaybackConnection? connection = null;
        var connectionGeneration = 0;

        try
        {
            var replacementState = BeginConnectionReplacement("pre-connect");
            connectionGeneration = replacementState.ConnectionGeneration;

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");

            var timeSinceDisconnect = DateTime.UtcNow - replacementState.LastDisconnectTime;
            var needsSettle = !replacementState.HasConnectedBefore
                || timeSinceDisconnect.TotalMilliseconds < SettleDelayMs;
            var settleDelay = needsSettle
                ? CalculateRemainingSettleDelay(replacementState.LastDisconnectTime, DateTime.UtcNow)
                : 0;

            AudioPlaybackConnectionOpenResult? openResult = null;
            await _dispatcherService.InvokeAsync(async () =>
            {
                connection = AudioPlaybackConnection.TryCreateFromId(deviceId);
                if (connection == null)
                {
                    return;
                }

                connection.StateChanged += OnAudioConnectionStateChanged;
                if (!TryRegisterConnection(connection, connectionGeneration))
                {
                    connection.StateChanged -= OnAudioConnectionStateChanged;
                    connection.Dispose();
                    connection = null;
                    return;
                }

                connection.Start();

                if (settleDelay > 0)
                {
                    Debug.WriteLine($"[ConnectBT] Settling {settleDelay}ms between Start() and OpenAsync()...");
                    await Task.Delay(settleDelay);
                }

                if (!IsCurrentConnection(connection, connectionGeneration))
                {
                    return;
                }

                openResult = await connection.OpenAsync();
            });

            if (connection == null || !IsCurrentConnection(connection, connectionGeneration))
            {
                Debug.WriteLine("[ConnectBT] Connection attempt was superseded before it could complete.");
                return false;
            }

            if (openResult?.Status == AudioPlaybackConnectionOpenResultStatus.Success)
            {
                Debug.WriteLine("[ConnectBT] AudioPlaybackConnection Success!");
                return TryActivateConnection(connection, connectionGeneration, deviceId);
            }

            Debug.WriteLine($"[ConnectBT] Failed status: {openResult?.Status}");
            TearDownConnectionIfCurrent(
                $"open-failed-{openResult?.Status}",
                connection,
                connectionGeneration);
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConnectBT] Error: {ex.Message}");
            if (connection != null)
            {
                TearDownConnectionIfCurrent("connect-exception", connection, connectionGeneration);
            }

            return false;
        }
    }

    /// <summary>
    /// Unhooks the <see cref="AudioPlaybackConnection.StateChanged"/> handler, disposes the current
    /// connection and resets tracking fields without allowing an older in-flight operation to
    /// mutate the connection that belongs to a newer generation.
    /// </summary>
    /// <param name="reason">Short tag describing why the teardown is happening (logged when a connection was actually open).</param>
    /// <returns>The generation assigned to the replacement and a snapshot of the settle state.</returns>
    private (int ConnectionGeneration, DateTime LastDisconnectTime, bool HasConnectedBefore)
        BeginConnectionReplacement(string reason)
    {
        AudioPlaybackConnection? connection;
        (int ConnectionGeneration, DateTime LastDisconnectTime, bool HasConnectedBefore) replacementState;
        lock (_connectionSync)
        {
            _connectionGeneration++;
            connection = DetachAudioConnectionCore(updateDisconnectTimestamp: false);
            replacementState = (_connectionGeneration, _lastDisconnectTime, _hasConnectedBefore);
        }

        DisposeAudioConnection(connection, reason);
        return replacementState;
    }

    private void TearDownAudioConnection(string reason, bool preserveDisconnectTimestamp)
    {
        AudioPlaybackConnection? connection;
        lock (_connectionSync)
        {
            _connectionGeneration++;
            connection = DetachAudioConnectionCore(
                updateDisconnectTimestamp: !preserveDisconnectTimestamp);
        }

        DisposeAudioConnection(connection, reason);
    }

    private void TearDownConnectionIfCurrent(
        string reason,
        AudioPlaybackConnection connection,
        int connectionGeneration)
    {
        AudioPlaybackConnection? detachedConnection;
        lock (_connectionSync)
        {
            if (_connectionGeneration != connectionGeneration
                || !ReferenceEquals(_audioConnection, connection))
            {
                return;
            }

            _connectionGeneration++;
            detachedConnection = DetachAudioConnectionCore(updateDisconnectTimestamp: true);
        }

        DisposeAudioConnection(detachedConnection, reason);
    }

    private AudioPlaybackConnection? DetachAudioConnectionCore(bool updateDisconnectTimestamp)
    {
        var connection = _audioConnection;
        _audioConnection = null;
        _activeDeviceId = null;
        if (connection == null)
        {
            return null;
        }

        if (updateDisconnectTimestamp)
        {
            _lastDisconnectTime = DateTime.UtcNow;
        }

        return connection;
    }

    private void DisposeAudioConnection(AudioPlaybackConnection? connection, string reason)
    {
        if (connection == null)
        {
            return;
        }

        Debug.WriteLine($"[AudioService] Tearing down connection (reason={reason}).");
        connection.StateChanged -= OnAudioConnectionStateChanged;
        connection.Dispose();
    }

    private bool TryRegisterConnection(
        AudioPlaybackConnection connection,
        int connectionGeneration)
    {
        lock (_connectionSync)
        {
            if (_connectionGeneration != connectionGeneration)
            {
                return false;
            }

            _audioConnection = connection;
            return true;
        }
    }

    private bool TryActivateConnection(
        AudioPlaybackConnection connection,
        int connectionGeneration,
        string deviceId)
    {
        lock (_connectionSync)
        {
            if (_connectionGeneration != connectionGeneration
                || !ReferenceEquals(_audioConnection, connection))
            {
                return false;
            }

            _activeDeviceId = deviceId;
            _hasConnectedBefore = true;
            _lastDisconnectTime = DateTime.MinValue;
            return true;
        }
    }

    private bool IsCurrentConnection(
        AudioPlaybackConnection connection,
        int connectionGeneration)
    {
        lock (_connectionSync)
        {
            return _connectionGeneration == connectionGeneration
                && ReferenceEquals(_audioConnection, connection);
        }
    }

    /// <summary>
    /// Calculates how long a replacement connection must wait before opening after a real disconnect.
    /// </summary>
    /// <param name="lastDisconnectTime">The time at which the previous real disconnect was recorded.</param>
    /// <param name="currentTime">The current time used for the calculation.</param>
    /// <returns>The remaining settle delay in milliseconds, clamped to the configured window.</returns>
    internal static int CalculateRemainingSettleDelay(DateTime lastDisconnectTime, DateTime currentTime)
    {
        var elapsedMilliseconds = Math.Max(0, (currentTime - lastDisconnectTime).TotalMilliseconds);
        return Math.Clamp(SettleDelayMs - (int)elapsedMilliseconds, 0, SettleDelayMs);
    }

    private void OnAudioConnectionStateChanged(AudioPlaybackConnection sender, object args)
    {
        try
        {
            var state = sender.State;
            Debug.WriteLine($"[StateChanged] State={state}");
            lock (_connectionSync)
            {
                if (!ReferenceEquals(sender, _audioConnection))
                {
                    return;
                }
            }

            if (state != AudioPlaybackConnectionState.Opened)
            {
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StateChanged] Error reading state: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsBluetoothDeviceConnectedAsync(string deviceId)
    {
        try
        {
            AudioPlaybackConnection? connection;
            string? activeDeviceId;
            lock (_connectionSync)
            {
                connection = _audioConnection;
                activeDeviceId = _activeDeviceId;
            }

            if (activeDeviceId != deviceId || connection == null)
            {
                Debug.WriteLine($"[IsDeviceConnected] returning false: reason=no-active-connection, activeId={activeDeviceId ?? "null"}, queriedId={deviceId}");
                return false;
            }

            var state = connection.State;
            if (state != AudioPlaybackConnectionState.Opened)
            {
                Debug.WriteLine($"[IsDeviceConnected] returning false: reason=state-not-opened, connectionState={state}");
                return false;
            }

            try
            {
                var deviceInfo = await DeviceInformation.CreateFromIdAsync(
                    deviceId,
                    new[] { "System.Devices.Aep.IsConnected" });

                if (deviceInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var value)
                    && value is bool btConnected
                    && !btConnected)
                {
                    if (deviceId.EndsWith("\\SNK", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"[IsDeviceConnected] AEP reports disconnected for active SNK endpoint; trusting AudioPlaybackConnection.State={state}.");
                    }
                    else
                    {
                        Debug.WriteLine($"[IsDeviceConnected] returning false: reason=aep-disconnected, connectionState={state}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IsDeviceConnected] DeviceInfo query failed (assuming connected): {ex.Message}");
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IsDeviceConnected] Error: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsBluetoothPhysicallyConnectedAsync(string deviceId)
    {
        try
        {
            AudioPlaybackConnection? connection;
            string? activeDeviceId;
            lock (_connectionSync)
            {
                connection = _audioConnection;
                activeDeviceId = _activeDeviceId;
            }

            if (string.Equals(activeDeviceId, deviceId, StringComparison.OrdinalIgnoreCase)
                && connection?.State == AudioPlaybackConnectionState.Opened)
            {
                return true;
            }

            var devices = await GetConnectedAudioPlaybackDevicesAsync();
            var isConnected = devices.Any(device =>
                string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            Debug.WriteLine($"[IsPhysicallyConnected] returning {isConnected}: reason=selector-presence");
            return isConnected;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IsPhysicallyConnected] Error: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public void Disconnect(string reason = "unspecified", bool preserveDisconnectTimestamp = false)
    {
        TearDownAudioConnection(reason, preserveDisconnectTimestamp);
    }

    /// <summary>
    /// Releases the audio connection and suppresses finalization.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the audio connection resources.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Disconnect("dispose");
        }
    }
}
