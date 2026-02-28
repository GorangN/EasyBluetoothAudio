using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Tests for <see cref="MainViewModel"/> covering device refresh, connection lifecycle, and command states.
/// </summary>
public class MainViewModelTests
{
    private readonly Mock<IAudioService> _audioServiceMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IStartupService> _startupServiceMock;
    private readonly IMessenger _messenger;

    /// <summary>
    /// Initializes shared mocks and a fresh messenger for each test.
    /// </summary>
    public MainViewModelTests()
    {
        _audioServiceMock = new Mock<IAudioService>();
        _processServiceMock = new Mock<IProcessService>();
        _updateServiceMock = new Mock<IUpdateService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _startupServiceMock = new Mock<IStartupService>();
        _messenger = new WeakReferenceMessenger();

        _settingsServiceMock.Setup(s => s.Load()).Returns(new AppSettings());
    }

    private MainViewModel CreateViewModel()
    {
        var settingsVm = new SettingsViewModel(_settingsServiceMock.Object, _startupServiceMock.Object, _messenger);
        var updateVm = new UpdateViewModel(_updateServiceMock.Object);
        return new MainViewModel(
            _audioServiceMock.Object,
            _processServiceMock.Object,
            updateVm,
            settingsVm,
            _settingsServiceMock.Object,
            _messenger);
    }

    /// <summary>
    /// Verifies default property values after construction.
    /// </summary>
    [Fact]
    public void Constructor_SetsDefaultState()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsConnected);
        Assert.False(vm.IsBusy);
        Assert.Equal("IDLE", vm.StatusText);
        Assert.Empty(vm.BluetoothDevices);
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
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();

        Assert.Equal(2, vm.BluetoothDevices.Count);
        Assert.Contains(vm.BluetoothDevices, d => d.Name == "iPhone");
        Assert.Contains(vm.BluetoothDevices, d => d.Name == "Laptop");
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
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        vm.SelectedBluetoothDevice = vm.BluetoothDevices.First(d => d.Id == "2");

        await vm.RefreshDevicesAsync();

        Assert.NotNull(vm.SelectedBluetoothDevice);
        Assert.Equal("2", vm.SelectedBluetoothDevice!.Id);
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
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();

        Assert.NotNull(vm.SelectedBluetoothDevice);
        Assert.Equal("1", vm.SelectedBluetoothDevice!.Id);
    }

    /// <summary>
    /// Verifies that a scan error is reported when device enumeration fails.
    /// </summary>
    [Fact]
    public async Task RefreshDevices_SetsErrorStatus_OnException()
    {
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ThrowsAsync(new Exception("fail"));

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();

        Assert.Equal("SCAN ERROR", vm.StatusText);
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
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(devices);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();

        _settingsServiceMock.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    /// <summary>
    /// Verifies that a successful connection sets status and connected state.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SetsStatusAndIsConnected_OnSuccess()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        Assert.True(vm.IsConnected);
        Assert.Equal("STREAMING ACTIVE", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    /// <summary>
    /// Verifies behavior when the audio service reports connection failure.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SetsErrorStatus_OnConnectionFailure()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(false);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        Assert.False(vm.IsConnected);
        Assert.Equal("WAITING FOR SOURCE...", vm.StatusText);
    }

    /// <summary>
    /// Verifies that connect does nothing when no device is selected.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_DoesNothing_WhenNoDeviceSelected()
    {
        var vm = CreateViewModel();

        await vm.ConnectAsync();

        Assert.False(vm.IsConnected);
        _audioServiceMock.Verify(s => s.ConnectBluetoothAudioAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that an exception during connect is reported as an error status.
    /// </summary>
    [Fact]
    public async Task ConnectAsync_SetsErrorStatus_OnException()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ThrowsAsync(new Exception("timeout"));

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        Assert.False(vm.IsConnected);
        Assert.StartsWith("ERROR:", vm.StatusText);
    }

    /// <summary>
    /// Verifies that disconnecting resets state and calls the audio service.
    /// </summary>
    [Fact]
    public async Task Disconnect_DisconnectsAndSetsStatus()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        vm.Disconnect();

        Assert.False(vm.IsConnected);
        Assert.Equal("DISCONNECTED", vm.StatusText);
        _audioServiceMock.Verify(s => s.Disconnect(), Times.Once);
    }

    /// <summary>
    /// Verifies that connect is disabled when no device is selected.
    /// </summary>
    [Fact]
    public void CanConnect_FalseWhenNoDeviceSelected()
    {
        var vm = CreateViewModel();

        Assert.False(vm.ConnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that connect is disabled when already connected.
    /// </summary>
    [Fact]
    public async Task CanConnect_FalseWhenAlreadyConnected()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        Assert.False(vm.ConnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that disconnect is disabled when not connected.
    /// </summary>
    [Fact]
    public void CanDisconnect_FalseWhenNotConnected()
    {
        var vm = CreateViewModel();

        Assert.False(vm.DisconnectCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that the Bluetooth settings command opens the correct URI.
    /// </summary>
    [Fact]
    public void OpenBluetoothSettingsCommand_CallsProcessService()
    {
        var vm = CreateViewModel();

        vm.OpenBluetoothSettingsCommand.Execute(null);

        _processServiceMock.Verify(p => p.OpenUri("ms-settings:bluetooth"), Times.Once);
    }

    /// <summary>
    /// Verifies that StatusText raises PropertyChanged.
    /// </summary>
    [Fact]
    public void StatusText_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        string? raisedProperty = null;
        vm.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        vm.StatusText = "TESTING";

        Assert.Equal("StatusText", raisedProperty);
        Assert.Equal("TESTING", vm.StatusText);
    }

    /// <summary>
    /// Verifies that SelectedBluetoothDevice raises PropertyChanged.
    /// </summary>
    [Fact]
    public void SelectedBluetoothDevice_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        string? raisedProperty = null;
        vm.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        vm.SelectedBluetoothDevice = new BluetoothDevice { Name = "Test", Id = "1" };

        Assert.Equal("SelectedBluetoothDevice", raisedProperty);
    }

    /// <summary>
    /// Verifies that the Open command raises RequestShow.
    /// </summary>
    [Fact]
    public void RequestShow_RaisedByOpenCommand()
    {
        var vm = CreateViewModel();
        bool raised = false;
        vm.RequestShow += () => raised = true;

        vm.OpenCommand.Execute(null);

        Assert.True(raised);
    }

    /// <summary>
    /// Verifies that the Exit command raises RequestExit.
    /// </summary>
    [Fact]
    public void RequestExit_RaisedByExitCommand()
    {
        var vm = CreateViewModel();
        bool raised = false;
        vm.RequestExit += () => raised = true;

        vm.ExitCommand.Execute(null);

        Assert.True(raised);
    }

    /// <summary>
    /// Verifies that the connection monitor detects a disconnection and sets reconnecting status.
    /// </summary>
    [Fact]
    public async Task Monitor_DetectsDisconnectionAndSetsReconnecting()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var callCount = 0;
        _audioServiceMock.Setup(s => s.IsBluetoothDeviceConnectedAsync("1"))
            .ReturnsAsync(() =>
            {
                callCount++;
                return false;
            });

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        await Task.Delay(6500);

        Assert.Equal("RECONNECTING...", vm.StatusText);
        Assert.False(vm.IsConnected);

        vm.Disconnect();
    }

    /// <summary>
    /// Verifies that the connection monitor stops polling after disconnect.
    /// </summary>
    [Fact]
    public async Task Monitor_StopsOnDisconnect()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(s => s.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(true);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        vm.Disconnect();

        await Task.Delay(6000);

        _audioServiceMock.Verify(s => s.IsBluetoothDeviceConnectedAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the connection monitor successfully reconnects.
    /// </summary>
    [Fact]
    public async Task Monitor_ReconnectSucceeds_ResumesStreaming()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);

        var callCount = 0;
        _audioServiceMock.Setup(s => s.IsBluetoothDeviceConnectedAsync("1"))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount > 1;
            });

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        await Task.Delay(12000);

        Assert.True(vm.IsConnected);
        Assert.Equal("STREAMING ACTIVE", vm.StatusText);

        vm.Disconnect();
    }

    /// <summary>
    /// Verifies that an available update is discovered and surfaced.
    /// </summary>
    [Fact]
    public async Task CheckForUpdate_FindsNewVersion()
    {
        var update = new UpdateInfo("v2.0.0", "2.0.0", "http://url", "Notes");
        _updateServiceMock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);

        var vm = CreateViewModel();
        await vm.Updater.CheckForUpdateAsync();

        Assert.True(vm.Updater.UpdateAvailable);
    }

    /// <summary>
    /// Verifies that installing an update calls the update service.
    /// </summary>
    [Fact]
    public async Task InstallUpdate_CallsService()
    {
        var update = new UpdateInfo("v2.0.0", "2.0.0", "http://url", "Notes");
        _updateServiceMock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);

        var vm = CreateViewModel();
        await vm.Updater.CheckForUpdateAsync();
        
        await vm.Updater.InstallUpdateAsync();

        _updateServiceMock.Verify(s => s.DownloadAndInstallAsync(update, It.IsAny<CancellationToken>()), Times.Once);
    }
}
