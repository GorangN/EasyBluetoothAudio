using System.Diagnostics;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Default implementation of <see cref="IProcessService"/> that uses the OS shell.
/// </summary>
public class ProcessService : IProcessService
{
    /// <inheritdoc />
    public void OpenUri(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }
}
