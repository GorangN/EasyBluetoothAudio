using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>Exercises exclusive source coordination together with real native-route ownership.</summary>
public class ConcurrentSourceStartupTests
{
    private static Mock<IDispatcherService> CreateDispatcher()
    {
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        dispatcher.Setup(service => service.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> action) => action());
        return dispatcher;
    }

    private static MainViewModel CreateViewModel(IAudioService audio, IDispatcherService dispatcher,
        AppSettings? saved = null)
    {
        var settings = new Mock<ISettingsService>();
        var currentSettings = saved ?? new AppSettings { AutoConnect = false, ShowNotifications = false };
        settings.Setup(service => service.Load()).Returns(() => currentSettings);
        settings.Setup(service => service.Save(It.IsAny<AppSettings>()))
            .Callback<AppSettings>(value => currentSettings = value);
        var messenger = new WeakReferenceMessenger();
        return new MainViewModel(audio, Mock.Of<IDevicePickerService>(),
            new UpdateViewModel(Mock.Of<IUpdateService>()),
            new SettingsViewModel(settings.Object, Mock.Of<IStartupService>(),
                Mock.Of<IBluetoothQualityService>(), messenger),
            settings.Object, dispatcher, messenger,
            device => new DeviceConnectionViewModel(device, audio, dispatcher, messenger,
                (_, token) => Task.Delay(Timeout.Infinite, token)));
    }

    /// <summary>Switching a connected source cancels its monitor and ignores queued native loss notifications.</summary>
    /// <returns>A task completing after both source attempts and stale-event assertions.</returns>
    [Fact]
    public async Task SwitchConnectedSource_PreviousSourceCannotRecoverAfterHandoff()
    {
        var first = new FakeAudioPlaybackConnection();
        var secondInitial = new FakeAudioPlaybackConnection();
        var second = new FakeAudioPlaybackConnection();
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.SetupSequence(value => value.TryCreate("A")).Returns(new FakeAudioPlaybackConnection()).Returns(first);
        factory.SetupSequence(value => value.TryCreate("B")).Returns(secondInitial).Returns(second);
        var dispatcher = CreateDispatcher();
        using var audio = new TestAudioService(dispatcher.Object, [("A", "Phone"), ("B", "Computer")], factory.Object);
        using var viewModel = CreateViewModel(audio, dispatcher.Object);
        await viewModel.RefreshDevicesAsync();
        var oldSource = viewModel.Sources.Single(source => source.Id == "A");
        var newSource = viewModel.Sources.Single(source => source.Id == "B");
        await oldSource.ConnectAsync();
        var queuedOldLoss = first.CaptureStateNotification();

        await newSource.ConnectAsync();
        first.LoseConnection();
        queuedOldLoss();

        Assert.False(oldSource.IsConnected);
        Assert.False(oldSource.IsBusy);
        Assert.True(newSource.IsConnected);
        Assert.Equal("1 connected", viewModel.ConnectionSummary);
        Assert.False(await audio.IsBluetoothDeviceConnectedAsync("A"));
        Assert.True(await audio.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, secondInitial.DisposeCount);
        Assert.Equal(0, second.DisposeCount);
        factory.Verify(value => value.TryCreate("A"), Times.Exactly(2));
        factory.Verify(value => value.TryCreate("B"), Times.Exactly(2));
    }

    /// <summary>A pending old source is cancelled before its late native completion can replace the chosen source.</summary>
    /// <param name="nativeAlreadyOpened">Whether Windows reported the old native route open before completing its request.</param>
    /// <returns>A task completing after handoff and the previous attempt's cancellation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SwitchPendingSource_LateCompletionCannotRestorePreviousDevice(bool nativeAlreadyOpened)
    {
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var previous = new FakeAudioPlaybackConnection { PendingOpen = completion };
        var replacement = new FakeAudioPlaybackConnection();
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.Setup(value => value.TryCreate("A")).Returns(previous);
        factory.SetupSequence(value => value.TryCreate("B"))
            .Returns(new FakeAudioPlaybackConnection()).Returns(replacement);
        var dispatcher = CreateDispatcher();
        using var audio = new TestAudioService(dispatcher.Object, [("A", "Phone"), ("B", "Computer")], factory.Object);
        using var viewModel = CreateViewModel(audio, dispatcher.Object);
        await viewModel.RefreshDevicesAsync();
        var oldSource = viewModel.Sources.Single(source => source.Id == "A");
        var newSource = viewModel.Sources.Single(source => source.Id == "B");
        var previousAttempt = oldSource.ConnectAsync();
        await previous.OpenStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        if (nativeAlreadyOpened)
        {
            previous.OpenConnectionBeforeRequestCompletes();
        }

        try
        {
            await newSource.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(2));
            await previousAttempt.WaitAsync(TimeSpan.FromSeconds(2));
            completion.SetResult(AudioConnectionResult.Connected);

            Assert.False(oldSource.IsConnected);
            Assert.False(oldSource.IsBusy);
            Assert.True(newSource.IsConnected);
            Assert.False(await audio.IsBluetoothDeviceConnectedAsync("A"));
            Assert.True(await audio.IsBluetoothDeviceConnectedAsync("B"));
            Assert.Equal(1, previous.DisposeCount);
            Assert.Equal(0, replacement.DisposeCount);
            factory.Verify(value => value.TryCreate("A"), Times.Once);
        }
        finally
        {
            oldSource.CancelOperations();
            completion.TrySetResult(AudioConnectionResult.Connected);
            await previousAttempt.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    /// <summary>Saved settings from the multi-device version restore only the most recent source.</summary>
    /// <returns>A task completing after startup restoration and native ownership checks.</returns>
    [Fact]
    public async Task Startup_WithMultipleRememberedDevices_OpensOnlyOneNativeSource()
    {
        var current = new FakeAudioPlaybackConnection();
        var factory = new Mock<IAudioPlaybackConnectionFactory>();
        factory.SetupSequence(value => value.TryCreate("B")).Returns(new FakeAudioPlaybackConnection()).Returns(current);
        var dispatcher = CreateDispatcher();
        using var audio = new TestAudioService(dispatcher.Object, [("A", "Phone"), ("B", "Computer")], factory.Object);
        var saved = new AppSettings
        {
            AutoConnect = true,
            ShowNotifications = false,
            LastConnectedDevices = [new() { Id = "A", Name = "Phone" }, new() { Id = "B", Name = "Computer" }]
        };
        using var viewModel = CreateViewModel(audio, dispatcher.Object, saved);

        await viewModel.InitializeAsync();

        Assert.False(viewModel.Sources.Single(source => source.Id == "A").IsConnected);
        Assert.True(viewModel.Sources.Single(source => source.Id == "B").IsConnected);
        Assert.Equal("1 connected", viewModel.ConnectionSummary);
        Assert.Equal("B", Assert.Single(saved.LastConnectedDevices!).Id);
        Assert.True(await audio.IsBluetoothDeviceConnectedAsync("B"));
        Assert.Equal(0, current.DisposeCount);
        factory.Verify(value => value.TryCreate("A"), Times.Never);
        factory.Verify(value => value.TryCreate("B"), Times.Exactly(2));
    }
}
