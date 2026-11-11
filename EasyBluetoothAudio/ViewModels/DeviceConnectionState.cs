namespace EasyBluetoothAudio.ViewModels;

internal enum DeviceConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    RecoverableLoss,
    Failed
}
