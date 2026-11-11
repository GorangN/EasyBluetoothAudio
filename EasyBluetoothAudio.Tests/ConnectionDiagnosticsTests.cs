using System.Diagnostics;
using System.IO;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Tests;

/// <summary>Checks bounded local connection traces without touching personal configuration directories.</summary>
[Collection("Connection diagnostics")]
public class ConnectionDiagnosticsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"EBA-diagnostics-{Guid.NewGuid():N}");

    /// <summary>Registers once, flushes UTC session data, and removes its listener on disposal.</summary>
    [Fact]
    public void Initialize_RegistersOnceAndFlushesSessionAndEvents()
    {
        var listenerCount = Trace.Listeners.Count;
        using IConnectionDiagnostics diagnostics = new ConnectionDiagnostics(_directory);
        diagnostics.Initialize();
        diagnostics.Initialize();
        Assert.Equal(listenerCount + 1, Trace.Listeners.Count);
        Trace.WriteLine("device=ABC123 stage=open status=connected");
        var contents = File.ReadAllText(diagnostics.LogPath);
        Assert.Contains("session-start os=", contents);
        Assert.Contains("process=", contents);
        Assert.Contains("app=", contents);
        Assert.Contains("device=ABC123 stage=open status=connected", contents);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z ", contents);
        diagnostics.Dispose();
        diagnostics.Dispose();
        diagnostics.Initialize();
        Assert.Equal(listenerCount, Trace.Listeners.Count);
        Trace.WriteLine("after-disposal");
        Assert.DoesNotContain("after-disposal", File.ReadAllText(diagnostics.LogPath));
    }

    /// <summary>Repeated rollover preserves only the current and one previous file, each within the byte limit.</summary>
    [Fact]
    public void Listener_RollsAtOneMiBAndKeepsOnePreviousFile()
    {
        var path = Path.Combine(_directory, "connections.log");
        using var listener = new RollingConnectionTraceListener(path);
        for (var index = 0; index < 200; index++)
        {
            listener.WriteLine(new string('x', 16_000) + $" record={index}");
        }
        var files = Directory.GetFiles(_directory);
        Assert.Equal(2, files.Length);
        Assert.Contains(path, files);
        Assert.Contains(path + ".1", files);
        Assert.All(files, file => Assert.InRange(new FileInfo(file).Length, 1, RollingConnectionTraceListener.MaximumFileBytes));
        Assert.Contains("record=199", File.ReadAllText(path));
    }

    /// <summary>An invalid directory cannot interrupt startup, trace callers, or repeated shutdown.</summary>
    [Fact]
    public void UnwritableDirectory_DoesNotThrowAndStillUnregisters()
    {
        Directory.CreateDirectory(_directory);
        var blocked = Path.Combine(_directory, "file-not-directory");
        File.WriteAllText(blocked, "occupied");
        var listenerCount = Trace.Listeners.Count;
        using var diagnostics = new ConnectionDiagnostics(blocked);
        diagnostics.Initialize();
        Trace.WriteLine("storage-unavailable");
        diagnostics.Dispose();
        diagnostics.Dispose();
        Assert.Equal(listenerCount, Trace.Listeners.Count);
        Assert.False(File.Exists(diagnostics.LogPath));
    }

    /// <summary>Concurrent events remain whole UTC-prefixed records with no writer exceptions.</summary>
    [Fact]
    public void Listener_ConcurrentWritesRemainReadableAndFlushed()
    {
        var path = Path.Combine(_directory, "connections.log");
        using var listener = new RollingConnectionTraceListener(path);
        Parallel.For(0, 100, index => listener.WriteLine($"event={index}"));
        var lines = File.ReadAllLines(path);
        Assert.Equal(100, lines.Length);
        Assert.All(lines, line => Assert.Matches(@"^\d{4}-\d{2}-\d{2}T.*Z event=\d+$", line));
    }

    /// <summary>Removes only this test's isolated temporary log directory.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
