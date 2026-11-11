using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using Moq;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Simulates a Windows transport that displaces one route whenever the other source opens.
/// </summary>
public class DeviceTransportContentionTests
{
    /// <summary>
    /// Repeated OS route displacement must eventually yield to manual recovery instead of reopening forever.
    /// </summary>
    [Fact]
    public async Task AutomaticRecovery_StopsRepeatedAlternatingTransportDisplacement()
    {
        var audio = new Mock<IAudioService>();
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        string? activeTransport = null;
        var openAttempts = 0;
        audio.Setup(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((deviceId, _) =>
            {
                activeTransport = deviceId;
                openAttempts++;
                return Task.FromResult(AudioConnectionResult.Connected);
            });
        audio.Setup(service => service.IsBluetoothDeviceConnectedAsync(It.IsAny<string>()))
            .Returns<string>(deviceId => Task.FromResult(activeTransport == deviceId));
        audio.Setup(service => service.IsBluetoothPhysicallyConnectedAsync(It.IsAny<string>())).ReturnsAsync(true);
        audio.Setup(service => service.Disconnect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, string, bool>((deviceId, _, _) =>
            {
                if (activeTransport == deviceId)
                {
                    activeTransport = null;
                }
            });
        using var first = new DeviceConnectionViewModel(
            new BluetoothDevice { Id = "A", Name = "First phone", IsConnected = true },
            audio.Object, dispatcher.Object, new WeakReferenceMessenger());
        using var second = new DeviceConnectionViewModel(
            new BluetoothDevice { Id = "B", Name = "Second source", IsConnected = true },
            audio.Object, dispatcher.Object, new WeakReferenceMessenger());
        await first.ConnectAsync();
        await second.ConnectAsync();
        openAttempts = 0;

        for (var interruption = 0; interruption < 12; interruption++)
        {
            var displacedDevice = activeTransport == "A" ? "B" : "A";
            audio.Raise(service => service.ConnectionLost += null, new DeviceConnectionLostEventArgs(displacedDevice));
        }

        Assert.Equal(2 * DeviceConnectionOperation.AutomaticRecoveryLimit, openAttempts);
        Assert.False(first.IsConnected);
        Assert.True(first.ReconnectCommand.CanExecute(null));
        Assert.Contains("interrupted repeatedly", first.ErrorText);
        Assert.True(second.IsConnected);
    }
}
