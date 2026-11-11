namespace EasyBluetoothAudio.ViewModels;

internal sealed class DeviceConnectionMonitorRegistration : IDisposable
{
    private readonly CancellationTokenSource _cancellationSource;

    internal DeviceConnectionMonitorRegistration(CancellationToken operationToken, Func<string, Task> recover)
    {
        _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(operationToken);
        CancellationToken = _cancellationSource.Token;
        Recover = recover;
    }

    internal CancellationToken CancellationToken { get; }
    internal Func<string, Task> Recover { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();
    }
}
