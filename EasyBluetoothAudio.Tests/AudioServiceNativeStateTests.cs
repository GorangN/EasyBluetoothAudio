using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>Verifies native connection outcomes agree with current route state.</summary>
public class AudioServiceNativeStateTests
{
    /// <summary>A successful native request cannot mark an already closed route as connected.</summary>
    [Fact]
    public async Task OpenSuccess_WithClosedNativeState_ReturnsFailedAndReleasesRoute()
    {
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(service => service.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());
        var route = new Mock<IAudioPlaybackConnection>();
        route.SetupGet(connection => connection.IsOpen).Returns(false);
        route.Setup(connection => connection.StartAsync()).Returns(Task.CompletedTask);
        route.Setup(connection => connection.OpenAsync()).ReturnsAsync(AudioConnectionResult.Connected);
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(route.Object);
        using var service = new TestAudioService(dispatcher.Object, [], factory.Object);

        var result = await service.ConnectBluetoothAudioAsync("A");

        Assert.Equal(AudioConnectionResult.Failed, result);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("A"));
        route.Verify(connection => connection.Dispose(), Times.Once);
    }

    private static TestAudioService CreateService(Mock<IAudioPlaybackConnectionFactory> factory)
    {
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(service => service.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());
        return new TestAudioService(dispatcher.Object, [], factory.Object);
    }

    /// <summary>Opening a native route waits for asynchronous Windows sink configuration to finish.</summary>
    [Fact]
    public async Task Connect_WaitsForStartCompletionBeforeOpen()
    {
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var route = new FakeAudioPlaybackConnection { PendingStart = start };
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(route);
        using var service = CreateService(factory);
        var attempt = service.ConnectBluetoothAudioAsync("A");
        await route.StartStarted.Task;

        Assert.False(route.OpenStarted.Task.IsCompleted);
        Assert.False(attempt.IsCompleted);
        start.SetResult(true);
        Assert.Equal(AudioConnectionResult.Connected, await attempt);
    }

    /// <summary>Cancellation during native enabling releases the route and prevents a late open.</summary>
    [Fact]
    public async Task CancelDuringStart_ReleasesRouteWithoutOpening()
    {
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var route = new FakeAudioPlaybackConnection { PendingStart = start };
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(route);
        using var service = CreateService(factory);
        using var cancellation = new CancellationTokenSource();
        var attempt = service.ConnectBluetoothAudioAsync("A", cancellation.Token);
        await route.StartStarted.Task;

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attempt.WaitAsync(TimeSpan.FromSeconds(2)));
        start.SetResult(true);
        Assert.False(route.OpenStarted.Task.IsCompleted);
        Assert.Equal(1, route.DisposeCount);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("A"));
    }

    /// <summary>A device-scoped disconnect while Windows enables the route discards that attempt's late completion.</summary>
    [Fact]
    public async Task DisconnectDuringStart_DoesNotOpenSupersededRoute()
    {
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var route = new FakeAudioPlaybackConnection { PendingStart = start };
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(route);
        using var service = CreateService(factory);
        var attempt = service.ConnectBluetoothAudioAsync("A");
        await route.StartStarted.Task;

        service.Disconnect("A");
        start.SetResult(true);

        Assert.Equal(AudioConnectionResult.Failed, await attempt);
        Assert.False(route.OpenStarted.Task.IsCompleted);
        Assert.Equal(1, route.DisposeCount);
    }

    /// <summary>Switching devices while Windows enables the old route prevents its late native open.</summary>
    /// <returns>A task completing after switching and discarding the previous native start.</returns>
    [Fact]
    public async Task SwitchDuringStart_DoesNotOpenPreviousDeviceAfterReplacement()
    {
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var previous = new FakeAudioPlaybackConnection { PendingStart = start };
        var replacement = new FakeAudioPlaybackConnection();
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(previous);
        factory.Setup(value => value.TryCreate("B")).Returns(replacement);
        using var service = CreateService(factory);
        var attempt = service.ConnectBluetoothAudioAsync("A");
        await previous.StartStarted.Task;

        Assert.Equal(AudioConnectionResult.Connected, await service.ConnectBluetoothAudioAsync("B"));
        start.SetResult(true);

        Assert.Equal(AudioConnectionResult.Failed, await attempt);
        Assert.False(previous.OpenStarted.Task.IsCompleted);
        Assert.Equal(1, previous.DisposeCount);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("A"));
        Assert.True(await service.IsBluetoothDeviceConnectedAsync("B"));
    }

    /// <summary>Peer ownership excludes the requested source and released routes while retaining owned closed routes.</summary>
    [Fact]
    public async Task HasOtherAudioConnections_ExcludesSelfAndReleasedRoutes()
    {
        var first = new FakeAudioPlaybackConnection();
        var second = new FakeAudioPlaybackConnection();
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(first);
        factory.Setup(value => value.TryCreate("B")).Returns(second);
        using var service = CreateService(factory);
        Assert.False(service.HasOtherAudioConnections("A"));
        await service.ConnectBluetoothAudioAsync("A");

        Assert.False(service.HasOtherAudioConnections("a"));
        Assert.True(service.HasOtherAudioConnections("B"));
        await service.ConnectBluetoothAudioAsync("B");
        Assert.True(service.HasOtherAudioConnections("A"));
        service.Disconnect("A");
        Assert.False(service.HasOtherAudioConnections("B"));
        second.LoseConnection();
        Assert.True(service.HasOtherAudioConnections("A"));
        service.Disconnect("B");
        Assert.False(service.HasOtherAudioConnections("A"));
    }

    /// <summary>Registered native peers remain protected before their OpenAsync requests complete, including early Opened events.</summary>
    /// <param name="nativeOpened">Whether the peer reports native Opened state before its request completes.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HasOtherAudioConnections_IncludesPeerWithPendingOpenRequest(bool nativeOpened)
    {
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var peer = new FakeAudioPlaybackConnection { PendingOpen = completion };
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("B")).Returns(peer);
        using var service = CreateService(factory);
        var attempt = service.ConnectBluetoothAudioAsync("B");
        await peer.OpenStarted.Task;

        if (nativeOpened)
        {
            peer.OpenConnectionBeforeRequestCompletes();
        }

        var hasPendingPeer = service.HasOtherAudioConnections("A");
        var excludesPendingSelf = !service.HasOtherAudioConnections("b");
        var isManagedConnectedBeforeCompletion = await service.IsBluetoothDeviceConnectedAsync("B");
        completion.SetResult(AudioConnectionResult.Connected);
        await attempt;

        Assert.True(hasPendingPeer);
        Assert.True(excludesPendingSelf);
        Assert.False(isManagedConnectedBeforeCompletion);
    }

    /// <summary>A registered peer still awaiting native enabling is protected until its owner releases it.</summary>
    [Fact]
    public async Task HasOtherAudioConnections_IncludesPeerWithPendingEnableAndExcludesDisposedService()
    {
        var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var peer = new FakeAudioPlaybackConnection { PendingStart = start };
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("B")).Returns(peer);
        using var service = CreateService(factory);
        using var cancellation = new CancellationTokenSource();
        var attempt = service.ConnectBluetoothAudioAsync("B", cancellation.Token);
        await peer.StartStarted.Task;
        var hasPendingPeer = service.HasOtherAudioConnections("A");

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attempt);
        start.SetResult(true);
        var hasReleasedPeer = service.HasOtherAudioConnections("A");
        service.Dispose();

        Assert.True(hasPendingPeer);
        Assert.False(hasReleasedPeer);
        Assert.False(service.HasOtherAudioConnections("A"));
    }

    /// <summary>A foreign adapter sender cannot publish loss for the captured route, while real owned events still do.</summary>
    [Fact]
    public async Task StateChanged_FromUnrelatedAdapter_DoesNotPublishOwnedRouteLoss()
    {
        var route = new FakeAudioPlaybackConnection();
        var foreignRoute = new FakeAudioPlaybackConnection();
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(route);
        using var service = CreateService(factory);
        var lost = new List<string>();
        service.ConnectionLost += (_, args) => lost.Add(args.DeviceId);
        await service.ConnectBluetoothAudioAsync("A");

        route.RaiseStateChangedFrom(foreignRoute);

        Assert.Empty(lost);
        route.LoseConnection();
        Assert.Equal(["A"], lost);
    }
}
