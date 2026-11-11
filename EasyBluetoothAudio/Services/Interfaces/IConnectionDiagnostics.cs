namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>Owns a bounded, best-effort local connection trace without exposing hardware identifiers.</summary>
public interface IConnectionDiagnostics : IDisposable
{
    /// <summary>Gets the current connection log path for troubleshooting.</summary>
    string LogPath { get; }

    /// <summary>Registers the persistent trace listener once for this service lifetime.</summary>
    void Initialize();
}
