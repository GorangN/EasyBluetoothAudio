using System.Diagnostics;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Default implementation that opens URIs via the operating system shell.
/// </summary>
public class ProcessService : IProcessService
{
    /// <inheritdoc />
    public void OpenUri(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}
