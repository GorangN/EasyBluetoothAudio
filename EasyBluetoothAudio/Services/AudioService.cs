using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Implements audio services using the Windows AudioPlaybackConnection API for Bluetooth A2DP Sink connectivity.
/// </summary>
public class AudioService : IAudioService, IDisposable
{
    private AudioPlaybackConnection? _audioConnection;

    /// <inheritdoc />
    public bool IsRouting { get; private set; }

    /// <inheritdoc />
    public async Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync()
    {
        var result = new List<BluetoothDevice>();
        try
        {
            // Use the dedicated selector for AudioPlaybackConnection (A2DP Source role)
            // This naturally filters for devices that can send audio to this PC.
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
                catch
                {
                    // Property access may fail
                }

                result.Add(new BluetoothDevice
                {
                    Name = d.Name,
                    Id = d.Id,
                    IsConnected = connected,
                    IsPhoneOrComputer = true // Devices found via this selector ARE sources
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

            Debug.WriteLine($"[ConnectBT] Connecting to audio endpoint {deviceId}...");
            
            // Must execute on UI thread for certain WinRT/WPF interactions
            _audioConnection = System.Windows.Application.Current.Dispatcher.Invoke(() => 
                AudioPlaybackConnection.TryCreateFromId(deviceId));
            
            if (_audioConnection == null)
            {
                Debug.WriteLine($"[ConnectBT] Failed to create AudioPlaybackConnection from ID.");
                return false;
            }

            _audioConnection.Start();
            var openResult = await _audioConnection.OpenAsync();

            if (openResult.Status == AudioPlaybackConnectionOpenResultStatus.Success)
            {
                Debug.WriteLine("[ConnectBT] AudioPlaybackConnection Success!");
                IsRouting = true; 
                return true; 
            }
            else
            {
                Debug.WriteLine($"[ConnectBT] Failed status: {openResult.Status}");
                _audioConnection.Dispose();
                _audioConnection = null;
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConnectBT] Error: {ex.Message}");
            _audioConnection?.Dispose();
            _audioConnection = null;
            return false;
        }
    }

    /// <inheritdoc />
    public Task StartRoutingAsync(string captureDeviceFriendlyName, int bufferMs)
    {
        IsRouting = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void StopRouting()
    {
        if (_audioConnection != null)
        {
            Debug.WriteLine("[StopRouting] Closing connection...");
            _audioConnection.Dispose();
            _audioConnection = null;
        }
        IsRouting = false;
    }

    /// <summary>
    /// Disposes resources used by the service.
    /// </summary>
    public void Dispose()
    {
        StopRouting();
        GC.SuppressFinalize(this);
    }
}
