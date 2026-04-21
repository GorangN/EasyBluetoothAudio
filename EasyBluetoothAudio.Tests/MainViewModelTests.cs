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
/// Tests for <see cref="MainViewModel"/> covering device refresh, connection lifecycle, and command states.
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
        var devices = new List<BluetoothDevice>
        {
            new() { Name = "iPhone", Id = "1", IsPhoneOrComputer = true },
            new() { Name = "Laptop", Id = "2", IsPhoneOrComputer = true }
        };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

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
        var devices = new List<BluetoothDevice>
        {
            new() { Name = "iPhone", Id = "1" },
            new() { Name = "Laptop", Id = "2" }
        };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        viewModel.SelectedBluetoothDevice = viewModel.BluetoothDevices.First(device => device.Id == "2");

        await viewModel.RefreshDevicesAsync();

        Assert.NotNull(viewModel.SelectedBluetoothDevice);
        Assert.Equal("2", viewModel.SelectedBluetoothDevice!.Id);
    }

    /// <summary>
    /// Verifies that the first device is auto-selected when no prior selection exists.
    /// </summary>
    [Fact]
    public async Task RefreshDevices_SelectsFirst_WhenNoPreviousSelection()
    {
        var devices = new List<BluetoothDevice>
        {
            new() { Name = "iPhone", Id = "1" },
            new() { Name = "Laptop", Id = "2" }
        };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        Assert.NotNull(viewModel.SelectedBluetoothDevice);
        Assert.Equal("1", viewModel.SelectedBluetoothDevice!.Id);
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
        var devices = new List<BluetoothDevice>
        {
            new() { Name = "iPhone", Id = "1" },
            new() { Name = "Laptop", Id = "2" }
        };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();

        _settingsServiceMock.Verify(service => service.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    /// <summary>
    /// Verifies that a successful connection sets status and connected state.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SetsStatusAndIsConnected_OnSuccess()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Verifies that a failed initial connect uses the bounded retry budget and then falls back to manual reconnect.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_UsesBoundedRetries_AndFallsBackToManualReconnect()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("1")).ReturnsAsync(false);

        var balloonCount = 0;
        _messenger.Register<ShowBalloonRequestedMessage>(this, (_, _) => balloonCount++);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("AUDIO LOST - CLICK CONNECT", viewModel.StatusText);
        Assert.True(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.ReconnectCommand.CanExecute(null));
        Assert.Equal(1, balloonCount);
        _audioServiceMock.Verify(
            service => service.ConnectBluetoothAudioAsync("1"),
            Times.Exactly(MainViewModel.AutoReconnectAttemptLimit + 1));
    }

    /// <summary>
    /// Verifies that a failed initial connect falls back to manual reconnect when the physical
    /// Bluetooth link is still present.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_FallsBackToReconnect_WhenPhysicalLinkRemains()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("AUDIO LOST - CLICK RECONNECT", viewModel.StatusText);
        Assert.False(viewModel.ConnectCommand.CanExecute(null));
        Assert.True(viewModel.ReconnectCommand.CanExecute(null));
        Assert.True(viewModel.DisconnectCommand.CanExecute(null));
        _audioServiceMock.Verify(
            service => service.ConnectBluetoothAudioAsync("1"),
            Times.Exactly(MainViewModel.AutoReconnectAttemptLimit + 1));
    }

    /// <summary>
    /// Verifies that a failed initial connect can recover on a bounded automatic retry.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_RetriesAfterInitialFailure_AndRecovers()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });

        var connectResults = new Queue<bool>(new[] { false, true });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(() => connectResults.Dequeue());

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(2));
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
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
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
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        viewModel.Disconnect();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("DISCONNECTED", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.Disconnect(It.IsAny<string>()), Times.Once);
    }

    /// <summary>
    /// Verifies that disconnect clears a recoverable audio-loss fallback and returns the UI to
    /// a clean disconnected state.
    /// </summary>
    [Fact]
    public async Task Disconnect_ClearsRecoverableAudioLossState()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        viewModel.Disconnect();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("DISCONNECTED", viewModel.StatusText);
        Assert.True(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.ReconnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that manual reconnect is no longer a disconnected-state alias for full connect.
    /// </summary>
    [Fact]
    public async Task ReconnectAsync_DoesNothing_WhenNotConnectedOrRecoverable()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ReconnectAsync();

        Assert.False(viewModel.IsConnected);
        Assert.Equal("IDLE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Never);
        _audioServiceMock.Verify(service => service.Disconnect("manual-recover", true), Times.Never);
    }

    /// <summary>
    /// Verifies that manual reconnect performs only one reconnect attempt after a recoverable
    /// audio-loss fallback, rather than starting another automatic retry loop.
    /// </summary>
    [Fact]
    public async Task ReconnectAsync_AttemptsOneShotReconnect_WhenLossIsRecoverable()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.SetupSequence(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(false)
            .ReturnsAsync(false)
            .ReturnsAsync(false)
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
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(4));
        _audioServiceMock.Verify(service => service.Disconnect("manual-recover", true), Times.Never);
    }

    /// <summary>
    /// Verifies that manual reconnect explicitly tears down the active route before reconnecting.
    /// </summary>
    [Fact]
    public async Task ReconnectAsync_CyclesConnection_WhenAlreadyConnected()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        await viewModel.ReconnectAsync();

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.Disconnect("manual-recover", true), Times.Once);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(2));
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
    /// Verifies that connect is disabled when already connected.
    /// </summary>
    [Fact]
    public async Task CanConnect_FalseWhenAlreadyConnected()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        Assert.False(viewModel.ConnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that disconnect is disabled when not connected.
    /// </summary>
    [Fact]
    public void CanDisconnect_FalseWhenNotConnected()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.DisconnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that manual reconnect stays unavailable when only a device is selected and no
    /// active or recoverable audio route exists yet.
    /// </summary>
    [Fact]
    public async Task CanReconnect_FalseWhenOnlyDeviceSelected()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });

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
    /// Verifies that StatusText raises PropertyChanged.
    /// </summary>
    [Fact]
    public void StatusText_RaisesPropertyChanged()
    {
        var viewModel = CreateViewModel();
        string? raisedProperty = null;
        viewModel.PropertyChanged += (_, args) => raisedProperty = args.PropertyName;

        viewModel.StatusText = "TESTING";

        Assert.Equal("StatusText", raisedProperty);
        Assert.Equal("TESTING", viewModel.StatusText);
    }

    /// <summary>
    /// Verifies that SelectedBluetoothDevice raises PropertyChanged.
    /// </summary>
    [Fact]
    public void SelectedBluetoothDevice_RaisesPropertyChanged()
    {
        var viewModel = CreateViewModel();
        string? raisedProperty = null;
        viewModel.PropertyChanged += (_, args) => raisedProperty = args.PropertyName;

        viewModel.SelectedBluetoothDevice = new BluetoothDevice { Name = "Test", Id = "1" };

        Assert.Equal("SelectedBluetoothDevice", raisedProperty);
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
    /// Verifies that the monitor successfully reconnects after a real connection-lost event.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_RunsBoundedReconnect_AndRecovers()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });

        var connectResults = new Queue<bool>(new[] { true, true });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(() => connectResults.Dequeue());
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(false);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        _audioServiceMock.Raise(service => service.ConnectionLost += null, EventArgs.Empty);
        await Task.Delay(MainViewModel.AutoReconnectDelayMs + 500);

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.Disconnect("service-connection-lost"), Times.Once);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that the monitor falls back to manual reconnect after the bounded retry budget is
    /// exhausted while the physical Bluetooth link remains present.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_FallsBackToReconnect_WhenPhysicalLinkRemains()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.SetupSequence(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(true)
            .ReturnsAsync(false)
            .ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("1")).ReturnsAsync(true);

        var balloonCount = 0;
        _messenger.Register<ShowBalloonRequestedMessage>(this, (_, _) => balloonCount++);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        _audioServiceMock.Raise(service => service.ConnectionLost += null, EventArgs.Empty);
        await Task.Delay((MainViewModel.AutoReconnectDelayMs * MainViewModel.AutoReconnectAttemptLimit) + 1000);
        await Task.Delay(MainViewModel.MonitorPollDelayMs + 500);

        Assert.False(viewModel.IsConnected);
        Assert.Equal("AUDIO LOST - CLICK RECONNECT", viewModel.StatusText);
        Assert.False(viewModel.ConnectCommand.CanExecute(null));
        Assert.True(viewModel.ReconnectCommand.CanExecute(null));
        Assert.Equal(1, balloonCount);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(3));
    }

    /// <summary>
    /// Verifies that the monitor falls back to full connect after the bounded retry budget is
    /// exhausted and the physical Bluetooth link is no longer present.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_FallsBackToConnect_WhenPhysicalLinkIsGone()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.SetupSequence(service => service.ConnectBluetoothAudioAsync("1"))
            .ReturnsAsync(true)
            .ReturnsAsync(false)
            .ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(false);
        _audioServiceMock.Setup(service => service.IsBluetoothPhysicallyConnectedAsync("1")).ReturnsAsync(false);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        _audioServiceMock.Raise(service => service.ConnectionLost += null, EventArgs.Empty);
        await Task.Delay((MainViewModel.AutoReconnectDelayMs * MainViewModel.AutoReconnectAttemptLimit) + 1000);
        await Task.Delay(MainViewModel.MonitorPollDelayMs + 500);

        Assert.False(viewModel.IsConnected);
        Assert.Equal("AUDIO LOST - CLICK CONNECT", viewModel.StatusText);
        Assert.True(viewModel.ConnectCommand.CanExecute(null));
        Assert.False(viewModel.ReconnectCommand.CanExecute(null));
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Exactly(3));
    }

    /// <summary>
    /// Verifies that a user disconnect during the service-loss reconnect delay cancels the pending
    /// reconnect without surfacing an unhandled cancellation from the async event handler.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_DoesNotThrow_WhenUserDisconnectsDuringPendingReconnectDelay()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
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
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Once);
        _audioServiceMock.Verify(service => service.Disconnect("service-connection-lost"), Times.Once);
        _audioServiceMock.Verify(service => service.Disconnect("user"), Times.Once);
    }

    /// <summary>
    /// Verifies that the monitor does not perform hidden idle-time recycling while the device stays connected.
    /// </summary>
    [Fact]
    public async Task Monitor_DoesNothingDuringIdle_WhenConnectionRemainsHealthy()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();
        await Task.Delay(MainViewModel.MonitorPollDelayMs + 500);

        Assert.True(viewModel.IsConnected);
        Assert.Equal("STREAMING ACTIVE", viewModel.StatusText);
        _audioServiceMock.Verify(service => service.ConnectBluetoothAudioAsync("1"), Times.Once);
    }

    /// <summary>
    /// Verifies that the connection monitor stops polling after disconnect.
    /// </summary>
    [Fact]
    public async Task Monitor_StopsOnDisconnect()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(service => service.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(service => service.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(true);

        var viewModel = CreateViewModel();
        await viewModel.RefreshDevicesAsync();
        await viewModel.ConnectAsync();

        viewModel.Disconnect();
        await Task.Delay(MainViewModel.MonitorPollDelayMs + 500);

        _audioServiceMock.Verify(service => service.IsBluetoothDeviceConnectedAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that a transient ConnectionLost event does not update the UI when the cross-check
    /// confirms the device is still connected.
    /// </summary>
    [Fact]
    public async Task ConnectionLost_Event_DoesNotUpdateUi_WhenCrossCheckShowsStillConnected()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
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
