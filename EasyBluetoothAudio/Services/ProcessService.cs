using System;
using System.Diagnostics;
using System.Linq;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Default implementation that opens URIs via the operating system shell.
/// </summary>
public class ProcessService : IProcessService
{
    private static readonly string[] AllowedSchemes = { "http", "https", "mailto", "ms-settings" };

    /// <inheritdoc />
    public void OpenUri(string uri)
    {
        if (IsValidUri(uri))
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
    }

    internal bool IsValidUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
        {
            return false;
        }

        return AllowedSchemes.Contains(parsedUri.Scheme.ToLowerInvariant());
    }
}
