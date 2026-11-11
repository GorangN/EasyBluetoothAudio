using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace EasyBluetoothAudio.Services;

/// <summary>Writes flushed UTC connection events with one bounded previous log.</summary>
internal sealed class RollingConnectionTraceListener(string logPath) : TraceListener
{
    internal const long MaximumFileBytes = 1024 * 1024;
    private const int MaximumMessageCharacters = 16 * 1024;
    private readonly object _sync = new();
    private bool _disposed;

    /// <inheritdoc />
    public override bool IsThreadSafe
    {
        get { return true; }
    }

    /// <inheritdoc />
    public override void Write(string? message)
    {
        Append(message);
    }

    /// <inheritdoc />
    public override void WriteLine(string? message)
    {
        Append(message);
    }

    private void Append(string? message)
    {
        lock (_sync)
        {
            if (_disposed || message == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                if (message.Length > MaximumMessageCharacters)
                {
                    message = message[..MaximumMessageCharacters] + " [truncated]";
                }

                var timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                var bytes = Encoding.UTF8.GetBytes($"{timestamp} {message}{Environment.NewLine}");
                if (File.Exists(logPath) && new FileInfo(logPath).Length + bytes.Length > MaximumFileBytes)
                {
                    File.Move(logPath, logPath + ".1", overwrite: true);
                }

                using var stream = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                stream.Write(bytes);
                stream.Flush();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or SecurityException or ArgumentException or NotSupportedException)
            {
                // Diagnostics must never change connection outcomes when local storage is unavailable.
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_sync)
            {
                _disposed = true;
            }
        }
        base.Dispose(disposing);
    }
}
