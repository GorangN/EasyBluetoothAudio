using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Test double for <see cref="AudioService"/> that injects a deterministic selector snapshot.
/// </summary>
internal sealed class TestAudioService : AudioService
{
    private readonly IReadOnlyList<(string Id, string Name)> _connectedDevices;
    internal Exception? DiscoveryException { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAudioService"/> class.
    /// </summary>
    /// <param name="dispatcherService">The dispatcher service dependency required by the base class.</param>
    /// <param name="connectedDevices">The selector snapshot to expose to the test.</param>
    /// <param name="connectionFactory">An optional factory for controllable native audio routes.</param>
    internal TestAudioService(
        IDispatcherService dispatcherService,
        IReadOnlyList<(string Id, string Name)> connectedDevices,
        IAudioPlaybackConnectionFactory? connectionFactory = null)
        : base(dispatcherService, connectionFactory ?? Mock.Of<IAudioPlaybackConnectionFactory>())
    {
        _connectedDevices = connectedDevices;
    }

    /// <inheritdoc />
    internal override Task<IReadOnlyList<(string Id, string Name)>> GetConnectedAudioPlaybackDevicesAsync()
    {
        if (DiscoveryException != null)
        {
            return Task.FromException<IReadOnlyList<(string Id, string Name)>>(DiscoveryException);
        }

        return Task.FromResult(_connectedDevices);
    }

    /// <inheritdoc />
    internal override Task WaitForSettleAsync(int delayMilliseconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
