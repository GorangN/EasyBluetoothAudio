using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Services;

public partial class AudioService
{
    private (AudioConnectionEntry Entry, int Generation, int SettleDelay) BeginConnectionAttempt(string deviceId)
    {
        lock (_connectionHandoffSync)
        {
            AudioConnectionEntry entry;
            List<(string DeviceId, IAudioPlaybackConnection? Connection, EventHandler? Handler)> previous = [];
            int generation;
            int settleDelay;
            lock (_connectionSync)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                if (!_connections.TryGetValue(deviceId, out entry!))
                {
                    entry = new AudioConnectionEntry { LastDisconnectTime = _initialDisconnectTime };
                    _connections.Add(deviceId, entry);
                }

                foreach (var pair in _connections)
                {
                    pair.Value.Generation++;
                    var route = DetachConnectionCore(pair.Value,
                        preserveDisconnectTimestamp: ReferenceEquals(pair.Value, entry));
                    previous.Add((pair.Key, route.Connection, route.Handler));
                }

                generation = entry.Generation;
                var needsSettle = !entry.HasConnectedBefore
                    || (DateTime.UtcNow - entry.LastDisconnectTime).TotalMilliseconds < SettleDelayMs;
                settleDelay = needsSettle ? CalculateRemainingSettleDelay(entry.LastDisconnectTime, DateTime.UtcNow) : 0;
            }

            foreach (var route in previous)
            {
                DisposeAudioConnection(route.Connection, route.Handler, route.DeviceId, "pre-connect");
            }

            return (entry, generation, settleDelay);
        }
    }

    private bool IsCurrentAttempt(string deviceId, AudioConnectionEntry entry, int generation)
    {
        lock (_connectionSync)
        {
            return IsCurrentAttemptCore(deviceId, entry, generation);
        }
    }

    private bool IsCurrentAttemptCore(string deviceId, AudioConnectionEntry entry, int generation)
    {
        return !_isDisposed && _connections.TryGetValue(deviceId, out var current)
            && ReferenceEquals(current, entry) && entry.Generation == generation;
    }

    private bool TryRegisterConnection(
        string deviceId, AudioConnectionEntry entry, int generation,
        IAudioPlaybackConnection connection, EventHandler stateChangedHandler)
    {
        lock (_connectionSync)
        {
            if (!IsCurrentAttemptCore(deviceId, entry, generation))
            {
                return false;
            }

            entry.Connection = connection;
            entry.StateChangedHandler = stateChangedHandler;
            return true;
        }
    }

    private bool TryActivateConnection(
        string deviceId, AudioConnectionEntry entry, int generation, IAudioPlaybackConnection connection)
    {
        lock (_connectionSync)
        {
            if (!IsCurrentAttemptCore(deviceId, entry, generation) || !ReferenceEquals(entry.Connection, connection)
                || !connection.IsOpen)
            {
                return false;
            }

            entry.IsActive = true;
            entry.HasConnectedBefore = true;
            entry.LastDisconnectTime = DateTime.MinValue;
            return true;
        }
    }

    private void ReleaseIfCurrent(string deviceId, AudioConnectionEntry entry, int generation, string reason)
    {
        lock (_connectionHandoffSync)
        {
            (IAudioPlaybackConnection? Connection, EventHandler? Handler) detached;
            lock (_connectionSync)
            {
                if (!IsCurrentAttemptCore(deviceId, entry, generation))
                {
                    return;
                }

                entry.Generation++;
                detached = DetachConnectionCore(entry, preserveDisconnectTimestamp: false);
            }

            DisposeAudioConnection(detached.Connection, detached.Handler, deviceId, reason);
        }
    }

    private static (IAudioPlaybackConnection? Connection, EventHandler? Handler) DetachConnectionCore(
        AudioConnectionEntry entry, bool preserveDisconnectTimestamp)
    {
        var detached = (entry.Connection, entry.StateChangedHandler);
        if (entry.Connection != null && !preserveDisconnectTimestamp)
        {
            entry.LastDisconnectTime = DateTime.UtcNow;
        }

        entry.Connection = null;
        entry.StateChangedHandler = null;
        entry.IsActive = false;
        return detached;
    }

    private static void DisposeAudioConnection(
        IAudioPlaybackConnection? connection, EventHandler? handler, string deviceId, string reason)
    {
        if (connection == null)
        {
            return;
        }

        try
        {
            connection.StateChanged -= handler;
            connection.Dispose();
            AudioConnectionDiagnostics.Write(deviceId, "released", $"reason={reason}");
        }
        catch (Exception ex)
        {
            AudioConnectionDiagnostics.Write(deviceId, "release-failed", $"error={ex.GetType().Name}, hresult=0x{ex.HResult:X8}");
        }
    }

    /// <inheritdoc />
    public void Disconnect(string deviceId, string reason = "unspecified", bool preserveDisconnectTimestamp = false)
    {
        lock (_connectionHandoffSync)
        {
            (IAudioPlaybackConnection? Connection, EventHandler? Handler) detached;
            lock (_connectionSync)
            {
                if (!_connections.TryGetValue(deviceId, out var entry))
                {
                    return;
                }

                entry.Generation++;
                detached = DetachConnectionCore(entry, preserveDisconnectTimestamp);
            }

            DisposeAudioConnection(detached.Connection, detached.Handler, deviceId, reason);
        }
    }

    /// <inheritdoc />
    public void DisconnectAll(string reason = "unspecified")
    {
        ReleaseAllConnections(reason, disposing: false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ReleaseAllConnections("dispose", disposing: true);
        GC.SuppressFinalize(this);
    }

    private void ReleaseAllConnections(string reason, bool disposing)
    {
        lock (_connectionHandoffSync)
        {
            List<(string DeviceId, IAudioPlaybackConnection? Connection, EventHandler? Handler)> detached = [];
            lock (_connectionSync)
            {
                _isDisposed |= disposing;
                foreach (var pair in _connections)
                {
                    pair.Value.Generation++;
                    var route = DetachConnectionCore(pair.Value, preserveDisconnectTimestamp: false);
                    detached.Add((pair.Key, route.Connection, route.Handler));
                }
            }

            foreach (var route in detached)
            {
                DisposeAudioConnection(route.Connection, route.Handler, route.DeviceId, reason);
            }
        }
    }
}
