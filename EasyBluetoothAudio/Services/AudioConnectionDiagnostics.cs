using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace EasyBluetoothAudio.Services;

/// <summary>Correlates native audio events without writing remote device identifiers or names to logs.</summary>
internal static class AudioConnectionDiagnostics
{
    internal static void Write(string deviceId, string stage, string details)
    {
        var deviceHash = SHA256.HashData(Encoding.UTF8.GetBytes(deviceId.ToUpperInvariant()));
        var deviceTag = Convert.ToHexString(deviceHash.AsSpan(0, 6));
        Trace.WriteLine($"[BluetoothAudio] source={deviceTag}, stage={stage}, {details}");
    }
}
