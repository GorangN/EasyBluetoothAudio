using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Windows.Media.Audio;

namespace EasyBluetoothAudio.Services;

/// <summary>Adapts and releases one Windows Bluetooth audio playback connection.</summary>
internal sealed class NativeAudioPlaybackConnection : IAudioPlaybackConnection
{
    private readonly AudioPlaybackConnection _connection;
    private readonly string _deviceId;
    private int _isDisposed;

    internal NativeAudioPlaybackConnection(AudioPlaybackConnection connection, string deviceId)
    {
        _connection = connection;
        _deviceId = deviceId;
        _connection.StateChanged += OnStateChanged;
    }

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public bool IsOpen
    {
        get
        {
            return _connection.State == AudioPlaybackConnectionState.Opened;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        AudioConnectionDiagnostics.Write(_deviceId, "start", $"state={ReadStateForDiagnostics()}");
        await _connection.StartAsync();
        AudioConnectionDiagnostics.Write(_deviceId, "started", $"state={ReadStateForDiagnostics()}");
    }

    /// <inheritdoc />
    public async Task<AudioConnectionResult> OpenAsync()
    {
        AudioConnectionDiagnostics.Write(_deviceId, "open", $"state={ReadStateForDiagnostics()}");
        var result = await _connection.OpenAsync();
        var extendedError = result.ExtendedError?.HResult;
        AudioConnectionDiagnostics.Write(_deviceId, "open-result",
            $"status={result.Status}, hresult={(extendedError.HasValue ? $"0x{extendedError.Value:X8}" : "none")}, state={ReadStateForDiagnostics()}");
        return result.Status switch
        {
            AudioPlaybackConnectionOpenResultStatus.Success => AudioConnectionResult.Connected,
            AudioPlaybackConnectionOpenResultStatus.RequestTimedOut => AudioConnectionResult.TimedOut,
            AudioPlaybackConnectionOpenResultStatus.DeniedBySystem => AudioConnectionResult.DeniedBySystem,
            _ => AudioConnectionResult.Failed
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
        {
            return;
        }

        try
        {
            _connection.StateChanged -= OnStateChanged;
        }
        finally
        {
            _connection.Dispose();
        }
    }

    private void OnStateChanged(AudioPlaybackConnection sender, object args)
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            return;
        }

        try
        {
            if (!string.Equals(sender.DeviceId, _deviceId, StringComparison.OrdinalIgnoreCase))
            {
                AudioConnectionDiagnostics.Write(_deviceId, "foreign-native-state-event", "Ignored unrelated native device sender.");
                return;
            }

            AudioConnectionDiagnostics.Write(_deviceId, "state-changed", $"state={ReadStateForDiagnostics()}");
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            AudioConnectionDiagnostics.Write(_deviceId, "state-callback-failed",
                $"error={exception.GetType().Name}, hresult=0x{exception.HResult:X8}");
        }
    }

    private string ReadStateForDiagnostics()
    {
        try
        {
            return _connection.State.ToString();
        }
        catch (Exception exception)
        {
            return $"Unavailable(0x{exception.HResult:X8})";
        }
    }
}
