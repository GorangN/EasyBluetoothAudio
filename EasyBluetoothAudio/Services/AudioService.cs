using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
            _audioConnection?.Dispose();
            _audioConnection = null;
            _isAudioConnectionActive = false;
            _activeDeviceId = null;

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");

            // Determine whether a settle delay is needed between Start() and OpenAsync().
            // On the first connect after app start, a stale AudioPlaybackConnection from the
            // prior session may still be registered — always settle.
            // On subsequent connects, settle only if the physical BT link was torn down.
            var needsSettle = !_hasConnectedBefore
                || !await IsBluetoothPhysicallyConnectedAsync(deviceId);

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
                    var timeSinceDisconnect = (DateTime.UtcNow - _lastDisconnectTime).TotalMilliseconds;
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
            _lastDisconnectTime = DateTime.UtcNow;
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

    /// <inheritdoc />
    public bool IsAudioCurrentlyPlaying()
    {
        object? enumeratorObj = null;
        object? deviceObj = null;
        object? meterObj = null;
        try
        {
            enumeratorObj = new MMDeviceEnumeratorCoClass();
            var enumerator = (IMMDeviceEnumerator)enumeratorObj;

            const int eRender = 0;
            const int eMultimedia = 1;
            enumerator.GetDefaultAudioEndpoint(eRender, eMultimedia, out var device);
            deviceObj = device;

            var meterGuid = typeof(IAudioMeterInformation).GUID;
            const int clsCtxAll = 23;
            device.Activate(ref meterGuid, clsCtxAll, IntPtr.Zero, out meterObj);
            var meter = (IAudioMeterInformation)meterObj;

            meter.GetPeakValue(out float peak);
            return peak > 0.0001f;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioMeter] Error reading peak meter: {ex.Message}");
            // On failure, report no audio — the keepalive timer will eventually fire,
            // which is the safe default (a harmless sub-second recycle).
            return false;
        }
        finally
        {
            if (meterObj != null) { Marshal.ReleaseComObject(meterObj); }
            if (deviceObj != null) { Marshal.ReleaseComObject(deviceObj); }
            if (enumeratorObj != null) { Marshal.ReleaseComObject(enumeratorObj); }
        }
    }

    #region Core Audio COM Interop (peak meter)

    /// <summary>COM co-class for <see cref="IMMDeviceEnumerator"/>.</summary>
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorCoClass
    {
    }

    /// <summary>Minimal declaration of the Core Audio IMMDeviceEnumerator interface.</summary>
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        /// <summary>Placeholder for vtable slot — not called.</summary>
        void EnumAudioEndpoints(
            int dataFlow,
            uint stateMask,
            [MarshalAs(UnmanagedType.IUnknown)] out object devices);

        /// <summary>Returns the default audio endpoint for the specified data-flow direction and role.</summary>
        void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
    }

    /// <summary>Minimal declaration of the Core Audio IMMDevice interface.</summary>
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        /// <summary>Activates a COM interface on this device endpoint.</summary>
        void Activate(
            ref Guid iid,
            int clsCtx,
            IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    /// <summary>Minimal declaration of the Core Audio IAudioMeterInformation interface.</summary>
    [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMeterInformation
    {
        /// <summary>Returns the current peak sample value for all channels.</summary>
        void GetPeakValue(out float pfPeak);
    }

    #endregion
}
