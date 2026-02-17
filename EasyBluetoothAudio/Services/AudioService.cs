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
        private WasapiCapture? _capture;
        private WasapiOut? _output;
        private BufferedWaveProvider? _buffer;
        private AudioPlaybackConnection? _audioConnection;

        public bool IsRouting { get; private set; }

        public async Task<IEnumerable<BluetoothDeviceInfo>> GetBluetoothDevicesAsync()
        {
            var result = new List<BluetoothDeviceInfo>();

            try
            {
                // Find all paired Bluetooth devices
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
                    catch { /* property not available, ignore */ }

                    result.Add(new BluetoothDeviceInfo
                    {
                        Name = d.Name,
                        Id = d.Id,
                        IsConnected = connected
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GetBluetoothDevices] Error: {ex.Message}");
            }

            return result;
        }

        public async Task<bool> ConnectBluetoothAudioAsync(string deviceId)
        {
            try
            {
                // Clean up any previous connection
                _audioConnection?.Dispose();
                _audioConnection = null;

                Debug.WriteLine($"[ConnectBT] finding device via AudioPlaybackConnection selector for {deviceId}...");
                
                // Get the selector for AudioPlaybackConnection
                var selector = AudioPlaybackConnection.GetDeviceSelector();
                var audioDevices = await DeviceInformation.FindAllAsync(selector);

                // Find the device that matches our selected Bluetooth address or name
                // The 'deviceId' passed in is likely the BluetoothDevice Id. 
                // We need to match it to the AudioPlaybackConnection device list.
                // A reliable way is likely name or checking if the ID contains the address.
                
                // Let's try matching by Name first as it's simplest for now
                // (In a prod app we'd parse the address from the ID)
                var selectedDeviceName = (await BluetoothDevice.FromIdAsync(deviceId))?.Name;
                
                var playbackDevice = audioDevices.FirstOrDefault(d => d.Name == selectedDeviceName);

                if (playbackDevice == null)
                {
                    Debug.WriteLine($"[ConnectBT] Could not find AudioPlaybackConnection compatible device for '{selectedDeviceName}'");
                    return false;
                }

                Debug.WriteLine($"[ConnectBT] Found AudioPlaybackConnection device: {playbackDevice.Name} (ID: {playbackDevice.Id})");

                // 2. Create AudioPlaybackConnection using the ID from the specific selector
                // MUST run on UI thread (STA) to avoid WinRT InvalidCastException? (Keeping it just in case)
                _audioConnection = System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    AudioPlaybackConnection.TryCreateFromId(playbackDevice.Id));
                
                if (_audioConnection == null)
                {
                    Debug.WriteLine("[ConnectBT] AudioPlaybackConnection.TryCreateFromId returned null.");
                    return false;
                }

                // 2. Start the connection
                Debug.WriteLine("[ConnectBT] Starting AudioPlaybackConnection...");
                _audioConnection.Start();

                // 3. Open the connection
                // This is the critical step that usually triggers the "Connected" state on the phone
                // and makes the audio endpoint appear on Windows.
                Debug.WriteLine("[ConnectBT] Opening AudioPlaybackConnection...");
                var openResult = await _audioConnection.OpenAsync();
                
                Debug.WriteLine($"[ConnectBT] OpenAsync result: {openResult.Status}");

                if (openResult.Status == AudioPlaybackConnectionOpenResultStatus.Success)
                {
                    Debug.WriteLine("[ConnectBT] Connection established successfully!");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"[ConnectBT] Connection failed. Status: {openResult.Status}, ExtendedError: {openResult.ExtendedError}");
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

        public async Task StartRoutingAsync(string captureDeviceFriendlyName, int bufferMs)
        {
            if (IsRouting) StopRouting();

            var enumerator = new MMDeviceEnumerator();
            Debug.WriteLine($"[StartRouting] Looking for device containing '{captureDeviceFriendlyName}'");
            
            MMDevice? inputDevice = null;
            var sw = Stopwatch.StartNew();

            // Retry loop: wait up to 15 seconds for the audio endpoint to appear
            // (Increased to 15s as AudioPlaybackConnection might take a moment to register)
            while (sw.ElapsedMilliseconds < 15000)
            {
                var allCapture = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
                
                // 1. Try exact match (or contains)
                inputDevice = allCapture.FirstOrDefault(d => 
                    d.FriendlyName.IndexOf(captureDeviceFriendlyName, StringComparison.OrdinalIgnoreCase) >= 0);

                // 2. Fallback: try "Bluetooth" if not found
                if (inputDevice == null)
                {
                    inputDevice = allCapture.FirstOrDefault(d => 
                        d.FriendlyName.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) &&
                        d.FriendlyName.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) == false); // Try to avoid HFP
                }

                if (inputDevice != null) break;

                if (sw.ElapsedMilliseconds % 1000 < 100)
                    Debug.WriteLine($"[StartRouting] Waiting for device... ({sw.ElapsedMilliseconds}ms)");
                
                await Task.Delay(500); 
            }

            if (inputDevice == null)
            {
                var all = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                var names = string.Join(", ", all.Select(d => d.FriendlyName));
                throw new Exception($"Audio device not found after 15s. Available: [{names}]");
            }

            Debug.WriteLine($"[StartRouting] Found capture device: '{inputDevice.FriendlyName}'");

            // ────── LATENCY OPTIMIZATION ──────
            _capture = new WasapiCapture(inputDevice, false, bufferMs);

            var defaultOutput = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            _output = new WasapiOut(defaultOutput, AudioClientShareMode.Shared, false, bufferMs);

            _buffer = new BufferedWaveProvider(_capture.WaveFormat)
            {
                DiscardOnBufferOverflow = true,
                BufferDuration = TimeSpan.FromMilliseconds(bufferMs * 4) 
            };

            _capture.DataAvailable += (s, e) =>
            {
                _buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            _output.Init(_buffer);
            _capture.StartRecording();
            _output.Play();

            IsRouting = true;
            Debug.WriteLine("[StartRouting] Audio routing started.");
        }

        public void StopRouting()
        {
            try { _capture?.StopRecording(); } catch { }
            try { _output?.Stop(); } catch { }

            _capture?.Dispose();
            _output?.Dispose();

            // Also close the Bluetooth connection
            if (_audioConnection != null)
            {
                Debug.WriteLine("[StopRouting] Closing AudioPlaybackConnection...");
                _audioConnection.Dispose();
                _audioConnection = null;
            }

            _capture = null;
            _output = null;
            _buffer = null;
            IsRouting = false;

            Debug.WriteLine("[StopRouting] Audio routing stopped.");
        }

        public void Dispose()
        {
            StopRouting();
        }
    }
}
