using System;
using System.Diagnostics;
using EasyBluetoothAudio.Services.Interfaces;

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
        if (IsValidUri(uri, out var parsedUri) && parsedUri != null)
        {
            Process.Start(new ProcessStartInfo(parsedUri.AbsoluteUri) { UseShellExecute = true });
        }
    }

    internal bool IsValidUri(string uri)
    {
        return IsValidUri(uri, out _);
    }

    private bool IsValidUri(string uri, out Uri? parsedUri)
    {
        parsedUri = null;

        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out parsedUri))
        {
            return false;
        }

        return AllowedSchemes.Contains(parsedUri.Scheme.ToLowerInvariant());
    }
}
