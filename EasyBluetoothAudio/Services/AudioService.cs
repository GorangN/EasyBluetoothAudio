using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
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
    private string? _activeDeviceName;
    private bool _hasConnectedBefore;
    private bool _hasDumpedSessions;
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
                _activeDeviceName = await TryGetDeviceFriendlyNameAsync(deviceId);
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
        _activeDeviceName = null;
        _hasDumpedSessions = false;
        _lastDisconnectTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Fetches the human-readable name for a given WinRT device ID, used to match the
    /// Bluetooth capture endpoint exposed by CoreAudio (whose <c>FriendlyName</c> embeds
    /// the same device name). Returns <see langword="null"/> when the lookup fails so
    /// the peak-meter heuristic falls back to "no judgment" rather than a false zombie verdict.
    /// </summary>
    /// <param name="deviceId">The WinRT device identifier of the connected Bluetooth source.</param>
    /// <returns>The friendly name, or <see langword="null"/> on failure.</returns>
    private static async Task<string?> TryGetDeviceFriendlyNameAsync(string deviceId)
    {
        try
        {
            var info = await DeviceInformation.CreateFromIdAsync(deviceId);
            return string.IsNullOrWhiteSpace(info?.Name) ? null : info.Name;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConnectBT] Failed to resolve friendly name for {deviceId}: {ex.Message}");
            return null;
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
                    && val is bool btConnected
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

    /// <inheritdoc />
    public float? GetActiveDevicePeakLevel()
    {
        var activeDeviceName = _activeDeviceName;
        if (string.IsNullOrEmpty(activeDeviceName))
        {
            return null;
        }

        try
        {
            using var enumerator = new MMDeviceEnumerator();

            // Preferred match: a Capture endpoint whose FriendlyName embeds the BT device name.
            // Some BT drivers expose the A2DP source that way; the WinRT AudioPlaybackConnection
            // path used by this app does not. When no match is found we fall through to the
            // per-session Render fallback below.
            var captureEndpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            try
            {
                foreach (var endpoint in captureEndpoints)
                {
                    if (endpoint.FriendlyName.Contains(activeDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        var peak = endpoint.AudioMeterInformation.MasterPeakValue;
                        Debug.WriteLine($"[PeakMeter] capture={endpoint.FriendlyName} peak={peak:F4}");
                        return peak;
                    }
                }
            }
            finally
            {
                foreach (var endpoint in captureEndpoints)
                {
                    endpoint.Dispose();
                }
            }

            // Fallback: inspect sessions on the Default Render endpoint and only trust a session
            // that can be matched back to the active Bluetooth device. If no session matches, we
            // return null instead of trusting the aggregated endpoint peak.
            using var defaultRender = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var sessionManager = defaultRender.AudioSessionManager;
            var sessions = sessionManager.Sessions;
            var shouldDumpSessions = !_hasDumpedSessions;
            float? matchedPeak = null;
            string? matchedSession = null;

            if (shouldDumpSessions && sessions.Count == 0)
            {
                Debug.WriteLine($"[PeakMeter][Sessions] No render sessions found on '{defaultRender.FriendlyName}'.");
            }

            for (var i = 0; i < sessions.Count; i++)
            {
                try
                {
                    using var session = sessions[i];
                    var displayName = session.DisplayName ?? string.Empty;
                    var iconPath = session.IconPath ?? string.Empty;
                    var processId = session.GetProcessID;
                    var state = session.State;
                    var peak = session.AudioMeterInformation.MasterPeakValue;

                    if (shouldDumpSessions)
                    {
                        var loggedDisplayName = string.IsNullOrWhiteSpace(displayName) ? "<empty>" : displayName;
                        var loggedIconPath = string.IsNullOrWhiteSpace(iconPath) ? "<empty>" : iconPath;
                        Debug.WriteLine($"[PeakMeter][Sessions] idx={i} displayName='{loggedDisplayName}' iconPath='{loggedIconPath}' pid={processId} state={state} peak={peak:F4}");
                    }

                    if (matchedPeak == null &&
                        (displayName.Contains(activeDeviceName, StringComparison.OrdinalIgnoreCase) ||
                         iconPath.Contains(activeDeviceName, StringComparison.OrdinalIgnoreCase)))
                    {
                        matchedPeak = peak;
                        matchedSession = string.IsNullOrWhiteSpace(displayName) ? iconPath : displayName;

                        if (!shouldDumpSessions)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PeakMeter][Sessions] idx={i} error={ex.Message}");
                }
            }

            if (shouldDumpSessions)
            {
                _hasDumpedSessions = true;
            }

            if (matchedPeak.HasValue)
            {
                Debug.WriteLine($"[PeakMeter] session match='{matchedSession}' peak={matchedPeak.Value:F4}");
                return matchedPeak.Value;
            }

            Debug.WriteLine($"[PeakMeter] No session matched '{activeDeviceName}' - zombie detection disabled until matcher is refined.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PeakMeter] Error reading peak: {ex.Message}");
            return null;
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
