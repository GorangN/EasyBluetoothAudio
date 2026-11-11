using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Receives remote Bluetooth audio through one Windows A2DP Sink route at a time.
/// Windows performs audio routing after the connection opens.
/// </summary>
public partial class AudioService : IAudioService
{
    /// <summary>The existing release window between enabling and opening a recently disconnected route.</summary>
    internal const int SettleDelayMs = 5_000;

    private readonly IDispatcherService _dispatcherService;
    private readonly IAudioPlaybackConnectionFactory _connectionFactory;
    private readonly object _connectionHandoffSync = new();
    private readonly object _connectionSync = new();
    private readonly Dictionary<string, AudioConnectionEntry> _connections = new(StringComparer.OrdinalIgnoreCase);
    private readonly DateTime _initialDisconnectTime = DateTime.UtcNow;
    private bool _isDisposed;

    /// <summary>Initializes exclusive Bluetooth audio ownership and its native connection factory.</summary>
    /// <param name="dispatcherService">Dispatches native route creation and opening to the UI thread.</param>
    /// <param name="connectionFactory">Creates an independently owned native connection for each attempt.</param>
    public AudioService(IDispatcherService dispatcherService, IAudioPlaybackConnectionFactory connectionFactory)
    {
        _dispatcherService = dispatcherService;
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public event EventHandler<DeviceConnectionLostEventArgs>? ConnectionLost;

    /// <summary>Enumerates the authoritative Windows selector snapshot without relying on AEP connectivity flags.</summary>
    /// <returns>The available remote audio device identifiers and display names.</returns>
    internal virtual async Task<IReadOnlyList<(string Id, string Name)>> GetConnectedAudioPlaybackDevicesAsync()
    {
        var devices = await DeviceInformation.FindAllAsync(AudioPlaybackConnection.GetDeviceSelector());
        return devices.Select(device => (device.Id, device.Name)).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BluetoothDevice>> GetBluetoothDevicesAsync()
    {
        var devices = await GetConnectedAudioPlaybackDevicesAsync();
        return devices.Select(device => new BluetoothDevice
        {
            Id = device.Id,
            Name = device.Name,
            IsConnected = true,
            IsPhoneOrComputer = true
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<AudioConnectionResult> ConnectBluetoothAudioAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (entry, generation, settleDelay) = BeginConnectionAttempt(deviceId);
        AudioConnectionDiagnostics.Write(deviceId, "attempt", $"generation={generation}, settleMs={settleDelay}");
        using var cancellationRegistration = cancellationToken.Register(() =>
            ReleaseIfCurrent(deviceId, entry, generation, "cancelled"));
        IAudioPlaybackConnection? connection = null;
        EventHandler? stateChangedHandler = null;
        var registered = false;
        var result = AudioConnectionResult.Unavailable;

        try
        {
            await _dispatcherService.InvokeAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsCurrentAttempt(deviceId, entry, generation))
                {
                    return;
                }

                var nativeConnection = _connectionFactory.TryCreate(deviceId);
                if (nativeConnection == null)
                {
                    return;
                }

                connection = nativeConnection;
                stateChangedHandler = (sender, _) =>
                {
                    if (!ReferenceEquals(sender, nativeConnection))
                    {
                        AudioConnectionDiagnostics.Write(deviceId, "foreign-state-event", "Ignored unrelated adapter sender.");
                        return;
                    }

                    OnAudioConnectionStateChanged(deviceId, entry, generation, nativeConnection);
                };
                nativeConnection.StateChanged += stateChangedHandler;
                registered = TryRegisterConnection(deviceId, entry, generation, nativeConnection, stateChangedHandler);
                if (!registered)
                {
                    return;
                }

                await nativeConnection.StartAsync().WaitAsync(cancellationToken);
                if (settleDelay > 0)
                {
                    await WaitForSettleAsync(settleDelay, cancellationToken);
                }

                if (!IsCurrentAttempt(deviceId, entry, generation))
                {
                    return;
                }

                result = await nativeConnection.OpenAsync().WaitAsync(cancellationToken);
            });

            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentAttempt(deviceId, entry, generation))
            {
                return AudioConnectionResult.Failed;
            }

            if (result == AudioConnectionResult.Connected && connection != null
                && TryActivateConnection(deviceId, entry, generation, connection))
            {
                AudioConnectionDiagnostics.Write(deviceId, "connected", "state=Opened");
                return AudioConnectionResult.Connected;
            }

            if (result == AudioConnectionResult.Connected)
            {
                AudioConnectionDiagnostics.Write(deviceId, "open-rejected", "Native request succeeded, but the current route is closed.");
                result = AudioConnectionResult.Failed;
            }

            ReleaseIfCurrent(deviceId, entry, generation, $"open-{result}");
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ReleaseIfCurrent(deviceId, entry, generation, "cancelled");
            throw;
        }
        catch (Exception ex)
        {
            AudioConnectionDiagnostics.Write(deviceId, "attempt-failed", $"error={ex.GetType().Name}, hresult=0x{ex.HResult:X8}");
            ReleaseIfCurrent(deviceId, entry, generation, "connect-exception");
            cancellationToken.ThrowIfCancellationRequested();
            return AudioConnectionResult.Failed;
        }
        finally
        {
            if (connection != null && !registered)
            {
                DisposeAudioConnection(connection, stateChangedHandler, deviceId, "superseded-before-registration");
            }
        }
    }

    /// <summary>Waits for the existing Windows route-release window before native opening.</summary>
    /// <param name="delayMilliseconds">The remaining settle window in milliseconds.</param>
    /// <param name="cancellationToken">Cancels this attempt's wait.</param>
    /// <returns>A task that completes after the settle window or throws on cancellation.</returns>
    internal virtual Task WaitForSettleAsync(int delayMilliseconds, CancellationToken cancellationToken)
    {
        return Task.Delay(delayMilliseconds, cancellationToken);
    }

    /// <summary>Calculates the remaining release window after a real disconnect.</summary>
    /// <param name="lastDisconnectTime">The time of this device's last real disconnect.</param>
    /// <param name="currentTime">The current time used for the calculation.</param>
    /// <returns>The remaining milliseconds, clamped to the existing settle window.</returns>
    internal static int CalculateRemainingSettleDelay(DateTime lastDisconnectTime, DateTime currentTime)
    {
        var elapsedMilliseconds = Math.Max(0, (currentTime - lastDisconnectTime).TotalMilliseconds);
        return (int)Math.Clamp(SettleDelayMs - elapsedMilliseconds, 0, SettleDelayMs);
    }

    /// <inheritdoc />
    public Task<bool> IsBluetoothDeviceConnectedAsync(string deviceId)
    {
        return Task.FromResult(IsDeviceConnected(deviceId));
    }

    private bool IsDeviceConnected(string deviceId)
    {
        try
        {
            IAudioPlaybackConnection? connection;
            lock (_connectionSync)
            {
                connection = _connections.TryGetValue(deviceId, out var entry) && entry.IsActive
                    ? entry.Connection : null;
            }

            return connection?.IsOpen == true;
        }
        catch (Exception ex)
        {
            AudioConnectionDiagnostics.Write(deviceId, "state-query-failed", $"error={ex.GetType().Name}, hresult=0x{ex.HResult:X8}");
            return false;
        }
    }

    /// <inheritdoc />
    public bool HasOtherAudioConnections(string deviceId)
    {
        lock (_connectionSync)
        {
            return !_isDisposed && _connections.Any(pair => pair.Value.Connection != null
                && !string.Equals(pair.Key, deviceId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsBluetoothPhysicallyConnectedAsync(string deviceId)
    {
        if (await IsBluetoothDeviceConnectedAsync(deviceId))
        {
            return true;
        }

        try
        {
            var devices = await GetConnectedAudioPlaybackDevicesAsync();
            return devices.Any(device => string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            AudioConnectionDiagnostics.Write(deviceId, "selector-failed", $"error={ex.GetType().Name}, hresult=0x{ex.HResult:X8}");
            return false;
        }
    }

    private void OnAudioConnectionStateChanged(
        string deviceId, AudioConnectionEntry entry, int generation, IAudioPlaybackConnection connection)
    {
        try
        {
            lock (_connectionSync)
            {
                if (!IsCurrentAttemptCore(deviceId, entry, generation)
                    || !entry.IsActive || !ReferenceEquals(entry.Connection, connection))
                {
                    return;
                }
            }

            if (!connection.IsOpen && IsCurrentAttempt(deviceId, entry, generation))
            {
                AudioConnectionDiagnostics.Write(deviceId, "connection-lost", $"generation={generation}, state=Closed");
                ConnectionLost?.Invoke(this, new DeviceConnectionLostEventArgs(deviceId));
            }
        }
        catch (Exception ex)
        {
            AudioConnectionDiagnostics.Write(deviceId, "state-event-failed", $"error={ex.GetType().Name}, hresult=0x{ex.HResult:X8}");
        }
    }
}
