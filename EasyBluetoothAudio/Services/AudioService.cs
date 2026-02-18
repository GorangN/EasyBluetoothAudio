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
            var selector = Windows.Devices.Bluetooth.BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            // Request the Class of Device property in addition to default properties
            string[] requestedProperties = { "System.Devices.Aep.IsConnected", "System.Devices.Bluetooth.ClassOfDevice" };
            var devices = await DeviceInformation.FindAllAsync(selector, requestedProperties);

            foreach (var d in devices)
            {
                bool connected = false;
                bool isPhoneOrComputer = false;
                try
                {
                    if (d.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var value) && value is bool isConnected)
                    {
                        connected = isConnected;
                    }

                    if (d.Properties.TryGetValue("System.Devices.Bluetooth.ClassOfDevice", out var codValue) && codValue is uint cod)
                    {
                        // Bluetooth Class of Device (CoD) structure:
                        // Bits 0-1: Format Type
                        // Bits 2-7: Minor Class
                        // Bits 8-12: Major Class
                        // Bits 13-23: Service Class
                        
                        uint majorClass = (cod >> 8) & 0x1F;
                        // Major Class: 1 = Computer, 2 = Phone, 4 = Audio/Video
                        isPhoneOrComputer = (majorClass == 1 || majorClass == 2);
                    }
                }
                catch
                {
                    // Property access may fail, treat as not connected/not phone.
                }

                result.Add(new BluetoothDevice
                {
                    Name = d.Name,
                    Id = d.Id,
                    IsConnected = connected,
                    IsPhoneOrComputer = isPhoneOrComputer
                });
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

            Debug.WriteLine($"[ConnectBT] Finding device for {deviceId}...");
            
            var selector = AudioPlaybackConnection.GetDeviceSelector();
            var audioDevices = await DeviceInformation.FindAllAsync(selector);
            
            var bluetoothDevice = await Windows.Devices.Bluetooth.BluetoothDevice.FromIdAsync(deviceId);
            var selectedDeviceName = bluetoothDevice?.Name;
            
            var playbackDevice = audioDevices.FirstOrDefault(d => d.Name == selectedDeviceName);

            if (playbackDevice == null)
            {
                Debug.WriteLine($"[ConnectBT] Device not found in AudioPlaybackConnection list.");
                return false;
            }

            // Must execute on UI thread to interact with certain WinRT APIs in a WPF context
            _audioConnection = System.Windows.Application.Current.Dispatcher.Invoke(() => 
                AudioPlaybackConnection.TryCreateFromId(playbackDevice.Id));
            
            if (_audioConnection == null) return false;

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
                Debug.WriteLine($"[ConnectBT] Failed: {openResult.Status}");
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
