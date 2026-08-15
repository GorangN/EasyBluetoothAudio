using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Tests for <see cref="AudioService"/> selector-based Bluetooth presence checks.
/// </summary>
public class AudioServiceTests
{
    private readonly Mock<IDispatcherService> _dispatcherServiceMock = new();

    /// <summary>
    /// Verifies that a recent real disconnect receives the remainder of the configured settle window.
    /// </summary>
    [Fact]
    public void CalculateRemainingSettleDelay_ReturnsRemainingWindow_AfterRecentDisconnect()
    {
        var lastDisconnectTime = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);
        var currentTime = lastDisconnectTime.AddMilliseconds(1_250);

        var delay = AudioService.CalculateRemainingSettleDelay(lastDisconnectTime, currentTime);

        Assert.Equal(AudioService.SettleDelayMs - 1_250, delay);
    }

    /// <summary>
    /// Verifies that no settle delay remains once Windows has received the full release window.
    /// </summary>
    [Fact]
    public void CalculateRemainingSettleDelay_ReturnsZero_AfterSettleWindowElapsed()
    {
        var lastDisconnectTime = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);
        var currentTime = lastDisconnectTime.AddMilliseconds(AudioService.SettleDelayMs);

        var delay = AudioService.CalculateRemainingSettleDelay(lastDisconnectTime, currentTime);

        Assert.Equal(0, delay);
    }

    /// <summary>
    /// Verifies that selector presence is treated as a physical Bluetooth connection.
    /// </summary>
    [Fact]
    public async Task IsBluetoothPhysicallyConnectedAsync_ReturnsTrue_WhenDeviceAppearsInSelectorSnapshot()
    {
        var service = new TestAudioService(
            _dispatcherServiceMock.Object,
            [(@"BTHHFENUM\DEV_00112233\SNK", "iPhone")]);

        var isConnected = await service.IsBluetoothPhysicallyConnectedAsync(@"BTHHFENUM\DEV_00112233\SNK");

        Assert.True(isConnected);
    }

    /// <summary>
    /// Verifies that selector absence is treated as a missing physical Bluetooth connection.
    /// </summary>
    [Fact]
    public async Task IsBluetoothPhysicallyConnectedAsync_ReturnsFalse_WhenDeviceIsMissingFromSelectorSnapshot()
    {
        var service = new TestAudioService(
            _dispatcherServiceMock.Object,
            [(@"BTHHFENUM\DEV_00112233\SNK", "iPhone")]);

        var isConnected = await service.IsBluetoothPhysicallyConnectedAsync(@"BTHHFENUM\DEV_99887766\SNK");

        Assert.False(isConnected);
    }

    /// <summary>
    /// Verifies that discovered selector devices are surfaced as connected sources.
    /// </summary>
    [Fact]
    public async Task GetBluetoothDevicesAsync_MarksSelectorDevicesAsConnected()
    {
        var service = new TestAudioService(
            _dispatcherServiceMock.Object,
            [(@"BTHHFENUM\DEV_00112233\SNK", "iPhone")]);

        var devices = (await service.GetBluetoothDevicesAsync()).ToList();

        var device = Assert.Single(devices);
        Assert.Equal(@"BTHHFENUM\DEV_00112233\SNK", device.Id);
        Assert.Equal("iPhone", device.Name);
        Assert.True(device.IsConnected);
    }

}
