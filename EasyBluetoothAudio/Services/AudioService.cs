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
            if (_audioConnection != null)
            {
                _audioConnection.StateChanged -= OnAudioConnectionStateChanged;
                _audioConnection.Dispose();
                _audioConnection = null;
            }

            _isAudioConnectionActive = false;
            _activeDeviceId = null;

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");

            // Check physical connectivity before Start() so we know whether to insert a
            // settle delay between Start() and OpenAsync().  Start() initiates the Bluetooth
            // link; if the physical link was not yet up the BT stack needs time to complete
            // that negotiation before A2DP can be opened reliably.
            var wasPhysicallyConnected = await IsBluetoothPhysicallyConnectedAsync(deviceId);

            // All WinRT audio API calls must run on the UI (STA) thread.
            // Start() is dispatched first so the settle delay can be awaited on the background
            // thread between Start() and OpenAsync() without blocking the UI dispatcher.
            await _dispatcherService.InvokeAsync(() =>
            {
                _audioConnection = AudioPlaybackConnection.TryCreateFromId(deviceId);
                if (_audioConnection == null)
                {
                    return Task.CompletedTask;
                }

                _audioConnection.StateChanged += OnAudioConnectionStateChanged;
                _audioConnection.Start();
                return Task.CompletedTask;
            });

            if (_audioConnection == null)
            {
                Debug.WriteLine("[ConnectBT] Failed to create AudioPlaybackConnection from ID.");
                return false;
            }

            // Always wait a minimum delay so the Windows audio stack can register the new
            // endpoint before OpenAsync() is called.  When the physical link was not yet up,
            // add the full settle delay on top to give the BT stack time to complete
            // link negotiation.
            var totalDelay = wasPhysicallyConnected
                ? MinStartToOpenDelayMs
                : SettleDelayAfterStartMs + MinStartToOpenDelayMs;

            Debug.WriteLine($"[ConnectBT] Settling {totalDelay}ms before OpenAsync (physicallyConnected={wasPhysicallyConnected})...");
            await Task.Delay(totalDelay);

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
            if (_audioConnection != null)
            {
                _audioConnection.StateChanged -= OnAudioConnectionStateChanged;
                _audioConnection.Dispose();
                _audioConnection = null;
            }
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
            return false;
        }
    }

    private void OnAudioConnectionStateChanged(AudioPlaybackConnection sender, object args)
    {
        try
        {
            _isAudioConnectionActive = sender.State == AudioPlaybackConnectionState.Opened;
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

            // Directly read the connection state instead of relying solely on the event-driven flag,
            // because StateChanged does not fire reliably when the device goes out of range.
            if (_audioConnection.State != AudioPlaybackConnectionState.Opened)
            {
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
