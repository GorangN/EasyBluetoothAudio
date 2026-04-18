using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    /// Milliseconds to wait between <c>Start()</c> and <c>OpenAsync()</c> to allow Windows
    /// to complete teardown of the previous <see cref="AudioPlaybackConnection"/> before the
    /// new audio endpoint negotiates A2DP with the remote device.
    /// Applied on the first connect (stale endpoint from prior session) and after a disconnect
    /// where the physical Bluetooth link was torn down.
    /// </summary>
    internal const int SettleDelayMs = 5_000;

    private readonly IDispatcherService _dispatcherService;
    private AudioPlaybackConnection? _audioConnection;
    private volatile bool _isAudioConnectionActive;
    private string? _activeDeviceId;
    private bool _hasConnectedBefore;
    private DateTime _lastDisconnectTime = DateTime.UtcNow;

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
            TearDownAudioConnection("pre-connect");

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");

            // A settle delay between Start() and OpenAsync() is required whenever the previous
            // audio endpoint was torn down less than SettleDelayMs ago — regardless of whether
            // the physical BT link is still up. In the common zombie-state scenario the ACL link
            // stays connected while Windows rebuilds the A2DP endpoint, and skipping the settle
            // there is what produces the UnknownFailure burst in the retry loop.
            var timeSinceDisconnect = (DateTime.UtcNow - _lastDisconnectTime).TotalMilliseconds;
            var needsSettle = !_hasConnectedBefore || timeSinceDisconnect < SettleDelayMs;

            // All WinRT audio API calls must run on the UI (STA) thread.
            // When called from a background thread (e.g. the reconnect monitor), Start() and
            // OpenAsync() fail silently on the MTA thread pool — dispatching everything here
            // ensures the same threading behaviour as a user-initiated connect.
            AudioPlaybackConnectionOpenResult? openResult = null;
            await _dispatcherService.InvokeAsync(async () =>
            {
                _audioConnection = AudioPlaybackConnection.TryCreateFromId(deviceId);
                if (_audioConnection == null)
                {
                    return;
                }

                _audioConnection.StateChanged += OnAudioConnectionStateChanged;
                _audioConnection.Start();

                // Allow Windows to complete teardown of the previous audio endpoint before
                // the new connection negotiates A2DP with the remote device.
                if (needsSettle)
                {
                    var settleRemaining = SettleDelayMs - (int)timeSinceDisconnect;
                    if (settleRemaining > 0)
                    {
                        Debug.WriteLine($"[ConnectBT] Settling {settleRemaining}ms between Start() and OpenAsync()...");
                        await Task.Delay(settleRemaining);
                    }
                }

                openResult = await _audioConnection.OpenAsync();
            });

            if (_audioConnection == null)
            {
                Debug.WriteLine("[ConnectBT] Failed to create AudioPlaybackConnection from ID.");
                return false;
            }

            if (openResult?.Status == AudioPlaybackConnectionOpenResultStatus.Success)
            {
                Debug.WriteLine("[ConnectBT] AudioPlaybackConnection Success!");
                _activeDeviceId = deviceId;
                _isAudioConnectionActive = true;
                _hasConnectedBefore = true;
                return true;
            }

            Debug.WriteLine($"[ConnectBT] Failed status: {openResult?.Status}");
            TearDownAudioConnection($"open-failed-{openResult?.Status}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConnectBT] Error: {ex.Message}");
            TearDownAudioConnection("connect-exception");
            return false;
        }
    }

    /// <summary>
    /// Unhooks the <see cref="AudioPlaybackConnection.StateChanged"/> handler, disposes the current
    /// connection and resets tracking fields. Always updates <see cref="_lastDisconnectTime"/> so
    /// subsequent connect attempts apply the Settle delay against a correct timestamp.
    /// </summary>
    /// <param name="reason">Short tag describing why the teardown is happening (logged when a connection was actually open).</param>
    private void TearDownAudioConnection(string reason)
    {
        if (_audioConnection != null)
        {
            Debug.WriteLine($"[AudioService] Tearing down connection (reason={reason}).");
            _audioConnection.StateChanged -= OnAudioConnectionStateChanged;
            _audioConnection.Dispose();
            _audioConnection = null;
        }

        _isAudioConnectionActive = false;
        _activeDeviceId = null;
        _lastDisconnectTime = DateTime.UtcNow;
    }

    private void OnAudioConnectionStateChanged(AudioPlaybackConnection sender, object args)
    {
        try
        {
            var state = sender.State;
            Debug.WriteLine($"[StateChanged] State={state}");
            _isAudioConnectionActive = state == AudioPlaybackConnectionState.Opened;
            if (state != AudioPlaybackConnectionState.Opened)
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
                Debug.WriteLine($"[IsDeviceConnected] returning false: reason=no-active-connection, activeId={_activeDeviceId ?? "null"}, queriedId={deviceId}");
                return false;
            }

            // Directly read the connection state instead of relying solely on the event-driven flag,
            // because StateChanged does not fire reliably when the device goes out of range.
            var state = _audioConnection.State;
            if (state != AudioPlaybackConnectionState.Opened)
            {
                Debug.WriteLine($"[IsDeviceConnected] returning false: reason=state-not-opened, connectionState={state}");
                _isAudioConnectionActive = false;
                return false;
            }

            // Cross-check with Windows device manager to detect out-of-range scenarios
            // where AudioPlaybackConnection.State may still appear Opened.
            try
            {
                var deviceInfo = await DeviceInformation.CreateFromIdAsync(
                    deviceId,
                    new[] { "System.Devices.Aep.IsConnected" });

                if (deviceInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var val)
                    && val is bool btConnected && !btConnected)
                {
                    Debug.WriteLine($"[IsDeviceConnected] returning false: reason=aep-disconnected, connectionState={state}");
                    _isAudioConnectionActive = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                // DeviceInfo query failed transiently (e.g. WinRT error during Bluetooth teardown).
                // AudioPlaybackConnection.State was already confirmed Opened above, so trust that.
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
    public void Disconnect(string reason = "unspecified")
    {
        if (_audioConnection != null)
        {
            Debug.WriteLine($"[Disconnect] Closing connection (reason={reason})...");
            TearDownAudioConnection(reason);
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
            Disconnect("dispose");
        }
    }

}
