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
    /// Applied on the first connect, or when the last <i>real</i> disconnect happened within
    /// this window. Internal recycles (pre-connect reset, user-triggered reconnect while
    /// the phone stays up) do not update <see cref="_lastDisconnectTime"/>, so they bypass the
    /// settle.
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

            foreach (var device in devices)
            {
                var connected = false;
                try
                {
                    if (device.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var value)
                        && value is bool isConnected)
                    {
                        connected = isConnected;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DeviceDiscover] Error retrieving properties for {device.Name}: {ex.Message}");
                }

                result.Add(new BluetoothDevice
                {
                    Name = device.Name,
                    Id = device.Id,
                    IsConnected = connected,
                    IsPhoneOrComputer = true
                });

                Debug.WriteLine($"[DeviceDiscover] Found Source: {device.Name} (ID: {device.Id}, Connected: {connected})");
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
            TearDownAudioConnection("pre-connect", updateDisconnectTimestamp: false);

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");

            // Settle only when the last real disconnect was within SettleDelayMs, or on the
            // first connect. Internal recycles (pre-connect teardown, manual-recover while the
            // phone stays up) leave _lastDisconnectTime untouched, so timeSinceDisconnect
            // reflects the last actual BT-layer loss rather than our own audio-endpoint reset.
            // AEP.IsConnected cannot be used here: it reports False for \SNK endpoints even
            // when the phone is still BT-connected (see lessons.md).
            var timeSinceDisconnect = (DateTime.UtcNow - _lastDisconnectTime).TotalMilliseconds;
            var needsSettle = !_hasConnectedBefore || timeSinceDisconnect < SettleDelayMs;

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
                _lastDisconnectTime = DateTime.MinValue;
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
    /// connection and resets tracking fields. Real disconnect/failure teardowns update
    /// <see cref="_lastDisconnectTime"/> so subsequent connect attempts apply the Settle delay
    /// against the last actual disconnect instead of an internal pre-connect reset.
    /// </summary>
    /// <param name="reason">Short tag describing why the teardown is happening (logged when a connection was actually open).</param>
    /// <param name="updateDisconnectTimestamp"><see langword="true"/> when this teardown represents a real disconnect or failed connect attempt; otherwise <see langword="false"/>.</param>
    private void TearDownAudioConnection(string reason, bool updateDisconnectTimestamp = true)
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
        if (updateDisconnectTimestamp)
        {
            _lastDisconnectTime = DateTime.UtcNow;
        }
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

            var state = _audioConnection.State;
            if (state != AudioPlaybackConnectionState.Opened)
            {
                Debug.WriteLine($"[IsDeviceConnected] returning false: reason=state-not-opened, connectionState={state}");
                _isAudioConnectionActive = false;
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
                        _isAudioConnectionActive = false;
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
            var deviceInfo = await DeviceInformation.CreateFromIdAsync(
                deviceId,
                new[] { "System.Devices.Aep.IsConnected" });

            if (deviceInfo.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var value)
                && value is bool btConnected)
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
    public void Disconnect(string reason = "unspecified", bool preserveDisconnectTimestamp = false)
    {
        if (_audioConnection != null)
        {
            Debug.WriteLine($"[Disconnect] Closing connection (reason={reason})...");
            TearDownAudioConnection(reason, updateDisconnectTimestamp: !preserveDisconnectTimestamp);
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
