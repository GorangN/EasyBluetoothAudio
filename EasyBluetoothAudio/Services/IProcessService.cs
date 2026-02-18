namespace EasyBluetoothAudio.Services;

/// <summary>
/// Abstracts launching system URIs and external processes, enabling testability.
/// </summary>
public interface IProcessService
{
    /// <summary>
    /// Opens the specified URI using the default system handler (e.g. a settings page or URL).
    /// </summary>
    /// <param name="uri">The URI to open.</param>
    void OpenUri(string uri);
}
