using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Services;

/// <summary>Tracks ownership and real-disconnect timing for one remote device.</summary>
internal sealed class AudioConnectionEntry
{
    internal IAudioPlaybackConnection? Connection { get; set; }
    internal EventHandler? StateChangedHandler { get; set; }
    internal int Generation { get; set; }
    internal bool IsActive { get; set; }
    internal bool HasConnectedBefore { get; set; }
    internal DateTime LastDisconnectTime { get; set; }
}
