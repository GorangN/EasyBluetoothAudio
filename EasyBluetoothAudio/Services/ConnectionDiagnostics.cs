using System.Diagnostics;
using System.IO;
using System.Reflection;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Services;

/// <summary>Registers a persistent, bounded connection trace for the application session.</summary>
public sealed class ConnectionDiagnostics : IConnectionDiagnostics
{
    private readonly object _sync = new();
    private RollingConnectionTraceListener? _listener;
    private bool _disposed;

    /// <summary>Creates diagnostics under the current user's local application-data directory.</summary>
    public ConnectionDiagnostics()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EasyBluetoothAudio", "Logs"))
    {
    }

    /// <summary>Creates diagnostics in an explicitly supplied directory for isolated hosts or tests.</summary>
    /// <param name="directory">The directory containing the current and previous connection logs.</param>
    /// <exception cref="ArgumentNullException">The directory is null.</exception>
    public ConnectionDiagnostics(string directory)
    {
        LogPath = Path.Combine(directory, "connections.log");
    }

    /// <inheritdoc />
    public string LogPath { get; }

    /// <inheritdoc />
    public void Initialize()
    {
        lock (_sync)
        {
            if (_disposed || _listener != null)
            {
                return;
            }

            _listener = new RollingConnectionTraceListener(LogPath);
            var version = typeof(ConnectionDiagnostics).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            _listener.WriteLine($"session-start os={Environment.OSVersion.VersionString} process={Environment.ProcessId} app={version}");
            Trace.Listeners.Add(_listener);
        }
    }

    /// <summary>Removes the listener and stops further file writes; repeated disposal is harmless.</summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_listener != null)
            {
                Trace.Listeners.Remove(_listener);
                _listener.Dispose();
                _listener = null;
            }
        }
    }
}
