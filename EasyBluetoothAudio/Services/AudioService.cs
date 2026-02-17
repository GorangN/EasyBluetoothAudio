using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace EasyBluetoothAudio.Services
{
    public class AudioService : IAudioService, IDisposable
    {
        private WasapiCapture? _capture;
        private WasapiOut? _output;
        private BufferedWaveProvider? _buffer;

        public bool IsRouting { get; private set; }

        public async Task<IEnumerable<BluetoothDeviceInfo>> GetBluetoothDevicesAsync()
        {
            // Find paired Bluetooth devices
            var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(selector);

            return devices.Select(d => new BluetoothDeviceInfo
            {
                Name = d.Name,
                Id = d.Id,
                IsConnected = (bool?)d.Properties["System.Devices.Connected"] ?? false
            });
        }

        public async Task<bool> ConnectBluetoothAudioAsync(string deviceId)
        {
            try
            {
                var device = await BluetoothDevice.FromIdAsync(deviceId);
                if (device == null) return false;

                // Getting the A2DP Sink service is a bit more complex in WinRT
                // But usually, Windows 10/11 handles the Sink connection when we interact with the device
                // We'll attempt to connect via the device's audio services if applicable
                // For now, let's assume the user just needs us to find the Line-In associated with this device
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void StartRouting(string captureDeviceFriendlyName, int bufferMs)
        {
            if (IsRouting) StopRouting();

            var enumerator = new MMDeviceEnumerator();
            var inputDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                                        .FirstOrDefault(d => d.FriendlyName.Contains(captureDeviceFriendlyName, StringComparison.OrdinalIgnoreCase));

            if (inputDevice == null)
            {
                // Fallback: list all to help debugging
                var all = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).Select(d => d.FriendlyName);
                throw new Exception($"Capture device '{captureDeviceFriendlyName}' not found. Available: {string.Join(", ", all)}");
            }

            _capture = new WasapiCapture(inputDevice, false, bufferMs);
            _output = new WasapiOut(AudioClientShareMode.Shared, bufferMs);
            
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
        }

        public void StopRouting()
        {
            _capture?.StopRecording();
            _output?.Stop();
            
            _capture?.Dispose();
            _output?.Dispose();

            _capture = null;
            _output = null;
            IsRouting = false;
        }

        public void Dispose()
        {
            StopRouting();
        }
    }
}
