using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using EasyBluetoothAudio.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Implements audio services using the Windows AudioPlaybackConnection API for Bluetooth A2DP Sink connectivity.
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    private readonly IDispatcherService _dispatcherService;
    private AudioPlaybackConnection? _audioConnection;
    private WasapiCapture? _capture;
    private WasapiOut? _render;
    private BufferedWaveProvider? _waveProvider;
    private volatile bool _isAudioConnectionActive;
    private string? _activeDeviceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioService"/> class.
    /// </summary>
    /// <param name="dispatcherService">The dispatcher service for UI thread operations.</param>
    public AudioService(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;
    }

    /// <inheritdoc />
    public bool IsRouting { get; private set; }

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
    public IEnumerable<AudioDevice> GetOutputDevices()
    {
        var list = new List<AudioDevice>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var d in devices)
            {
                list.Add(new AudioDevice { Name = d.FriendlyName, Id = d.ID });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[GetOutputDevices] Error: {ex.Message}");
        }
        return list;
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StateChanged] Error reading state: {ex.Message}");
            _isAudioConnectionActive = false;
        }
    }

    /// <inheritdoc />
    public Task StartRoutingAsync(string captureDeviceFriendlyName, string? outputDeviceId, int bufferMs)
    {
        if (IsRouting) return Task.CompletedTask;

        try
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            var captureDevice = devices.FirstOrDefault(d => d.FriendlyName.Contains(captureDeviceFriendlyName, StringComparison.OrdinalIgnoreCase));

            if (captureDevice == null)
            {
                Debug.WriteLine($"[StartRouting] Device '{captureDeviceFriendlyName}' not found.");
                return Task.CompletedTask;
            }

            Debug.WriteLine($"[StartRouting] Using capture device: {captureDevice.FriendlyName}");

            _capture = new WasapiCapture(captureDevice, true, bufferMs);
            _waveProvider = new BufferedWaveProvider(_capture.WaveFormat);
            _capture.DataAvailable += (s, e) => _waveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);

            MMDevice? renderDevice = null;
            if (!string.IsNullOrEmpty(outputDeviceId))
            {
                try
                {
                    renderDevice = enumerator.GetDevice(outputDeviceId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[StartRouting] Could not find output device {outputDeviceId}: {ex.Message}");
                }
            }

            if (renderDevice == null)
            {
                renderDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }

            Debug.WriteLine($"[StartRouting] Using output device: {renderDevice.FriendlyName}");

            _render = new WasapiOut(renderDevice, AudioClientShareMode.Shared, true, bufferMs);
            _render.Init(_waveProvider);

            _capture.StartRecording();
            _render.Play();

            IsRouting = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StartRouting] Error: {ex.Message}");
            StopRouting();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsBluetoothDeviceConnectedAsync(string deviceId)
    {
        try
        {
            if (_activeDeviceId == deviceId)
            {
                return Task.FromResult(_isAudioConnectionActive);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IsDeviceConnected] Error: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public void StopRouting()
    {
        if (_audioConnection != null)
        {
            Debug.WriteLine("[StopRouting] Closing connection...");
            _audioConnection.StateChanged -= OnAudioConnectionStateChanged;
            _audioConnection.Dispose();
            _audioConnection = null;
            _activeDeviceId = null;
            _isAudioConnectionActive = false;
        }

        if (_capture != null)
        {
            _capture.StopRecording();
            _capture.Dispose();
            _capture = null;
        }

        if (_render != null)
        {
            _render.Stop();
            _render.Dispose();
            _render = null;
        }

        _waveProvider = null;
        IsRouting = false;
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
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopRouting();
        }
    }
}
