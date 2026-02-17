using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyBluetoothAudio.Services
{
    public interface IAudioService
    {
        Task<IEnumerable<BluetoothDeviceInfo>> GetBluetoothDevicesAsync();
        Task<bool> ConnectBluetoothAudioAsync(string deviceId);
        void StartRouting(string captureDeviceFriendlyName, int bufferMs);
        void StopRouting();
        bool IsRouting { get; }
    }

    public class BluetoothDeviceInfo
    {
        public string Name { get; set; } = "";
        public string Id { get; set; } = "";
        public bool IsConnected { get; set; }
    }
}
