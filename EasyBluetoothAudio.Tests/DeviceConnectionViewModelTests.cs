using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>Verifies isolated source connection, retry, cancellation, and recovery behavior.</summary>
public class DeviceConnectionViewModelTests
{
    private readonly Mock<IAudioService> _audio = new();
    private readonly Mock<IDispatcherService> _dispatcher = new();

    /// <summary>Creates immediate dispatcher and successful audio defaults.</summary>
    public DeviceConnectionViewModelTests()
    {
        _dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        _audio.Setup(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Connected);
    }

    private DeviceConnectionViewModel CreateSource(string id = "A",
        Func<int, CancellationToken, Task>? retryDelay = null)
    {
        return new DeviceConnectionViewModel(new BluetoothDevice { Id = id, Name = $"Phone {id}", IsConnected = true },
            _audio.Object, _dispatcher.Object, new WeakReferenceMessenger(), retryDelay ?? ((_, _) => Task.CompletedTask));
    }

    /// <summary>Fresh connections prime only their own route before reporting success.</summary>
    [Fact]
    public async Task Connect_PrimesOnceBeforeReportingConnected()
    {
        using var source = CreateSource();
        var established = 0;
        source.ConnectionEstablished += (_, _) => established++;

        await source.ConnectAsync();

        Assert.True(source.IsConnected);
        Assert.Equal("Connected", source.StatusText);
        Assert.Equal(1, established);
        _audio.Verify(service => service.Disconnect("A", "startup-prime", true), Times.Once);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>Initial transient failures retain connection intent beyond the old bounded retry count.</summary>
    [Fact]
    public async Task Connect_RetriesTransientFailuresUntilSuccessful()
    {
        _audio.SetupSequence(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Unavailable)
            .ReturnsAsync(AudioConnectionResult.TimedOut)
            .ReturnsAsync(AudioConnectionResult.Failed)
            .ReturnsAsync(AudioConnectionResult.Connected)
            .ReturnsAsync(AudioConnectionResult.Connected);
        using var source = CreateSource();

        await source.ConnectAsync();

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    /// <summary>A failed startup prime joins sticky retries without priming repeatedly.</summary>
    [Fact]
    public async Task Connect_PrimeFailureRetriesWithoutAnotherPrime()
    {
        _audio.SetupSequence(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Connected)
            .ReturnsAsync(AudioConnectionResult.Failed)
            .ReturnsAsync(AudioConnectionResult.Connected);
        using var source = CreateSource();

        await source.ConnectAsync();

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.Disconnect("A", "startup-prime", true), Times.Once);
    }

    /// <summary>System denial ends automatic retry and leaves a manual Connect action.</summary>
    [Fact]
    public async Task Connect_SystemDenialStopsRetrying()
    {
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.DeniedBySystem);
        using var source = CreateSource();

        await source.ConnectAsync();

        Assert.False(source.IsBusy);
        Assert.True(source.ConnectCommand.CanExecute(null));
        Assert.Contains("Windows", source.ErrorText);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Disconnect cancels pending retry delay immediately and prevents another attempt.</summary>
    [Fact]
    public async Task Disconnect_DuringRetryDelayPreventsFurtherAttempts()
    {
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Unavailable);
        using var source = CreateSource(retryDelay: (_, token) => Task.Delay(Timeout.Infinite, token));
        var connection = source.ConnectAsync();
        Assert.True(source.IsReconnecting);
        Assert.True(source.DisconnectCommand.CanExecute(null));

        source.Disconnect();
        await connection.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(source.IsBusy);
        Assert.False(source.IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>A native operation completing after Disconnect cannot restore state or persistence events.</summary>
    [Fact]
    public async Task Disconnect_DuringNativeOpenDiscardsLateSuccess()
    {
        var pending = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>())).Returns(pending.Task);
        using var source = CreateSource();
        var established = 0;
        source.ConnectionEstablished += (_, _) => established++;
        var connection = source.ConnectAsync();

        source.Disconnect();
        pending.SetResult(AudioConnectionResult.Connected);
        await connection;

        Assert.False(source.IsConnected);
        Assert.Equal(0, established);
        _audio.Verify(service => service.Disconnect("A", "startup-prime", true), Times.Never);
    }

    /// <summary>Route actions on A leave B's state and native connection untouched.</summary>
    [Fact]
    public async Task ReconnectAndDisconnect_OnlyAffectTheirOwnDevice()
    {
        using var first = CreateSource("A");
        using var second = CreateSource("B");
        await Task.WhenAll(first.ConnectAsync(), second.ConnectAsync());
        _audio.Invocations.Clear();

        await first.ReconnectAsync();
        first.Disconnect();

        Assert.True(second.IsConnected);
        Assert.True(second.ReconnectCommand.CanExecute(null));
        _audio.Verify(service => service.Disconnect("B", It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("B", It.IsAny<CancellationToken>()), Times.Never);
        _audio.Verify(service => service.Disconnect("A", "manual-recover", true), Times.Once);
    }

    /// <summary>A missing source's sticky retry does not block a different source from opening.</summary>
    [Fact]
    public async Task MissingDevice_DoesNotBlockAnotherDevice()
    {
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Unavailable);
        using var first = CreateSource("A", (_, token) => Task.Delay(Timeout.Infinite, token));
        using var second = CreateSource("B");
        var firstConnection = first.ConnectAsync();

        await second.ConnectAsync();

        Assert.True(first.IsReconnecting);
        Assert.True(second.IsConnected);
        first.Disconnect();
        await firstConnection;
    }

    /// <summary>Failed manual recycle preserves Reconnect guidance when the physical source remains present.</summary>
    [Fact]
    public async Task Reconnect_FailureWithPhysicalLinkOffersReconnect()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(AudioConnectionResult.Failed);
        _audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("A")).ReturnsAsync(true);

        await source.ReconnectAsync();

        Assert.False(source.IsConnected);
        Assert.True(source.ReconnectCommand.CanExecute(null));
        Assert.False(source.ConnectCommand.CanExecute(null));
        Assert.True(source.DisconnectCommand.CanExecute(null));
    }

    /// <summary>Failed manual recycle offers full Connect when the source's physical link has disappeared.</summary>
    [Fact]
    public async Task Reconnect_FailureWithoutPhysicalLinkOffersConnect()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(AudioConnectionResult.Failed);

        await source.ReconnectAsync();

        Assert.True(source.ConnectCommand.CanExecute(null));
        Assert.False(source.ReconnectCommand.CanExecute(null));
    }

    /// <summary>Disconnect invalidates an outstanding physical-presence query used for manual fallback.</summary>
    [Fact]
    public async Task Disconnect_DuringFallbackQueryCannotRestoreReconnectGuidance()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(AudioConnectionResult.Failed);
        var presence = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("A")).Returns(presence.Task);
        var reconnect = source.ReconnectAsync();

        source.Disconnect();
        presence.SetResult(true);
        await reconnect;

        Assert.False(source.IsConnected);
        Assert.False(source.ShowReconnectActions);
    }

    /// <summary>Device-tagged loss events restore only the affected source without a second startup prime.</summary>
    [Fact]
    public async Task ConnectionLost_RecoversOnlyMatchingDevice()
    {
        using var first = CreateSource("A");
        using var second = CreateSource("B");
        await Task.WhenAll(first.ConnectAsync(), second.ConnectAsync());
        _audio.Invocations.Clear();

        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("a"));

        Assert.True(first.IsConnected);
        Assert.True(second.IsConnected);
        _audio.Verify(service => service.Disconnect("A", "service-connection-lost", false), Times.Once);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Once);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("B", It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A transient loss event is ignored when the device-scoped cross-check still reports an open route.</summary>
    [Fact]
    public async Task ConnectionLost_ConnectedCrossCheckDoesNotRecycle()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        _audio.Setup(service => service.IsBluetoothDeviceConnectedAsync("A")).ReturnsAsync(true);
        _audio.Invocations.Clear();

        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.Disconnect("A", It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    /// <summary>A system denial during recovery stops monitoring rather than silently restarting the terminal failure.</summary>
    [Fact]
    public async Task ConnectionLost_SystemDenialStopsFurtherAutomaticRecovery()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.DeniedBySystem);
        _audio.Invocations.Clear();

        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));
        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));

        Assert.False(source.IsBusy);
        Assert.True(source.ConnectCommand.CanExecute(null));
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>An older open completing after a replacement cannot replace the newer source's stabilized state.</summary>
    [Fact]
    public async Task Connect_ReplacementDiscardsPredecessorSuccess()
    {
        var predecessor = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.SetupSequence(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .Returns(predecessor.Task)
            .ReturnsAsync(AudioConnectionResult.Connected)
            .ReturnsAsync(AudioConnectionResult.Connected);
        using var source = CreateSource();
        var established = 0;
        source.ConnectionEstablished += (_, _) => established++;
        var originalAttempt = source.ConnectAsync();

        source.Disconnect();
        await source.ConnectAsync();
        predecessor.SetResult(AudioConnectionResult.Connected);
        await originalAttempt;

        Assert.True(source.IsConnected);
        Assert.Equal(1, established);
        _audio.Verify(service => service.Disconnect("A", "startup-prime", true), Times.Once);
    }

    /// <summary>Concurrent loss notifications share one recovery and remain cancellable.</summary>
    [Fact]
    public async Task ConnectionLost_DuplicateEventsStartOneRecovery()
    {
        using var source = CreateSource(retryDelay: (_, token) => Task.Delay(Timeout.Infinite, token));
        await source.ConnectAsync();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(AudioConnectionResult.Unavailable);
        _audio.Invocations.Clear();

        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));
        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));

        Assert.True(source.IsReconnecting);
        _audio.Verify(service => service.Disconnect("A", "service-connection-lost", false), Times.Once);
        source.Disconnect();
        Assert.False(source.IsReconnecting);
    }

    /// <summary>Queued worker recovery cannot tear down a route after the user cancels its operation.</summary>
    [Fact]
    public async Task ConnectionLost_QueuedRecoveryRechecksCancellationOnDispatcher()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        var queued = new Queue<Action>();
        _dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => queued.Enqueue(action));
        _audio.Invocations.Clear();

        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));
        source.Disconnect();
        while (queued.TryDequeue(out var action))
        {
            action();
        }

        Assert.False(source.IsConnected);
        _audio.Verify(service => service.Disconnect("A", "service-connection-lost", false), Times.Never);
    }

    /// <summary>Queued successful recovery cannot restore Connected after a later user disconnect.</summary>
    [Fact]
    public async Task ConnectionLost_QueuedSuccessCannotRestoreCancelledState()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        Action? connectedCallback = null;
        var dispatches = 0;
        _dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action =>
        {
            if (++dispatches == 2)
            {
                connectedCallback = action;
            }
            else
            {
                action();
            }
        });

        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));
        source.Disconnect();
        Assert.NotNull(connectedCallback);
        connectedCallback();

        Assert.False(source.IsConnected);
    }

    /// <summary>Cleanup failure does not preserve user connection intent or block row state reset.</summary>
    [Fact]
    public async Task Disconnect_StillNotifiesUserIntentWhenNativeTeardownFails()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        var disconnected = 0;
        source.UserDisconnected += (_, _) => disconnected++;
        _audio.Setup(service => service.Disconnect("A", "user", false)).Throws(new InvalidOperationException("native failure"));

        source.Disconnect();

        Assert.False(source.IsConnected);
        Assert.Equal(1, disconnected);
    }

    /// <summary>Disposal releases the route once without emitting user-disconnect persistence intent.</summary>
    [Fact]
    public async Task Dispose_IsIdempotentAndRejectsFurtherConnects()
    {
        var source = CreateSource();
        await source.ConnectAsync();
        _audio.Invocations.Clear();
        var disconnected = 0;
        source.UserDisconnected += (_, _) => disconnected++;

        source.Dispose();
        source.Dispose();
        await source.ConnectAsync();

        Assert.Equal(0, disconnected);
        _audio.Verify(service => service.Disconnect("A", "dispose", false), Times.Once);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Never);
    }
}
