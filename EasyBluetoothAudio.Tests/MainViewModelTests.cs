using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>Checks source discovery, exclusive connections, restoration, and cancellation.</summary>
public class MainViewModelTests
{
    private readonly Mock<IAudioService> _audio = new();
    private readonly Mock<IDevicePickerService> _picker = new();
    private readonly Mock<IDispatcherService> _dispatcher = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<IUpdateService> _updates = new();
    private readonly Mock<IStartupService> _startup = new();
    private readonly Mock<IBluetoothQualityService> _quality = new();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();
    private AppSettings _saved = new() { AutoConnect = false };

    /// <summary>Builds deterministic service defaults while preserving saved snapshots across writes.</summary>
    public MainViewModelTests()
    {
        _settings.Setup(service => service.Load()).Returns(() => _saved);
        _settings.Setup(service => service.Save(It.IsAny<AppSettings>())).Callback<AppSettings>(settings => _saved = settings);
        _dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        _picker.Setup(service => service.ShowAsync()).Returns(Task.CompletedTask);
        _audio.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync([]);
        _audio.Setup(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Connected);
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel(_audio.Object, _picker.Object, new UpdateViewModel(_updates.Object),
            new SettingsViewModel(_settings.Object, _startup.Object, _quality.Object, _messenger),
            _settings.Object, _dispatcher.Object, _messenger,
            device => new DeviceConnectionViewModel(device, _audio.Object, _dispatcher.Object, _messenger,
                (_, token) => Task.Delay(Timeout.Infinite, token)));
    }

    private static BluetoothDevice Device(string id, string? name = null)
    {
        return new BluetoothDevice { Id = id, Name = name ?? $"Phone {id}", IsConnected = true };
    }

    private void Discover(params BluetoothDevice[] devices)
    {
        _audio.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(devices);
    }

    /// <summary>The compact selector defaults to an available source instead of an unavailable remembered source.</summary>
    /// <returns>A task completing after startup discovery and selection.</returns>
    [Fact]
    public async Task Initialize_SelectedSourceDefaultsToFirstAvailableSource()
    {
        _saved.LastConnectedDevices = [new() { Id = "A", Name = "Unavailable phone" }];
        Discover(Device("B"));
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.False(viewModel.Sources[0].IsAvailable);
        Assert.Same(viewModel.Sources.Single(source => source.Id == "B"), viewModel.SelectedSource);
    }

    /// <summary>Discovery preserves the selected controller when a new source appears and existing devices are renamed.</summary>
    /// <returns>A task completing after both discovery snapshots.</returns>
    [Fact]
    public async Task Refresh_SelectedSourcePreservesIdentityWhenDevicesChange()
    {
        Discover(Device("A"), Device("B"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        Assert.Same(viewModel.Sources[0], viewModel.SelectedSource);
        var selected = viewModel.Sources[1];
        viewModel.SelectedSource = selected;
        Discover(Device("B", "Renamed phone"), Device("A"), Device("C"));

        await viewModel.RefreshDevicesAsync();

        Assert.Same(selected, viewModel.SelectedSource);
        Assert.Equal("Renamed phone", selected.Name);
        Assert.Contains(viewModel.Sources, source => source.Id == "C");
        _settings.Verify(service => service.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    /// <summary>Removing the selected idle source selects a remaining source and an empty discovery clears selection.</summary>
    /// <returns>A task completing after source removal and empty discovery.</returns>
    [Fact]
    public async Task Refresh_RemovedSelectedSourceReselectsOrClears()
    {
        Discover(Device("A"), Device("B"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        viewModel.SelectedSource = viewModel.Sources[1];
        Discover(Device("A"), Device("C"));

        await viewModel.RefreshDevicesAsync();

        Assert.Same(viewModel.Sources[0], viewModel.SelectedSource);
        Assert.Equal("A", viewModel.SelectedSource!.Id);
        Discover();
        await viewModel.RefreshDevicesAsync();
        Assert.Empty(viewModel.Sources);
        Assert.Null(viewModel.SelectedSource);
    }

    /// <summary>Changing the displayed source leaves the current route and startup preference untouched.</summary>
    /// <returns>A task completing after the current source connects.</returns>
    [Fact]
    public async Task SelectedSource_SwitchingPreservesCurrentRouteAndRememberedDevice()
    {
        Discover(Device("A"), Device("B"));
        var routeTokens = new List<CancellationToken>();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, token) => routeTokens.Add(token))
            .ReturnsAsync(AudioConnectionResult.Connected);
        var viewModel = CreateViewModel();
        try
        {
            await viewModel.RefreshDevicesAsync();
            await viewModel.Sources[0].ConnectAsync();
            var remembered = _saved.LastConnectedDevices;
            _audio.Invocations.Clear();
            _settings.Invocations.Clear();

            viewModel.SelectedSource = viewModel.Sources[1];
            viewModel.SelectedSource = viewModel.Sources[0];
            viewModel.SelectedSource = null;
            viewModel.SelectedSource = viewModel.Sources[1];

            Assert.True(viewModel.Sources[0].IsConnected);
            Assert.False(viewModel.Sources[1].IsConnected);
            Assert.NotEmpty(routeTokens);
            Assert.All(routeTokens, token => Assert.False(token.IsCancellationRequested));
            Assert.Equal("1 connected", viewModel.ConnectionSummary);
            Assert.Same(remembered, _saved.LastConnectedDevices);
            Assert.Equal("A", Assert.Single(_saved.LastConnectedDevices!).Id);
            _audio.Verify(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _audio.Verify(service => service.Disconnect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            _audio.Verify(service => service.DisconnectAll(It.IsAny<string>()), Times.Never);
            _settings.Verify(service => service.Save(It.IsAny<AppSettings>()), Times.Never);
        }
        finally
        {
            viewModel.Dispose();
        }
        Assert.Null(viewModel.SelectedSource);
    }

    /// <summary>The settings icon is a reversible toggle and persists changed preferences on return.</summary>
    [Fact]
    public async Task SettingsButton_ClickingAgainClosesAndSaves()
    {
        using var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();
        viewModel.OpenSettingsCommand.Execute(null);
        Assert.True(viewModel.IsSettingsOpen);
        viewModel.SettingsViewModel.ThemeMode = AppThemeMode.Light;

        viewModel.OpenSettingsCommand.Execute(null);

        Assert.False(viewModel.IsSettingsOpen);
        Assert.Equal(AppThemeMode.Light, _saved.ThemeMode);
    }

    /// <summary>The close affordance returns from preferences before it hides the receiver panel.</summary>
    [Fact]
    public async Task CloseAction_SettingsReturnsToSources_ThenHidesPanel()
    {
        using var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();
        var hidden = false;
        viewModel.RequestHide += () => hidden = true;
        viewModel.OpenSettingsCommand.Execute(null);

        await viewModel.CloseOrHideCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsSettingsOpen);
        Assert.False(hidden);
        await viewModel.CloseOrHideCommand.ExecuteAsync(null);
        Assert.True(hidden);
    }

    /// <summary>Discovery deduplicates identity while allowing two sources with the same friendly name.</summary>
    [Fact]
    public async Task Refresh_DeduplicatesIdsAndPreservesDuplicateNames()
    {
        Discover(Device("A", "Phone"), Device("a", "Phone"), Device("B", "Phone"));
        using var viewModel = CreateViewModel();

        await viewModel.RefreshDevicesAsync();

        Assert.Equal(2, viewModel.Sources.Count);
        Assert.All(viewModel.Sources, source => Assert.Equal("Phone", source.Name));
        _settings.Verify(service => service.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    /// <summary>Refresh updates names in place without replacing connected rows or their routes.</summary>
    [Fact]
    public async Task Refresh_PreservesRowIdentityAndConnection()
    {
        Discover(Device("A"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        var original = Assert.Single(viewModel.Sources);
        await original.ConnectAsync();
        Discover(Device("a", "Renamed phone"));

        await viewModel.RefreshDevicesAsync();

        Assert.Same(original, Assert.Single(viewModel.Sources));
        Assert.Equal("Renamed phone", original.Name);
        Assert.True(original.IsConnected);
    }

    /// <summary>Discovery failure retains live rows instead of treating an error as no devices.</summary>
    [Fact]
    public async Task Refresh_ErrorRetainsExistingRowsAndConnections()
    {
        Discover(Device("A"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        var source = Assert.Single(viewModel.Sources);
        await source.ConnectAsync();
        _audio.Setup(service => service.GetBluetoothDevicesAsync()).ThrowsAsync(new InvalidOperationException("Bluetooth unavailable"));

        await viewModel.RefreshDevicesAsync();

        Assert.Same(source, Assert.Single(viewModel.Sources));
        Assert.True(source.IsConnected);
        Assert.NotNull(viewModel.DiscoveryError);
        Assert.False(viewModel.IsRefreshing);
    }

    /// <summary>A live source absent from discovery stays visible while its route remains owned.</summary>
    [Fact]
    public async Task Refresh_MissingConnectedSourceRemainsVisible()
    {
        Discover(Device("A"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        var source = Assert.Single(viewModel.Sources);
        await source.ConnectAsync();
        Discover();

        await viewModel.RefreshDevicesAsync();

        Assert.Same(source, Assert.Single(viewModel.Sources));
        Assert.False(source.IsAvailable);
        Assert.True(source.IsConnected);
    }

    /// <summary>Startup restores only the last remembered source, without starting earlier saved devices.</summary>
    [Fact]
    public async Task Initialize_RestoresOnlyLastRememberedSource()
    {
        _saved.AutoConnect = true;
        _saved.LastConnectedDevices = [new() { Id = "A" }, new() { Id = "B" }];
        Discover(Device("B"));
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Unavailable);
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.DoesNotContain(viewModel.Sources, source => source.Id == "A" && source.IsBusy);
        Assert.True(viewModel.Sources.Single(source => source.Id == "B").IsConnected);
        Assert.Equal("B", Assert.Single(_saved.LastConnectedDevices!).Id);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A manual connection made while discovery is pending supersedes deferred startup restoration.</summary>
    /// <returns>A task completing after manual connection and pending startup discovery finish.</returns>
    [Fact]
    public async Task Initialize_ManualConnectDuringDiscoveryPreventsDeferredDeviceSwitch()
    {
        _saved.AutoConnect = true;
        _saved.LastConnectedDevices = [new() { Id = "A" }, new() { Id = "B" }];
        var discovery = new TaskCompletionSource<IEnumerable<BluetoothDevice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.Setup(service => service.GetBluetoothDevicesAsync()).Returns(discovery.Task);
        using var viewModel = CreateViewModel();
        var initialization = viewModel.InitializeAsync();

        await viewModel.Sources.Single(source => source.Id == "A").ConnectAsync();
        discovery.SetResult([Device("A"), Device("B")]);
        await initialization;

        Assert.True(viewModel.Sources.Single(source => source.Id == "A").IsConnected);
        Assert.False(viewModel.Sources.Single(source => source.Id == "B").IsConnected);
        Assert.Equal("A", Assert.Single(_saved.LastConnectedDevices!).Id);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("B", It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>An explicitly empty restore set never revives the legacy single selected source.</summary>
    [Fact]
    public async Task Initialize_ExplicitEmptySetDoesNotRestoreLegacyId()
    {
        _saved.AutoConnect = true;
        _saved.LastConnectedDevices = [];
        _saved.LastDeviceId = "A";
        Discover(Device("A"));
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        _audio.Verify(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Old settings without a restore set still restore their legacy device.</summary>
    [Fact]
    public async Task Initialize_MissingRestoreSetUsesLegacyId()
    {
        _saved.AutoConnect = true;
        _saved.LastDeviceId = "A";
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.True(Assert.Single(viewModel.Sources).IsConnected);
        Assert.Equal("A", Assert.Single(_saved.LastConnectedDevices!).Id);
    }

    /// <summary>The global startup toggle prevents saved source connections while retaining remembered rows.</summary>
    [Fact]
    public async Task Initialize_AutoConnectDisabledKeepsSourcesIdle()
    {
        _saved.LastConnectedDevices = [new() { Id = "A", Name = "Remembered phone" }];
        using var viewModel = CreateViewModel();

        await viewModel.InitializeAsync();

        Assert.Equal("Remembered phone", Assert.Single(viewModel.Sources).Name);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Connecting another source replaces the startup preference without changing unrelated settings.</summary>
    [Fact]
    public async Task ConnectionSuccess_PersistsOnlyCurrentSourceAndUnrelatedSettings()
    {
        _saved.ThemeMode = AppThemeMode.Light;
        _saved.ShowNotifications = false;
        Discover(Device("A"), Device("B"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        await Task.WhenAll(viewModel.Sources.Select(source => source.ConnectAsync()));

        Assert.Equal("B", Assert.Single(_saved.LastConnectedDevices!).Id);
        Assert.Equal(AppThemeMode.Light, _saved.ThemeMode);
        Assert.False(_saved.ShowNotifications);
        Assert.Equal("1 connected", viewModel.ConnectionSummary);
        Assert.False(viewModel.Sources[0].IsConnected);
        Assert.True(viewModel.Sources[1].IsConnected);
    }

    /// <summary>The old source is cancelled and disconnected before another source opens its native route.</summary>
    /// <returns>A task completing after the replacement source connects.</returns>
    [Fact]
    public async Task ConnectAnotherSource_DisconnectsCurrentSourceBeforeOpeningReplacement()
    {
        Discover(Device("A"), Device("B"));
        var firstToken = CancellationToken.None;
        var events = new List<string>();
        _audio.Setup(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, token) =>
            {
                if (id == "A") { firstToken = token; }
                events.Add($"connect-{id}");
            }).ReturnsAsync(AudioConnectionResult.Connected);
        _audio.Setup(service => service.Disconnect("A", "user", false)).Callback(() => events.Add("disconnect-A"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.Sources[0].ConnectAsync();
        events.Clear();

        await viewModel.Sources[1].ConnectAsync();

        Assert.True(firstToken.IsCancellationRequested);
        Assert.Equal("disconnect-A", events[0]);
        Assert.Equal("connect-B", events[1]);
        Assert.False(viewModel.Sources[0].ShowDisconnectAction);
        Assert.True(viewModel.Sources[1].IsConnected);
        Assert.Equal("B", Assert.Single(_saved.LastConnectedDevices!).Id);
    }

    /// <summary>A superseded native success cannot restore the old source or its startup preference.</summary>
    /// <returns>A task completing after the replacement connects and the superseded native request finishes.</returns>
    [Fact]
    public async Task ConnectAnotherSource_DiscardsLateSuccessFromPreviousSource()
    {
        Discover(Device("A"), Device("B"));
        var completion = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstToken = CancellationToken.None;
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, token) => firstToken = token).Returns(completion.Task);
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        var firstAttempt = viewModel.Sources[0].ConnectAsync();

        await viewModel.Sources[1].ConnectAsync();
        completion.SetResult(AudioConnectionResult.Connected);
        await firstAttempt.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(firstToken.IsCancellationRequested);
        Assert.False(viewModel.Sources[0].IsConnected || viewModel.Sources[0].IsBusy);
        Assert.True(viewModel.Sources[1].IsConnected);
        Assert.Equal("B", Assert.Single(_saved.LastConnectedDevices!).Id);
    }

    /// <summary>Switching sources stops either initial retries or automatic recovery before they can reopen.</summary>
    /// <param name="duringRecovery">Whether the previous source lost an established route before retrying.</param>
    /// <returns>A task completing after replacement connection cancels the previous retry loop.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectAnotherSource_CancelsPreviousRetryLoop(bool duringRecovery)
    {
        Discover(Device("A"), Device("B"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        if (duringRecovery) { await viewModel.Sources[0].ConnectAsync(); }
        _audio.Setup(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Unavailable);
        Task previousAttempt;
        if (duringRecovery)
        {
            _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));
            previousAttempt = Task.CompletedTask;
        }
        else
        {
            previousAttempt = viewModel.Sources[0].ConnectAsync();
        }
        Assert.True(viewModel.Sources[0].IsReconnecting);
        _audio.Invocations.Clear();

        await viewModel.Sources[1].ConnectAsync();
        await previousAttempt.WaitAsync(TimeSpan.FromSeconds(1));
        _audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs("A"));

        Assert.False(viewModel.Sources[0].IsConnected || viewModel.Sources[0].IsBusy);
        Assert.True(viewModel.Sources[1].IsConnected);
        _audio.Verify(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A recoverable old route loses its reconnect intent when another device connects.</summary>
    /// <returns>A task completing after the previous route falls back and its replacement connects.</returns>
    [Fact]
    public async Task ConnectAnotherSource_ClearsPreviousRecoverableRoute()
    {
        Discover(Device("A"), Device("B"));
        _audio.SetupSequence(service => service.ConnectBluetoothAudioAsync("A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Connected).ReturnsAsync(AudioConnectionResult.Connected)
            .ReturnsAsync(AudioConnectionResult.Failed);
        _audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("A")).ReturnsAsync(true);
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.Sources[0].ConnectAsync();
        await viewModel.Sources[0].ReconnectAsync();
        Assert.True(viewModel.Sources[0].ShowReconnectActions);

        await viewModel.Sources[1].ConnectAsync();

        Assert.False(viewModel.Sources[0].ShowDisconnectAction);
        Assert.False(viewModel.Sources[0].ReconnectCommand.CanExecute(null));
        Assert.True(viewModel.Sources[1].IsConnected);
        Assert.Equal("B", Assert.Single(_saved.LastConnectedDevices!).Id);
    }

    /// <summary>Disconnecting one source removes only that source from startup restoration.</summary>
    [Fact]
    public async Task ManualDisconnect_PreservesOtherSavedAndConnectedSource()
    {
        Discover(Device("A"), Device("B"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await Task.WhenAll(viewModel.Sources.Select(source => source.ConnectAsync()));

        viewModel.Sources.Single(source => source.Id == "A").Disconnect();

        Assert.Equal("B", Assert.Single(_saved.LastConnectedDevices!).Id);
        Assert.True(viewModel.Sources.Single(source => source.Id == "B").IsConnected);
    }

    /// <summary>Disconnect all invalidates startup restoration awaiting discovery.</summary>
    [Fact]
    public async Task DisconnectAll_DuringStartupDiscoveryPreventsDeferredRestoration()
    {
        _saved.AutoConnect = true;
        _saved.LastConnectedDevices = [new() { Id = "A" }, new() { Id = "B" }];
        var discovery = new TaskCompletionSource<IEnumerable<BluetoothDevice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.Setup(service => service.GetBluetoothDevicesAsync()).Returns(discovery.Task);
        using var viewModel = CreateViewModel();
        var initialization = viewModel.InitializeAsync();

        viewModel.DisconnectAll();
        discovery.SetResult([Device("A"), Device("B")]);
        await initialization;

        Assert.Empty(_saved.LastConnectedDevices!);
        Assert.All(viewModel.Sources, source => Assert.False(source.IsBusy));
        _audio.Verify(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Late successful native completions cannot reconnect sources after aggregate cancellation.</summary>
    [Fact]
    public async Task DisconnectAll_DiscardsPendingNativeSuccesses()
    {
        Discover(Device("A"), Device("B"));
        var native = new TaskCompletionSource<AudioConnectionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.Setup(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(native.Task);
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        var attempts = viewModel.Sources.Select(source => source.ConnectAsync()).ToArray();

        viewModel.DisconnectAll();
        native.SetResult(AudioConnectionResult.Connected);
        await Task.WhenAll(attempts);

        Assert.All(viewModel.Sources, source => Assert.False(source.IsConnected));
        Assert.Empty(_saved.LastConnectedDevices!);
        _audio.Verify(service => service.DisconnectAll("user-all"), Times.Once);
    }

    /// <summary>Shutdown rejects late discovery rather than creating new controllers after disposal.</summary>
    [Fact]
    public async Task Dispose_DuringRefreshDiscardsLateDevices()
    {
        var discovery = new TaskCompletionSource<IEnumerable<BluetoothDevice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _audio.Setup(service => service.GetBluetoothDevicesAsync()).Returns(discovery.Task);
        var viewModel = CreateViewModel();
        var refresh = viewModel.RefreshDevicesAsync();

        viewModel.Dispose();
        discovery.SetResult([Device("A")]);
        await refresh;

        Assert.Empty(viewModel.Sources);
    }

    /// <summary>Exit releases controllers without clearing startup restoration preferences.</summary>
    [Fact]
    public async Task Dispose_PreservesSavedRestorationSet()
    {
        Discover(Device("A"));
        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.Sources[0].ConnectAsync();
        _settings.Invocations.Clear();

        viewModel.Dispose();

        Assert.Equal("A", Assert.Single(_saved.LastConnectedDevices!).Id);
        _settings.Verify(service => service.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    /// <summary>Successful global quality changes recycle only the current source.</summary>
    [Fact]
    public async Task QualityChange_RecyclesOnlyCurrentSource()
    {
        Discover(Device("A"), Device("B"));
        using var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();
        await Task.WhenAll(viewModel.Sources.Select(source => source.ConnectAsync()));
        _audio.Invocations.Clear();

        _messenger.Send(new ReconnectRequestedMessage());

        Assert.False(viewModel.Sources[0].IsConnected);
        Assert.True(viewModel.Sources[1].IsConnected);
        _audio.Verify(service => service.Disconnect("A", "manual-recover", true), Times.Never);
        _audio.Verify(service => service.Disconnect("B", "manual-recover", true), Times.Once);
    }

    /// <summary>Pairing cancellation remains ordinary completion and still refreshes discovery.</summary>
    [Fact]
    public async Task OpenDevicePicker_RefreshesAfterDismissalAndClearsExternalInteraction()
    {
        using var viewModel = CreateViewModel();
        var activated = false;
        viewModel.RequestActivate += () => activated = true;

        await viewModel.OpenBluetoothSettingsCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsExternalInteractionActive);
        Assert.True(activated);
        _picker.Verify(service => service.ShowAsync(), Times.Once);
        _audio.Verify(service => service.GetBluetoothDevicesAsync(), Times.Once);
    }

    /// <summary>A startup preference write failure cannot turn an opened audio route into failed UI state.</summary>
    /// <param name="accessDenied">Whether the settings folder rejects access instead of reporting an I/O error.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectionSuccess_SettingsSaveFailurePreservesConnectedState(bool accessDenied)
    {
        Discover(Device("A"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        _settings.Setup(service => service.Save(It.IsAny<AppSettings>()))
            .Throws(accessDenied ? new UnauthorizedAccessException("Read-only settings")
                : new System.IO.IOException("Settings disk unavailable"));

        await viewModel.Sources[0].ConnectAsync();

        Assert.True(viewModel.Sources[0].IsConnected);
        Assert.Equal("Connected", viewModel.Sources[0].StatusText);
        Assert.Equal("1 connected", viewModel.ConnectionSummary);
        Assert.Contains("couldn’t be saved", viewModel.DiscoveryError);
    }

    /// <summary>Disconnect all still releases every route when clearing saved startup preferences fails.</summary>
    /// <param name="accessDenied">Whether the settings folder rejects access instead of reporting an I/O error.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisconnectAll_SettingsSaveFailureStillReleasesNativeRoutes(bool accessDenied)
    {
        Discover(Device("A"), Device("B"));
        using var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await Task.WhenAll(viewModel.Sources.Select(source => source.ConnectAsync()));
        _settings.Setup(service => service.Save(It.IsAny<AppSettings>()))
            .Throws(accessDenied ? new UnauthorizedAccessException("Read-only settings")
                : new System.IO.IOException("Settings disk unavailable"));

        viewModel.DisconnectAll();

        Assert.All(viewModel.Sources, source => Assert.False(source.IsConnected || source.IsBusy));
        _audio.Verify(service => service.DisconnectAll("user-all"), Times.Once);
        Assert.Contains("couldn’t be saved", viewModel.DiscoveryError);
    }
}
