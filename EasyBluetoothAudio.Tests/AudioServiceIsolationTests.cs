using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>Exercises native ownership and cancellation without Bluetooth hardware.</summary>
public class AudioServiceIsolationTests
{
    private readonly Mock<IAudioPlaybackConnectionFactory> _factory = new();
    private readonly Mock<IDispatcherService> _dispatcher = new();

    private TestAudioService CreateService()
    {
        _dispatcher.Setup(service => service.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());
        return new TestAudioService(_dispatcher.Object, [], _factory.Object);
    }

    /// <summary>Connecting another device releases the existing route before creating its replacement.</summary>
    /// <returns>A task completing after both connections and exclusive-ownership checks.</returns>
    [Fact]
    public async Task Connect_ReplacesExistingDeviceBeforeCreatingNewRoute()
    {
        var first = new FakeAudioPlaybackConnection();
        var second = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("A")).Returns(first);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(() =>
        {
            Assert.Equal(1, first.DisposeCount);
            return second;
        });
        using var service = CreateService();

        var results = await Task.WhenAll(
            service.ConnectBluetoothAudioAsync("A"), service.ConnectBluetoothAudioAsync("B"));

        Assert.All(results, result => Assert.Equal(AudioConnectionResult.Connected, result));
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("a"));
        Assert.True(await service.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(0, second.DisposeCount);
    }

    /// <summary>Switching back releases the current device and does not revive its earlier route.</summary>
    /// <returns>A task completing after switching back and releasing the requested route.</returns>
    [Fact]
    public async Task ReplaceRoute_SwitchesBackWithoutLeavingAnotherDeviceOpen()
    {
        var oldFirst = new FakeAudioPlaybackConnection();
        var newFirst = new FakeAudioPlaybackConnection();
        var second = new FakeAudioPlaybackConnection();
        _factory.SetupSequence(factory => factory.TryCreate("A")).Returns(oldFirst).Returns(newFirst);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(second);
        using var service = CreateService();
        await service.ConnectBluetoothAudioAsync("A");
        await service.ConnectBluetoothAudioAsync("B");

        await service.ConnectBluetoothAudioAsync("A");
        service.Disconnect("A", "test");

        Assert.Equal(1, oldFirst.DisposeCount);
        Assert.Equal(1, newFirst.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("B"));
    }

    /// <summary>A failed replacement preserves its result and leaves the previous device disconnected.</summary>
    /// <param name="result">The failed native open outcome to preserve.</param>
    /// <returns>A task completing after the failed replacement's cleanup.</returns>
    [Theory]
    [InlineData(AudioConnectionResult.TimedOut)]
    [InlineData(AudioConnectionResult.DeniedBySystem)]
    [InlineData(AudioConnectionResult.Failed)]
    public async Task FailedOpen_PreservesResultWithoutRestoringPreviousDevice(AudioConnectionResult result)
    {
        var failed = new FakeAudioPlaybackConnection { Result = result };
        var healthy = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("A")).Returns(failed);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(healthy);
        using var service = CreateService();
        await service.ConnectBluetoothAudioAsync("B");

        Assert.Equal(result, await service.ConnectBluetoothAudioAsync("A"));
        Assert.Equal(1, failed.DisposeCount);
        Assert.Equal(1, healthy.DisposeCount);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("B"));
    }

    /// <summary>Verifies that an unavailable native route has a distinct application outcome.</summary>
    [Fact]
    public async Task Connect_ReturnsUnavailable_WhenFactoryCannotCreateRoute()
    {
        using var service = CreateService();

        Assert.Equal(AudioConnectionResult.Unavailable, await service.ConnectBluetoothAudioAsync("A"));
    }

    /// <summary>Verifies that late success from a replaced attempt cannot replace or dispose its successor.</summary>
    [Fact]
    public async Task SupersededOpen_CannotReplaceNewRoute()
    {
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldRoute = new FakeAudioPlaybackConnection { PendingOpen = completion };
        var newRoute = new FakeAudioPlaybackConnection();
        _factory.SetupSequence(factory => factory.TryCreate("A")).Returns(oldRoute).Returns(newRoute);
        using var service = CreateService();
        var obsoleteAttempt = service.ConnectBluetoothAudioAsync("A");
        await oldRoute.OpenStarted.Task;

        Assert.Equal(AudioConnectionResult.Connected, await service.ConnectBluetoothAudioAsync("A"));
        completion.SetResult(AudioConnectionResult.Connected);

        Assert.Equal(AudioConnectionResult.Failed, await obsoleteAttempt);
        Assert.True(await service.IsBluetoothDeviceConnectedAsync("A"));
        Assert.Equal(1, oldRoute.DisposeCount);
        Assert.Equal(0, newRoute.DisposeCount);
    }

    /// <summary>A pending open for another device cannot activate after the selected device switches.</summary>
    /// <returns>A task completing after handoff and the obsolete native result.</returns>
    [Fact]
    public async Task SwitchDuringOpen_DiscardsPreviousDeviceLateSuccess()
    {
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var previous = new FakeAudioPlaybackConnection { PendingOpen = completion };
        var replacement = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("A")).Returns(previous);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(replacement);
        using var service = CreateService();
        var oldAttempt = service.ConnectBluetoothAudioAsync("A");
        await previous.OpenStarted.Task;

        Assert.Equal(AudioConnectionResult.Connected, await service.ConnectBluetoothAudioAsync("B"));
        completion.SetResult(AudioConnectionResult.Connected);

        Assert.Equal(AudioConnectionResult.Failed, await oldAttempt);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("A"));
        Assert.True(await service.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(1, previous.DisposeCount);
        Assert.Equal(0, replacement.DisposeCount);
    }

    /// <summary>A queued obsolete dispatcher callback cannot create a second native device route.</summary>
    /// <returns>A task completing after dispatching the superseded and current requests.</returns>
    [Fact]
    public async Task SwitchBeforeDispatch_DiscardsQueuedPreviousDeviceCreation()
    {
        var dispatch = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var replacement = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("B")).Returns(replacement);
        using var service = CreateService();
        var dispatchCount = 0;
        _dispatcher.Setup(dispatcher => dispatcher.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> action) =>
            {
                if (++dispatchCount == 1)
                {
                    await dispatch.Task;
                }

                await action();
            });
        var oldAttempt = service.ConnectBluetoothAudioAsync("A");

        Assert.Equal(AudioConnectionResult.Connected, await service.ConnectBluetoothAudioAsync("B"));
        dispatch.SetResult(true);

        Assert.Equal(AudioConnectionResult.Failed, await oldAttempt);
        _factory.Verify(factory => factory.TryCreate("A"), Times.Never);
        Assert.True(await service.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(0, replacement.DisposeCount);
    }

    /// <summary>A request cancelled before entry cannot disconnect the current device.</summary>
    /// <returns>A task completing after the cancelled request is rejected.</returns>
    [Fact]
    public async Task Connect_WithAlreadyCancelledToken_PreservesCurrentDevice()
    {
        var current = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("B")).Returns(current);
        using var service = CreateService();
        await service.ConnectBluetoothAudioAsync("B");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ConnectBluetoothAudioAsync("A", cancellation.Token));

        Assert.True(await service.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(0, current.DisposeCount);
        _factory.Verify(factory => factory.TryCreate("A"), Times.Never);
    }

    /// <summary>A concurrent connect waits until the previous native route has finished disposing.</summary>
    /// <returns>A task completing after the concurrent release and replacement.</returns>
    [Fact]
    public async Task Connect_DuringPreviousDisposal_WaitsBeforeCreatingReplacement()
    {
        var disposing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var replacementRequested = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseDisposal = new ManualResetEventSlim();
        var disposalFinished = false;
        var previous = new Mock<IAudioPlaybackConnection>();
        previous.SetupGet(connection => connection.IsOpen).Returns(true);
        previous.Setup(connection => connection.StartAsync()).Returns(Task.CompletedTask);
        previous.Setup(connection => connection.OpenAsync()).ReturnsAsync(AudioConnectionResult.Connected);
        previous.Setup(connection => connection.Dispose()).Callback(() =>
        {
            disposing.TrySetResult(true);
            Assert.True(releaseDisposal.Wait(TimeSpan.FromSeconds(3)));
            disposalFinished = true;
        });
        var replacement = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("A")).Returns(previous.Object);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(() =>
        {
            Assert.True(disposalFinished);
            return replacement;
        });
        using var service = CreateService();
        await service.ConnectBluetoothAudioAsync("A");
        var disconnect = Task.Run(() => service.Disconnect("A"));
        await disposing.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var connect = Task.Run(() =>
        {
            replacementRequested.TrySetResult(true);
            return service.ConnectBluetoothAudioAsync("B");
        });

        try
        {
            await replacementRequested.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Delay(50);
            _factory.Verify(factory => factory.TryCreate("B"), Times.Never);
        }
        finally
        {
            releaseDisposal.Set();
            await disconnect.WaitAsync(TimeSpan.FromSeconds(2));
        }

        Assert.Equal(AudioConnectionResult.Connected, await connect.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(await service.IsBluetoothDeviceConnectedAsync("B"));
        previous.Verify(connection => connection.Dispose(), Times.Once);
    }

    /// <summary>Cancelling a replacement leaves both its route and the previous device disconnected.</summary>
    /// <returns>A task completing after cancellation and the late native result.</returns>
    [Fact]
    public async Task CancelDuringOpen_DoesNotRestorePreviousDeviceOrCancelledRoute()
    {
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new FakeAudioPlaybackConnection { PendingOpen = completion };
        var healthy = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("A")).Returns(cancelled);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(healthy);
        using var service = CreateService();
        using var cancellation = new CancellationTokenSource();
        await service.ConnectBluetoothAudioAsync("B");
        var attempt = service.ConnectBluetoothAudioAsync("A", cancellation.Token);
        await cancelled.OpenStarted.Task;

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attempt.WaitAsync(TimeSpan.FromSeconds(2)));
        completion.SetResult(AudioConnectionResult.Connected);

        Assert.False(await service.IsBluetoothDeviceConnectedAsync("A"));
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(1, cancelled.DisposeCount);
        Assert.Equal(1, healthy.DisposeCount);
    }

    /// <summary>Verifies that a device-scoped disconnect invalidates its pending native completion.</summary>
    [Fact]
    public async Task DisconnectDuringOpen_DiscardsLateSuccess()
    {
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var route = new FakeAudioPlaybackConnection { PendingOpen = completion };
        _factory.Setup(factory => factory.TryCreate("A")).Returns(route);
        using var service = CreateService();
        var attempt = service.ConnectBluetoothAudioAsync("A");
        await route.OpenStarted.Task;

        service.Disconnect("A");
        completion.SetResult(AudioConnectionResult.Connected);

        Assert.Equal(AudioConnectionResult.Failed, await attempt);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("A"));
        Assert.Equal(1, route.DisposeCount);
    }

    /// <summary>Verifies that startup state transitions and disposed senders cannot report unrelated losses.</summary>
    [Fact]
    public async Task ConnectionLost_IdentifiesOnlyCurrentOpenedDevice()
    {
        var oldRoute = new FakeAudioPlaybackConnection();
        var currentRoute = new FakeAudioPlaybackConnection();
        var second = new FakeAudioPlaybackConnection();
        _factory.SetupSequence(factory => factory.TryCreate("A")).Returns(oldRoute).Returns(currentRoute);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(second);
        using var service = CreateService();
        var lost = new List<string>();
        service.ConnectionLost += (_, args) => lost.Add(args.DeviceId);
        await service.ConnectBluetoothAudioAsync("A");
        var queuedOldNotification = oldRoute.CaptureStateNotification();
        await service.ConnectBluetoothAudioAsync("A");
        await service.ConnectBluetoothAudioAsync("B");
        Assert.Empty(lost);

        oldRoute.LoseConnection();
        queuedOldNotification();
        second.LoseConnection();

        Assert.Equal(["B"], lost);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("A"));
    }

    /// <summary>Verifies aggregate teardown releases every native route and rejects subsequent opens after disposal.</summary>
    [Fact]
    public async Task Dispose_ClosesAllRoutesAndRejectsNewAttempts()
    {
        var first = new FakeAudioPlaybackConnection();
        var second = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("A")).Returns(first);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(second);
        var service = CreateService();
        await service.ConnectBluetoothAudioAsync("A");
        await service.ConnectBluetoothAudioAsync("B");

        service.Dispose();
        service.Dispose();

        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.ConnectBluetoothAudioAsync("C"));
        _factory.Verify(factory => factory.TryCreate("C"), Times.Never);
    }

    /// <summary>Verifies disposal between native creation and registration releases the local route before it starts.</summary>
    [Fact]
    public async Task DisposeDuringCreation_DoesNotStartOrLeakLateNativeRoute()
    {
        var route = new FakeAudioPlaybackConnection();
        using var service = CreateService();
        _factory.Setup(factory => factory.TryCreate("A")).Returns(() =>
        {
            service.Dispose();
            return route;
        });

        Assert.Equal(AudioConnectionResult.Failed, await service.ConnectBluetoothAudioAsync("A"));
        Assert.Equal(0, route.StartCount);
        Assert.Equal(1, route.DisposeCount);
    }

    /// <summary>Verifies aggregate disconnect invalidates every in-flight route without marking the service disposed.</summary>
    [Fact]
    public async Task DisconnectAll_InvalidatesPendingOpensAndAllowsLaterConnect()
    {
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new FakeAudioPlaybackConnection { PendingOpen = completion };
        var other = new FakeAudioPlaybackConnection();
        var replacement = new FakeAudioPlaybackConnection();
        _factory.SetupSequence(factory => factory.TryCreate("A")).Returns(pending).Returns(replacement);
        _factory.Setup(factory => factory.TryCreate("B")).Returns(other);
        using var service = CreateService();
        var attempt = service.ConnectBluetoothAudioAsync("A");
        await pending.OpenStarted.Task;
        await service.ConnectBluetoothAudioAsync("B");

        service.DisconnectAll("test");
        completion.SetResult(AudioConnectionResult.Connected);

        Assert.Equal(AudioConnectionResult.Failed, await attempt);
        Assert.Equal(1, pending.DisposeCount);
        Assert.Equal(1, other.DisposeCount);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(AudioConnectionResult.Connected, await service.ConnectBluetoothAudioAsync("A"));
    }

    /// <summary>Cancelling before UI dispatch prevents creation after the previous device was released.</summary>
    /// <returns>A task completing after cancellation of the queued replacement.</returns>
    [Fact]
    public async Task CancelBeforeDispatch_DoesNotCreateNativeRoute()
    {
        var healthy = new FakeAudioPlaybackConnection();
        _factory.Setup(factory => factory.TryCreate("B")).Returns(healthy);
        using var service = CreateService();
        await service.ConnectBluetoothAudioAsync("B");
        var dispatch = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.Setup(dispatcher => dispatcher.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> action) =>
            {
                await dispatch.Task;
                await action();
            });
        using var cancellation = new CancellationTokenSource();
        var attempt = service.ConnectBluetoothAudioAsync("A", cancellation.Token);

        cancellation.Cancel();
        dispatch.SetResult(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => attempt);
        _factory.Verify(factory => factory.TryCreate("A"), Times.Never);
        Assert.False(await service.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(1, healthy.DisposeCount);
    }
}
