using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Owns a single native audio route so connection lifetime and state can be managed independently of WinRT.
/// </summary>
public interface IAudioPlaybackConnection : IDisposable
{
    /// <summary>Raised when Windows changes the underlying connection state.</summary>
    event EventHandler? StateChanged;

    /// <summary>Gets a value indicating whether Windows currently reports the audio route as open.</summary>
    bool IsOpen { get; }

    /// <summary>Enables incoming audio connections and waits for Windows to finish configuring this remote source.</summary>
    /// <returns>A task that completes after Windows has enabled the remote audio connection.</returns>
    Task StartAsync();

    /// <summary>Asks Windows to open the remote audio route.</summary>
    /// <returns>The native connection outcome translated into an application result.</returns>
    Task<AudioConnectionResult> OpenAsync();
}
