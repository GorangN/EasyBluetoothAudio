using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace EasyBluetoothAudio.Services
{
    public class AudioService : IAudioService, IDisposable
    {
        private AudioPlaybackConnection? _audioConnection;

        public bool IsRouting { get; private set; }

        public async Task<IEnumerable<BluetoothDeviceInfo>> GetBluetoothDevicesAsync()
        {
            var result = new List<BluetoothDeviceInfo>();
            try
            {
                var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
                var devices = await DeviceInformation.FindAllAsync(selector);
                foreach (var d in devices)
                {
                    bool connected = false;
                    try
                    {
                        if (d.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
                            connected = (bool)(d.Properties["System.Devices.Aep.IsConnected"] ?? false);
                    }
                    catch { }
                    result.Add(new BluetoothDeviceInfo { Name = d.Name, Id = d.Id, IsConnected = connected });
                }
            }
            catch (Exception ex) { Debug.WriteLine($"[GetBluetoothDevices] Error: {ex.Message}"); }
            return result;
        }

        public async Task<bool> ConnectBluetoothAudioAsync(string deviceId)
        {
            try
            {
                _audioConnection?.Dispose();
                _audioConnection = null;

                Debug.WriteLine($"[ConnectBT] Finding device for {deviceId}...");
                var selector = AudioPlaybackConnection.GetDeviceSelector();
                var audioDevices = await DeviceInformation.FindAllAsync(selector);
                var selectedDeviceName = (await BluetoothDevice.FromIdAsync(deviceId))?.Name;
                var playbackDevice = audioDevices.FirstOrDefault(d => d.Name == selectedDeviceName);

                if (playbackDevice == null)
                {
                    Debug.WriteLine($"[ConnectBT] Device not found in AudioPlaybackConnection list.");
                    return false;
                }

                _audioConnection = System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    AudioPlaybackConnection.TryCreateFromId(playbackDevice.Id));
                
                if (_audioConnection == null) return false;

                _audioConnection.Start();
                var openResult = await _audioConnection.OpenAsync();

                if (openResult.Status == AudioPlaybackConnectionOpenResultStatus.Success)
                {
                    Debug.WriteLine("[ConnectBT] AudioPlaybackConnection Success!");
                    // With this API, Windows handles the audio routing automatically to the default speaker.
                    // We don't need to manually capture/render anything.
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

        public Task StartRoutingAsync(string captureDeviceFriendlyName, int bufferMs)
        {
            // This method is now effectively a placeholder because ConnectBluetoothAudioAsync
            // handles the entire flow. We keep the signature to satisfy the interface/ViewModel,
            // or we could refactor the ViewModel to not call this.
            // For now, let's just ensure IsRouting is true.
            
            IsRouting = true;
            return Task.CompletedTask;
        }

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

        public void Dispose()
        {
            StopRouting();
        }
    }
}
