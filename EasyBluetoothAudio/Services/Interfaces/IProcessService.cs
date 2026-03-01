namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Abstracts system process operations for testability.
/// </summary>
public interface IProcessService
{
    /// <summary>
    /// Opens the specified URI using the system's default handler.
    /// </summary>
    /// <param name="uri">The URI to open (e.g. a settings deep-link).</param>
    void OpenUri(string uri);
}
