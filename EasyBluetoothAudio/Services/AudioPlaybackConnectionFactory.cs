using EasyBluetoothAudio.Services.Interfaces;
using Windows.Media.Audio;

namespace EasyBluetoothAudio.Services;

/// <summary>Creates WinRT audio connections without exposing native objects to application logic.</summary>
public sealed class AudioPlaybackConnectionFactory : IAudioPlaybackConnectionFactory
{
    /// <inheritdoc />
    public IAudioPlaybackConnection? TryCreate(string deviceId)
    {
        var connection = AudioPlaybackConnection.TryCreateFromId(deviceId);
        AudioConnectionDiagnostics.Write(deviceId, "create", $"available={connection != null}");
        return connection == null ? null : new NativeAudioPlaybackConnection(connection, deviceId);
    }
}
