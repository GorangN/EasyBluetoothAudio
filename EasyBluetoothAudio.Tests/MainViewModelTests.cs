using System.Threading;
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

    public MainViewModelTests()
    {
        _audioServiceMock = new Mock<IAudioService>();
        _processServiceMock = new Mock<IProcessService>();
        _updateServiceMock = new Mock<IUpdateService>();
    }

    private MainViewModel CreateViewModel() => new(_audioServiceMock.Object, _processServiceMock.Object, _updateServiceMock.Object);

    [Fact]
    public void Constructor_SetsDefaultState()
    {
        var vm = CreateViewModel();

        Assert.False(vm.IsConnected);
        Assert.False(vm.IsBusy);
        Assert.Equal("IDLE", vm.StatusText);
        Assert.Equal(45, vm.BufferMs);
        Assert.Empty(vm.BluetoothDevices);
    }

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

    [Fact]
    public async Task RefreshDevices_SetsErrorStatus_OnException()
    {
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ThrowsAsync(new Exception("fail"));

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();

        Assert.Equal("SCAN ERROR", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_SetsStatusAndIsConnected_OnSuccess()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(s => s.StartRoutingAsync("iPhone", null, 40)).Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        Assert.True(vm.IsConnected);
        Assert.Equal("STREAMING ACTIVE", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

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
        Assert.Equal("BT CONNECT FAILED", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_DoesNothing_WhenNoDeviceSelected()
    {
        var vm = CreateViewModel();

        await vm.ConnectAsync();

        Assert.False(vm.IsConnected);
        _audioServiceMock.Verify(s => s.ConnectBluetoothAudioAsync(It.IsAny<string>()), Times.Never);
    }

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

    [Fact]
    public async Task Disconnect_StopsRoutingAndSetsStatus()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(s => s.StartRoutingAsync("iPhone", null, 40)).Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        vm.Disconnect();

        Assert.False(vm.IsConnected);
        Assert.Equal("DISCONNECTED", vm.StatusText);
        _audioServiceMock.Verify(s => s.StopRouting(), Times.Once);
    }

    [Fact]
    public void CanConnect_FalseWhenNoDeviceSelected()
    {
        var vm = CreateViewModel();

        Assert.False(vm.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public async Task CanConnect_FalseWhenAlreadyConnected()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(s => s.StartRoutingAsync("iPhone", null, 40)).Returns(Task.CompletedTask);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        Assert.False(vm.ConnectCommand.CanExecute(null));
    }

    [Fact]
    public void CanDisconnect_FalseWhenNotConnected()
    {
        var vm = CreateViewModel();

        Assert.False(vm.DisconnectCommand.CanExecute(null));
    }

    [Fact]
    public void OpenBluetoothSettingsCommand_CallsProcessService()
    {
        var vm = CreateViewModel();

        vm.OpenBluetoothSettingsCommand.Execute(null);

        _processServiceMock.Verify(p => p.OpenUri("ms-settings:bluetooth"), Times.Once);
    }

    [Fact]
    public void BufferMs_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        string? raisedProperty = null;
        vm.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        vm.BufferMs = 100;

        Assert.Equal("BufferMs", raisedProperty);
        Assert.Equal(100, vm.BufferMs);
    }

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

    [Fact]
    public void SelectedBluetoothDevice_RaisesPropertyChanged()
    {
        var vm = CreateViewModel();
        string? raisedProperty = null;
        vm.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        vm.SelectedBluetoothDevice = new BluetoothDevice { Name = "Test", Id = "1" };

        Assert.Equal("SelectedBluetoothDevice", raisedProperty);
    }

    [Fact]
    public void RequestShow_RaisedByOpenCommand()
    {
        var vm = CreateViewModel();
        bool raised = false;
        vm.RequestShow += () => raised = true;

        vm.OpenCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public void RequestExit_RaisedByExitCommand()
    {
        var vm = CreateViewModel();
        bool raised = false;
        vm.RequestExit += () => raised = true;

        vm.ExitCommand.Execute(null);

        Assert.True(raised);
    }

    [Fact]
    public async Task Monitor_DetectsDisconnectionAndSetsReconnecting()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(s => s.StartRoutingAsync("iPhone", null, 40)).Returns(Task.CompletedTask);

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

    [Fact]
    public async Task Monitor_StopsOnDisconnect()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(s => s.StartRoutingAsync("iPhone", null, 40)).Returns(Task.CompletedTask);
        _audioServiceMock.Setup(s => s.IsBluetoothDeviceConnectedAsync("1")).ReturnsAsync(true);

        var vm = CreateViewModel();
        await vm.RefreshDevicesAsync();
        await vm.ConnectAsync();

        vm.Disconnect();

        await Task.Delay(6000);

        _audioServiceMock.Verify(s => s.IsBluetoothDeviceConnectedAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Monitor_ReconnectSucceeds_ResumesStreaming()
    {
        var device = new BluetoothDevice { Name = "iPhone", Id = "1" };
        _audioServiceMock.Setup(s => s.GetBluetoothDevicesAsync()).ReturnsAsync(new[] { device });
        _audioServiceMock.Setup(s => s.ConnectBluetoothAudioAsync("1")).ReturnsAsync(true);
        _audioServiceMock.Setup(s => s.StartRoutingAsync("iPhone", null, 40)).Returns(Task.CompletedTask);

        var callCount = 0;
        _audioServiceMock.Setup(s => s.IsBluetoothDeviceConnectedAsync("1"))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call: disconnected; second call onwards: reconnected
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

    [Fact]
    public async Task CheckForUpdate_FindsNewVersion()
    {
        var update = new UpdateInfo("v2.0.0", "2.0.0", "http://url", "Notes");
        _updateServiceMock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);

        var vm = CreateViewModel();
        await vm.CheckForUpdateAsync();

        Assert.True(vm.UpdateAvailable);
        Assert.Equal("UPDATE AVAILABLE", vm.StatusText);
    }

    [Fact]
    public async Task InstallUpdate_CallsService()
    {
        var update = new UpdateInfo("v2.0.0", "2.0.0", "http://url", "Notes");
        _updateServiceMock.Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(update);

        var vm = CreateViewModel();
        await vm.CheckForUpdateAsync();
        
        await vm.InstallUpdateAsync();

        _updateServiceMock.Verify(s => s.DownloadAndInstallAsync(update, It.IsAny<CancellationToken>()), Times.Once);
    }
}
