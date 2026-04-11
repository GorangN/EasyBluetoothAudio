using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Implements audio services using the Windows AudioPlaybackConnection API for Bluetooth A2DP Sink connectivity.
/// Windows handles all audio routing natively once the connection is established.
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    /// <summary>
    /// Milliseconds to wait between <c>Start()</c> and <c>OpenAsync()</c> when the Bluetooth
    /// device was not yet physically connected at the time <c>Start()</c> was called.
    /// <c>Start()</c> initiates the underlying Bluetooth link negotiation; this delay gives
    /// the BT stack time to complete that negotiation before the A2DP audio stream is opened.
    /// </summary>
    private const int SettleDelayAfterStartMs = 5_000;

    /// <summary>
    /// Minimum delay in milliseconds between <c>Start()</c> and <c>OpenAsync()</c> applied
    /// unconditionally, even when the Bluetooth link is already established.
    /// Gives the Windows audio stack a moment to register the new endpoint before opening
    /// the A2DP stream.
    /// </summary>
    private const int MinStartToOpenDelayMs = 500;

    private readonly IDispatcherService _dispatcherService;
    private AudioPlaybackConnection? _audioConnection;
    private volatile bool _isAudioConnectionActive;
    private string? _activeDeviceId;

    /// <summary>
    /// The device ID for which <see cref="AudioPlaybackConnection.Start"/> has been called
    /// but <see cref="AudioPlaybackConnection.OpenAsync"/> has not yet succeeded.
    /// Tracked separately from <see cref="_activeDeviceId"/> so that the BT connection
    /// can be reused across <see cref="ConnectBluetoothAudioAsync"/> retries without
    /// restarting link negotiation.
    /// </summary>
    private string? _pendingDeviceId;

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

    /// <inheritdoc />
    public async Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync()
    {
        var result = new List<BluetoothDevice>();
        try
        {
            var selector = AudioPlaybackConnection.GetDeviceSelector();
            string[] requestedProperties = { "System.Devices.Aep.IsConnected" };
            var devices = await DeviceInformation.FindAllAsync(selector, requestedProperties);

            foreach (var d in devices)
            {
                bool connected = false;
                try
                {
                    if (d.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var value) && value is bool isConnected)
                    {
                        connected = isConnected;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DeviceDiscover] Error retrieving properties for {d.Name}: {ex.Message}");
                }

                result.Add(new BluetoothDevice
                {
                    Name = d.Name,
                    Id = d.Id,
                    IsConnected = connected,
                    IsPhoneOrComputer = true
                });

                Debug.WriteLine($"[DeviceDiscover] Found Source: {d.Name} (ID: {d.Id}, Connected: {connected})");
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
        try
        {
            // Tear down only when switching to a different device.  When retrying for the
            // same device after an OpenAsync failure we deliberately reuse the existing
            // AudioPlaybackConnection so that the BT stack's ongoing link-negotiation
            // (initiated by Start()) is not reset on each attempt.
            // Use _pendingDeviceId (set when Start() is called) rather than _activeDeviceId
            // (set only on success) so the reuse path triggers correctly during retries.
            if (_audioConnection != null && _pendingDeviceId != deviceId)
            {
                _audioConnection.StateChanged -= OnAudioConnectionStateChanged;
                _audioConnection.Dispose();
                _audioConnection = null;
                _pendingDeviceId = null;
            }

            _isAudioConnectionActive = false;
            _activeDeviceId = null;

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");

            int settleDelayMs;

            if (_audioConnection == null)
            {
                // First attempt for this device: check physical connectivity, create the
                // connection, and call Start() to initiate BT link negotiation.
                var wasPhysicallyConnected = await IsBluetoothPhysicallyConnectedAsync(deviceId);

                // All WinRT audio API calls must run on the UI (STA) thread.
                await _dispatcherService.InvokeAsync(() =>
                {
                    _audioConnection = AudioPlaybackConnection.TryCreateFromId(deviceId);
                    if (_audioConnection == null)
                    {
                        return Task.CompletedTask;
                    }

                    _audioConnection.StateChanged += OnAudioConnectionStateChanged;
                    _audioConnection.Start();
                    _pendingDeviceId = deviceId;
                    return Task.CompletedTask;
                });

                if (_audioConnection == null)
                {
                    Debug.WriteLine("[ConnectBT] Failed to create AudioPlaybackConnection from ID.");
                    return false;
                }

                // When the physical link was not yet up, add the full settle delay so the
                // BT stack can complete link negotiation before OpenAsync() is called.
                settleDelayMs = wasPhysicallyConnected
                    ? MinStartToOpenDelayMs
                    : SettleDelayAfterStartMs + MinStartToOpenDelayMs;

                Debug.WriteLine($"[ConnectBT] Settling {settleDelayMs}ms before OpenAsync (physicallyConnected={wasPhysicallyConnected})...");
            }
            else
            {
                // Retry for the same device: Start() was already called; BT negotiation is
                // ongoing.  Wait only the minimum delay before retrying OpenAsync().
                settleDelayMs = MinStartToOpenDelayMs;
                Debug.WriteLine($"[ConnectBT] Reusing existing connection, retrying OpenAsync after {settleDelayMs}ms...");
            }

            await Task.Delay(settleDelayMs);

            AudioPlaybackConnectionOpenResult? openResult = null;
            await _dispatcherService.InvokeAsync(async () =>
            {
                if (_audioConnection == null)
                {
                    return;
                }

                openResult = await _audioConnection.OpenAsync();
            });

            if (openResult?.Status == AudioPlaybackConnectionOpenResultStatus.Success)
            {
                Debug.WriteLine("[ConnectBT] AudioPlaybackConnection Success!");
                _activeDeviceId = deviceId;
                _isAudioConnectionActive = true;
                return true;
            }

            Debug.WriteLine($"[ConnectBT] Failed status: {openResult?.Status}");

            // Do NOT dispose _audioConnection on failure.  Keeping it alive preserves the
            // BT stack's negotiation state so the next retry (from the monitor reconnect
            // loop) can call OpenAsync() again without restarting the physical link setup.
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConnectBT] Error: {ex.Message}");
            if (_audioConnection != null)
            {
                _audioConnection.StateChanged -= OnAudioConnectionStateChanged;
                _audioConnection.Dispose();
                _audioConnection = null;
            }

            _pendingDeviceId = null;
            return false;
        }
    }

    private void OnAudioConnectionStateChanged(AudioPlaybackConnection sender, object args)
    {
        try
        {
            _isAudioConnectionActive = sender.State == AudioPlaybackConnectionState.Opened;
            Debug.WriteLine($"[StateChanged] AudioPlaybackConnection state changed to: {sender.State} at {DateTime.Now:HH:mm:ss.fff}");
            if (sender.State != AudioPlaybackConnectionState.Opened)
            {
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StateChanged] Error reading state: {ex.Message}");
            _isAudioConnectionActive = false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsBluetoothDeviceConnectedAsync(string deviceId)
    {
        try
        {
            if (_activeDeviceId != deviceId || _audioConnection == null)
            {
                return false;
            }

            // Primary truth: is the physical Bluetooth link still up?
            // This is checked first because AudioPlaybackConnection.State can legitimately
            // be Closed when the phone suspends the A2DP channel due to inactivity — that
            // is NOT a real disconnect and must not trigger a teardown.
            try
            {
                var deviceInfo = await DeviceInformation.CreateFromIdAsync(
                    deviceId,
                    new[] { "System.Devices.Aep.IsConnected" });

                if (deviceInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var val)
                    && val is bool btConnected && !btConnected)
                {
                    Debug.WriteLine($"[IsDeviceConnected] Physical link gone at {DateTime.Now:HH:mm:ss.fff}");
                    _isAudioConnectionActive = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Device info query failed transiently; fall back to State check.
                Debug.WriteLine($"[IsDeviceConnected] DeviceInfo query failed (falling back to State): {ex.Message}");
                if (_audioConnection.State != AudioPlaybackConnectionState.Opened)
                {
                    _isAudioConnectionActive = false;
                    return false;
                }

                return true;
            }

            // If the A2DP channel is not open, signal disconnection so the monitor's
            // reconnect loop can create a fresh AudioPlaybackConnection and call Start()
            // again. The monitor's 2-failure hysteresis (2 × 10 s) prevents false-positive
            // teardowns from a single transient A2DP idle blip.
            if (_audioConnection.State != AudioPlaybackConnectionState.Opened)
            {
                Debug.WriteLine($"[IsDeviceConnected] A2DP not open (State={_audioConnection.State}) at {DateTime.Now:HH:mm:ss.fff}");
                _isAudioConnectionActive = false;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IsDeviceConnected] Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Determines whether the specified Bluetooth device is physically paired and connected
    /// in Windows, independent of any active audio connection.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device to check.</param>
    /// <returns>
    /// <c>true</c> if <c>System.Devices.Aep.IsConnected</c> reports <see langword="true"/>;
    /// otherwise <c>false</c>.
    /// </returns>
    private async Task<bool> IsBluetoothPhysicallyConnectedAsync(string deviceId)
    {
        try
        {
            var deviceInfo = await DeviceInformation.CreateFromIdAsync(
                deviceId,
                new[] { "System.Devices.Aep.IsConnected" });

            if (deviceInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var val)
                && val is bool btConnected)
            {
                return btConnected;
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IsPhysicallyConnected] Error: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        if (_audioConnection != null)
        {
            Debug.WriteLine("[Disconnect] Closing connection...");
            _audioConnection.StateChanged -= OnAudioConnectionStateChanged;
            _audioConnection.Dispose();
            _audioConnection = null;
            _activeDeviceId = null;
            _pendingDeviceId = null;
            _isAudioConnectionActive = false;
        }
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
            Disconnect();
        }
    }
}
