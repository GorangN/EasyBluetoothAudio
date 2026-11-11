namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>Creates independently owned native Bluetooth audio routes for remote device identifiers.</summary>
public interface IAudioPlaybackConnectionFactory
{
    /// <summary>Attempts to create an audio route for the specified remote device.</summary>
    /// <param name="deviceId">The identifier returned by the Windows remote-audio device selector.</param>
    /// <returns>A new owned route, or <see langword="null"/> when Windows cannot create one.</returns>
    IAudioPlaybackConnection? TryCreate(string deviceId);
}
