using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Tests for <see cref="MainViewModel"/> covering device refresh, connection lifecycle, and recovery behavior.
/// </summary>
public class MainViewModelTests
{
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<IDevicePickerService> _devicePickerServiceMock;
    private readonly Mock<IDispatcherService> _dispatcherServiceMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IStartupService> _startupServiceMock;
    private readonly Mock<IBluetoothQualityService> _qualityServiceMock;
    private readonly IMessenger _messenger;

    /// <summary>
    /// Initializes shared mocks and a fresh messenger for each test.
    /// </summary>
    public MainViewModelTests()
    {
        _audioServiceMock = new Mock<IAudioService>();
        _devicePickerServiceMock = new Mock<IDevicePickerService>();
        _dispatcherServiceMock = new Mock<IDispatcherService>();
        _updateServiceMock = new Mock<IUpdateService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _startupServiceMock = new Mock<IStartupService>();
        _qualityServiceMock = new Mock<IBluetoothQualityService>();
        _messenger = new WeakReferenceMessenger();

        _devicePickerServiceMock.Setup(service => service.ShowAsync()).Returns(Task.CompletedTask);
        _settingsServiceMock.Setup(service => service.Load()).Returns(new AppSettings());
        _dispatcherServiceMock.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync(It.IsAny<string>())).ReturnsAsync(false);
    }

    private MainViewModel CreateViewModel()
    {
        var settingsViewModel = new SettingsViewModel(
            _settingsServiceMock.Object,
            _startupServiceMock.Object,
            _qualityServiceMock.Object,
            _messenger);
        var updateViewModel = new UpdateViewModel(_updateServiceMock.Object);

        return new MainViewModel(
            _audioServiceMock.Object,
            _devicePickerServiceMock.Object,
            updateViewModel,
            settingsViewModel,
            _settingsServiceMock.Object,
            _dispatcherServiceMock.Object,
            _messenger);
    }

    private static BluetoothDevice CreateDevice(string id = "1", string name = "iPhone")
    {
        return new BluetoothDevice { Name = name, Id = id, IsPhoneOrComputer = true };
    }

    private Task WaitForReconnectRetriesAsync(int retryCount)
    {
        return Task.Delay((MainViewModel.AutoReconnectDelayMs * retryCount) + 700);
    }

    /// <summary>
    /// Verifies default property values after construction.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultState()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.IsConnected);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("IDLE", viewModel.StatusText);
        Assert.Empty(viewModel.BluetoothDevices);
    }

    /// <summary>
    /// Verifies that refreshing populates the device collection.
    /// </summary>
    [Fact]
    public async Task RefreshDevices_PopulatesCollection()
    {
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(
            new[]
            {
                CreateDevice("1", "iPhone"),
                CreateDevice("2", "Laptop"),
            });

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        Assert.Equal(2, viewModel.BluetoothDevices.Count);
        Assert.Contains(viewModel.BluetoothDevices, device => device.Name == "iPhone");
        Assert.Contains(viewModel.BluetoothDevices, device => device.Name == "Laptop");
    }

    /// <summary>
    /// Verifies that the currently selected device is preserved across refreshes.
    /// </summary>
    [Fact]
    public async Task RefreshDevices_PreservesSelection()
    {
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(
            new[]
            {
                CreateDevice("1", "iPhone"),
                CreateDevice("2", "Laptop"),
            });

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        viewModel.SelectedBluetoothDevice = viewModel.BluetoothDevices.First(device => device.Id == "2");

        await viewModel.RefreshDevicesAsync();

        Assert.NotNull(viewModel.SelectedBluetoothDevice);
        Assert.Equal("2", viewModel.SelectedBluetoothDevice!.Id);
    }

    /// <summary>
    /// Verifies that a scan error is reported when device enumeration fails.
    /// </summary>
    [Fact]
    public async Task RefreshDevices_SetsErrorStatus_OnException()
    {
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ThrowsAsync(new Exception("fail"));

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        Assert.Equal("SCAN ERROR", viewModel.StatusText);
    }

    /// <summary>
    /// Verifies that refreshing devices does not persist settings.
    /// </summary>
    [Fact]
    public async Task RefreshDevices_DoesNotTriggerSettingSave()
    {
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { CreateDevice() });

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        _settingsServiceMock.Verify(service => service.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    /// <summary>
    /// Verifies that a successful connect performs the one-time startup-prime recycle before the route is marked active.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_RunsStartupPrimeAfterInitialSuccess()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
        _audioServiceMock.Verify(service => service.Disconnect("startup-prime", true), Times.Once);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(2));

        viewModel.Disconnect();
    }

    /// <summary>
    /// Verifies that the initial connect keeps retrying past the legacy retry cap until the route is restored.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_ContinuesPastLegacyRetryBudget_AndRecovers()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });

        var connectResults = new Queue<bool>(new[] { false, false, false, true, true });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(() => connectResults.Dequeue());

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(12));

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(5));
        _audioServiceMock.Verify(service => service.Disconnect("startup-prime", true), Times.Once);

        viewModel.Disconnect();
    }

    /// <summary>
    /// Verifies that a failed startup-prime recycle transitions into the sticky reconnect loop and still recovers.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_StartupPrimeFailure_EntersStickyReconnect_AndRecovers()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });

        var connectResults = new Queue<bool>(new[] { true, false, true });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(() => connectResults.Dequeue());

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.Disconnect("startup-prime", true), Times.Once);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(3));

        viewModel.Disconnect();
    }

    /// <summary>
    /// Verifies that a user disconnect cancels the pending sticky reconnect delay so no stale retry executes afterwards.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_DoesNotRetry_WhenUserDisconnectsDuringPendingRetryDelay()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.SetupSequence(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        var connectTask = viewModel.ConnectAsync();
        await Task.Delay(200);

        Assert.True(viewModel.DisconnectCommand.CanExecute(null));

        viewModel.Disconnect();
        await connectTask.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(viewModel.IsConnected);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("DISCONNECTED", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Once);
    }

    /// <summary>
    /// Verifies that connect does nothing when no device is selected.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_DoesNothing_WhenNoDeviceSelected()
    {
        var viewModel = CreateViewModel();

        await viewModel.ConnectAsync();

        Assert.False(viewModel.IsConnected);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that an exception during connect is reported as an error status.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SetsErrorStatus_OnException()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ThrowsAsync(new Exception("timeout"));

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.False(viewModel.IsConnected);
        Assert.StartsWith("ERROR:", viewModel.StatusText);
    }

    /// <summary>
    /// Verifies that disconnecting resets state and calls the audio service.
    /// </summary>
    [Fact]
    public async Task Disconnect_DisconnectsAndSetsStatus()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        viewModel.Disconnect();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("DISCONNECTED", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.Disconnect("user", false), Times.Once);
    }

    /// <summary>
    /// Verifies that disconnect clears a recoverable manual-reconnect prompt and returns the UI to a clean disconnected state.
    /// </summary>
    [Fact]
    public async Task Disconnect_ClearsRecoverableAudioLossState()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.SetupSequence(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        await viewModel.ReconnectAsync();

        viewModel.Disconnect();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("DISCONNECTED", viewModel.StatusText);
        Assert.True(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.ReconnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that manual reconnect is not a disconnected-state alias for full connect.
    /// </summary>
    [Fact]
    public async Task ReconnectAsync_DoesNothing_WhenNotConnectedOrRecoverable()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ReconnectAsync();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("IDLE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Never);
        _audioServiceMock.Verify(service => service.Disconnect("manual-recover", true), Times.Never);
    }

    /// <summary>
    /// Verifies that manual reconnect explicitly tears down the active route before reconnecting.
    /// </summary>
    [Fact]
    public async Task ReconnectAsync_CyclesConnection_WhenAlreadyConnected()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        await viewModel.ReconnectAsync();

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.Disconnect("manual-recover", true), Times.Once);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(3));

        viewModel.Disconnect();
    }

    /// <summary>
    /// Verifies that a failed manual reconnect falls back to the explicit recoverable reconnect state.
    /// </summary>
    [Fact]
    public async Task ReconnectAsync_FallsBackToRecoverableReconnect_WhenManualAttemptFails()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.SetupSequence(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        await viewModel.ReconnectAsync();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("AUDIO LOST - CLICK RECONNECT", viewModel.StatusText);
        Assert.False(viewModel.ConnectCommand.CanExecute(null));
        Assert.True(viewModel.ReconnectCommand.CanExecute(null));
        _audioServiceMock.Verify(service => service.Disconnect("manual-recover", true), Times.Once);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(3));
    }

    /// <summary>
    /// Verifies that connect is disabled when no device is selected.
    /// </summary>
    [Fact]
    public void CanConnect_FalseWhenNoDeviceSelected()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.ConnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that connect is disabled when the route is already active.
    /// </summary>
    [Fact]
    public async Task CanConnect_FalseWhenAlreadyConnected()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.False(viewModel.ConnectCommand.CanExecute(null));

        viewModel.Disconnect();
    }

    /// <summary>
    /// Verifies that disconnect is disabled when there is no active, recoverable, or reconnecting route.
    /// </summary>
    [Fact]
    public void CanDisconnect_FalseWhenNotConnected()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.DisconnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that manual reconnect remains unavailable when only a device is selected and no route exists yet.
    /// </summary>
    [Fact]
    public async Task CanReconnect_FalseWhenOnlyDeviceSelected()
    {
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { CreateDevice() });

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        Assert.False(viewModel.ReconnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that the Bluetooth settings command opens the device picker.
    /// </summary>
    [Fact]
    public async Task OpenBluetoothSettingsCommand_CallsDevicePickerService()
    {
        var viewModel = CreateViewModel();

        await viewModel.OpenBluetoothSettingsCommand.ExecuteAsync(null);

        _devicePickerServiceMock.Verify(service => service.ShowAsync(), Times.Once);
    }

    /// <summary>
    /// Verifies that the Open command raises RequestShow.
    /// </summary>
    [Fact]
    public void RequestShow_RaisedByOpenCommand()
    {
        var viewModel = CreateViewModel();
        var raised = false;
        viewModel.RequestShow += () => raised = true;

        viewModel.OpenCommand.Execute(null);

        Assert.True(raised);
    }

    /// <summary>
    /// Verifies that the Exit command raises RequestExit.
    /// </summary>
    [Fact]
    public void RequestExit_RaisedByExitCommand()
    {
        var viewModel = CreateViewModel();
        var raised = false;
        viewModel.RequestExit += () => raised = true;

        viewModel.ExitCommand.Execute(null);

        Assert.True(raised);
    }

    /// <summary>
    /// Verifies that the monitor keeps retrying until a real connection loss eventually recovers.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_RunsStickyReconnect_AndRecovers()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });

        var connectResults = new Queue<bool>(new[] { true, true, false, false, false, true });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(() => connectResults.Dequeue());
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(false);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        _audioServiceMock.Raise(service => service.ConnectionLost += null, EventArgs.Empty);
        await WaitForReconnectRetriesAsync(3);

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.Disconnect("service-connection-lost", false), Times.Once);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(6));

        viewModel.Disconnect();
    }

    /// <summary>
    /// Verifies that a real connection loss remains in reconnecting state until the user explicitly disconnects.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_StaysReconnectingUntilUserDisconnects()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.SetupSequence(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false)
            .ReturnsAsync(false)
            .ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(false);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        _audioServiceMock.Raise(service => service.ConnectionLost += null, EventArgs.Empty);
        await WaitForReconnectRetriesAsync(2);

        Assert.False(viewModel.IsConnected);
        Assert.Equal("RECONNECTING...", viewModel.StatusText);
        Assert.True(viewModel.DisconnectCommand.CanExecute(null));
        Assert.False(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.ReconnectCommand.CanExecute(null));

        viewModel.Disconnect();
        await Task.Delay(200);

        Assert.Equal("DISCONNECTED", viewModel.StatusText);
    }

    /// <summary>
    /// Verifies that a user disconnect during the service-loss reconnect delay cancels the pending reconnect cleanly.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_DoesNotThrow_WhenUserDisconnectsDuringPendingReconnectDelay()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(false);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        _audioServiceMock.Raise(service => service.ConnectionLost += null, EventArgs.Empty);
        viewModel.Disconnect();
        await Task.Delay(200);

        Assert.False(viewModel.IsConnected);
        Assert.Equal("DISCONNECTED", viewModel.StatusText);
        Assert.True(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.ReconnectCommand.CanExecute(null));
        _audioServiceMock.Verify(service => service.Disconnect("service-connection-lost", false), Times.Once);
        _audioServiceMock.Verify(service => service.Disconnect("user", false), Times.Once);
    }

    /// <summary>
    /// Verifies that a transient ConnectionLost event does not update the UI when the cross-check shows the route is still connected.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_DoesNotUpdateUi_WhenCrossCheckShowsStillConnected()
    {
        var device = CreateDevice();
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        _audioServiceMock.Raise(service => service.ConnectionLost += null, EventArgs.Empty);
        await Task.Delay(200);

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);

        viewModel.Disconnect();
    }

    /// <summary>
    /// Verifies that an available update is discovered and surfaced.
    /// </summary>
    [Fact]
    public async Task CheckForUpdate_FindsNewVersion()
    {
        var update = new UpdateInfo("v2.0.0", "2.0.0", "http://url", "Notes");
        _updateServiceMock.Setup(service => service.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);

        var viewModel = CreateViewModel();
        await viewModel.Updater.CheckForUpdateAsync();

        Assert.True(viewModel.Updater.UpdateAvailable);
    }

    /// <summary>
    /// Verifies that installing an update calls the update service.
    /// </summary>
    [Fact]
    public async Task InstallUpdate_CallsService()
    {
        var update = new UpdateInfo("v2.0.0", "2.0.0", "http://url", "Notes");
        _updateServiceMock.Setup(service => service.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);

        var viewModel = CreateViewModel();
        await viewModel.Updater.CheckForUpdateAsync();
        await viewModel.Updater.InstallUpdateAsync();

        _updateServiceMock.Verify(service => service.DownloadAndInstallAsync(update, It.IsAny<CancellationToken>()), Times.Once);
    }
}
