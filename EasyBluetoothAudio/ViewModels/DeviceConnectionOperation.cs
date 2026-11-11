namespace EasyBluetoothAudio.ViewModels;

internal sealed class DeviceConnectionOperation : IDisposable
{
    internal const int AutomaticRecoveryLimit = 2;
    internal static readonly TimeSpan AutomaticRecoveryWindow = TimeSpan.FromSeconds(60);
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly TimeProvider _timeProvider;
    private readonly Queue<long> _recoveryTimestamps = new();

    internal DeviceConnectionOperation(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        CancellationToken = _cancellationSource.Token;
    }

    internal CancellationToken CancellationToken { get; }

    internal int RecoveryInFlight;

    internal bool TryBeginAutomaticRecovery()
    {
        var now = _timeProvider.GetTimestamp();
        while (_recoveryTimestamps.TryPeek(out var timestamp)
            && _timeProvider.GetElapsedTime(timestamp, now) >= AutomaticRecoveryWindow)
        {
            _recoveryTimestamps.Dequeue();
        }

        if (_recoveryTimestamps.Count >= AutomaticRecoveryLimit)
        {
            return false;
        }

        _recoveryTimestamps.Enqueue(now);
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();
    }
}
