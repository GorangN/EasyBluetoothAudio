using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>Checks bounded source recovery, monotonic budget expiry, and safe additional-source setup.</summary>
public class DeviceAutomaticRecoveryTests
{
    private readonly Mock<IAudioService> _audio = new();
    private readonly Mock<IDispatcherService> _dispatcher = new();

    /// <summary>Creates completed audio calls and a dispatcher for deterministic recovery tests.</summary>
    public DeviceAutomaticRecoveryTests()
    {
        _dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Connected);
        _audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("A")).ReturnsAsync(true);
    }

    private DeviceConnectionViewModel CreateSource(TimeProvider? timeProvider = null,
        Func<int, CancellationToken, Task>? retryDelay = null)
    {
        return new DeviceConnectionViewModel(new BluetoothDevice { Id = "A", Name = "Phone", IsConnected = true },
            _audio.Object, _dispatcher.Object, new WeakReferenceMessenger(),
            retryDelay ?? ((_, _) => Task.CompletedTask), timeProvider);
    }

    private void RaiseLoss()
    {
        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));
    }

    /// <summary>Successful short-lived reopens do not erase the repeated-interruption budget.</summary>
    [Fact]
    public async Task AutomaticRecovery_ThirdLossPausesForManualReconnect()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        _audio.Invocations.Clear();

        RaiseLoss();
        RaiseLoss();
        RaiseLoss();
        RaiseLoss();

        Assert.False(source.IsConnected);
        Assert.False(source.IsBusy);
        Assert.True(source.ReconnectCommand.CanExecute(null));
        Assert.Contains("interrupted repeatedly", source.ErrorText);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Exactly(2));
        _audio.Verify(service => service.Disconnect("A", "automatic-recovery-paused", false), Times.Once);
    }

    /// <summary>Losses older than the sliding recovery window expire without wall-clock sleeps.</summary>
    [Fact]
    public async Task AutomaticRecovery_StableElapsedTimeReplenishesSlidingWindow()
    {
        long timestamp = 0;
        var clock = new Mock<TimeProvider>();
        clock.SetupGet(provider => provider.TimestampFrequency).Returns(1_000);
        clock.Setup(provider => provider.GetTimestamp()).Returns(() => timestamp);
        using var source = CreateSource(clock.Object);
        await source.ConnectAsync();
        _audio.Invocations.Clear();

        RaiseLoss();
        timestamp = 10_000;
        RaiseLoss();
        timestamp = 60_000;
        RaiseLoss();

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Exactly(3));

        timestamp = 61_000;
        RaiseLoss();
        Assert.False(source.IsConnected);
        Assert.True(source.ReconnectCommand.CanExecute(null));
    }

    /// <summary>Explicit user recovery starts a new operation with a fresh automatic recovery budget.</summary>
    [Fact]
    public async Task ManualReconnect_ResetsAutomaticRecoveryBudget()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        RaiseLoss();
        RaiseLoss();
        RaiseLoss();
        Assert.False(source.IsConnected);

        await source.ReconnectAsync();
        _audio.Invocations.Clear();
        RaiseLoss();
        RaiseLoss();

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>A full user Connect also resets the budget after a missing source exhausted automatic recovery.</summary>
    [Fact]
    public async Task ManualConnect_ResetsAutomaticRecoveryBudget()
    {
        _audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("A")).ReturnsAsync(false);
        using var source = CreateSource();
        await source.ConnectAsync();
        RaiseLoss();
        RaiseLoss();
        RaiseLoss();
        Assert.False(source.IsConnected);

        await source.ConnectAsync();
        _audio.Invocations.Clear();
        RaiseLoss();
        RaiseLoss();

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>A delayed fallback check cannot restore reconnect guidance after the user cancels the source.</summary>
    [Fact]
    public async Task AutomaticRecovery_QueuedFallbackRechecksCancellation()
    {
        using var source = CreateSource();
        await source.ConnectAsync();
        RaiseLoss();
        RaiseLoss();
        var presence = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("A")).Returns(presence.Task);
        RaiseLoss();
        source.Disconnect();
        var dispatched = new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => dispatched.TrySetResult(action));

        presence.SetResult(true);
        var queuedFallback = await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(1));
        queuedFallback();

        Assert.False(source.IsConnected);
        Assert.False(source.ShowReconnectActions);
        Assert.False(source.IsBusy);
    }

    /// <summary>Transient automatic recovery failures stop after three opens with the existing retry spacing.</summary>
    [Fact]
    public async Task AutomaticRecovery_TransientFailuresStopAfterThreeAttempts()
    {
        var delays = new List<int>();
        using var source = CreateSource(retryDelay: (milliseconds, _) =>
        {
            delays.Add(milliseconds);
            return Task.CompletedTask;
        });
        await source.ConnectAsync();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Unavailable);
        _audio.Invocations.Clear();

        RaiseLoss();

        Assert.False(source.IsBusy);
        Assert.True(source.ReconnectCommand.CanExecute(null));
        Assert.Contains("Automatic reconnection failed", source.ErrorText);
        Assert.Equal([DeviceConnectionController.RetryDelayMs, DeviceConnectionController.RetryDelayMs], delays);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()),
            Times.Exactly(DeviceConnectionController.AutomaticRecoveryAttemptLimit));
    }

    /// <summary>Paused recovery guides a missing physical source to a full Connect instead of a route recycle.</summary>
    [Fact]
    public async Task AutomaticRecovery_MissingPhysicalSourceOffersConnectAfterBudgetExhaustion()
    {
        _audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("A")).ReturnsAsync(false);
        using var source = CreateSource();
        await source.ConnectAsync();

        RaiseLoss();
        RaiseLoss();
        RaiseLoss();

        Assert.False(source.ReconnectCommand.CanExecute(null));
        Assert.True(source.ConnectCommand.CanExecute(null));
        Assert.Contains("Bluetooth settings", source.ErrorText);
    }

    /// <summary>A peer route managed at the start prevents additional-source startup recycling.</summary>
    [Fact]
    public async Task Connect_ExistingManagedPeerSkipsStartupPrimeEvenIfPeerIsLaterReleased()
    {
        _audio.SetupSequence(service => service.HasOtherAudioConnections("A")).Returns(true).Returns(false);
        using var source = CreateSource();

        await source.ConnectAsync();

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Once);
        _audio.Verify(service => service.Disconnect("A", "startup-prime", true), Times.Never);
    }

    /// <summary>A peer appearing while the source opens also prevents the optional startup recycle.</summary>
    [Fact]
    public async Task Connect_PeerAppearingDuringOpenSkipsStartupPrime()
    {
        _audio.SetupSequence(service => service.HasOtherAudioConnections("A")).Returns(false).Returns(true);
        using var source = CreateSource();

        await source.ConnectAsync();

        Assert.True(source.IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Once);
        _audio.Verify(service => service.Disconnect("A", "startup-prime", true), Times.Never);
    }
}
