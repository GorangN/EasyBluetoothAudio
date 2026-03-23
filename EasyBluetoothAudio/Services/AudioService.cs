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
            _audioConnection?.Dispose();
            _audioConnection = null;
            _isAudioConnectionActive = false;
            _activeDeviceId = null;

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");

            _dispatcherService.Invoke(() =>
                _audioConnection = AudioPlaybackConnection.TryCreateFromId(deviceId));

            if (_audioConnection == null)
            {
                Debug.WriteLine("[ConnectBT] Failed to create AudioPlaybackConnection from ID.");
                return false;
            }

            _audioConnection.StateChanged += OnAudioConnectionStateChanged;
            _audioConnection.Start();
            var openResult = await _audioConnection.OpenAsync();

            if (openResult.Status == AudioPlaybackConnectionOpenResultStatus.Success)
            {
                Debug.WriteLine("[ConnectBT] AudioPlaybackConnection Success!");
                _activeDeviceId = deviceId;
                _isAudioConnectionActive = true;
                return true;
            }

            Debug.WriteLine($"[ConnectBT] Failed status: {openResult.Status}");
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
                Debug.WriteLine($"[IsDeviceConnected] DeviceInfo query failed: {ex.Message}");
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
