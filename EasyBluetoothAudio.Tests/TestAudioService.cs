using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Test double for <see cref="AudioService"/> that injects a deterministic selector snapshot.
/// </summary>
internal sealed class TestAudioService : AudioService
{
    private readonly IReadOnlyList<(string Id, string Name)> _connectedDevices;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAudioService"/> class.
    /// </summary>
    /// <param name="dispatcherService">The dispatcher service dependency required by the base class.</param>
    /// <param name="connectedDevices">The selector snapshot to expose to the test.</param>
    internal TestAudioService(
        IDispatcherService dispatcherService,
        IReadOnlyList<(string Id, string Name)> connectedDevices)
        : base(dispatcherService)
    {
        _connectedDevices = connectedDevices;
    }

    /// <inheritdoc />
    internal override Task<IReadOnlyList<(string Id, string Name)>> GetConnectedAudioPlaybackDevicesAsync()
    {
        return Task.FromResult(_connectedDevices);
    }
}
