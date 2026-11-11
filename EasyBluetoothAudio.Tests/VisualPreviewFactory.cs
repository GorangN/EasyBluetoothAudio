using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.ViewModels;
using Moq;

namespace EasyBluetoothAudio.Tests;

internal static class VisualPreviewFactory
{
    internal static MainViewModel Create(bool includeSources = true)
    {
        var audio = new Mock<IAudioService>();
        BluetoothDevice[] devices =
        [
            new BluetoothDevice { Id = "preview-phone-a", Name = "Preview phone A", IsConnected = true },
            new BluetoothDevice { Id = "preview-phone-b", Name = "Preview phone B", IsConnected = true },
            new BluetoothDevice { Id = "preview-laptop", Name = "Preview laptop with a longer device name", IsConnected = true }
        ];
        audio.Setup(service => service.GetBluetoothDevicesAsync()).ReturnsAsync(includeSources ? devices : []);
        audio.Setup(service => service.ConnectBluetoothAudioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.Connected);
        audio.Setup(service => service.ConnectBluetoothAudioAsync("preview-laptop", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AudioConnectionResult.DeniedBySystem);
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(service => service.Invoke(It.IsAny<Action>())).Callback<Action>(action => action());
        var preferences = new AppSettings { AutoConnect = false };
        var settings = new Mock<ISettingsService>();
        settings.Setup(service => service.Load()).Returns(() => preferences);
        settings.Setup(service => service.Save(It.IsAny<AppSettings>())).Callback<AppSettings>(saved => preferences = saved);
        var messenger = new WeakReferenceMessenger();
        var model = new MainViewModel(audio.Object, Mock.Of<IDevicePickerService>(),
            new UpdateViewModel(Mock.Of<IUpdateService>()),
            new SettingsViewModel(settings.Object, Mock.Of<IStartupService>(), Mock.Of<IBluetoothQualityService>(), messenger),
            settings.Object, dispatcher.Object, messenger,
            device => new DeviceConnectionViewModel(device, audio.Object, dispatcher.Object, messenger));
        model.InitializeAsync().GetAwaiter().GetResult();
        if (includeSources)
        {
            model.Sources[0].ConnectAsync().GetAwaiter().GetResult();
            model.Sources[2].ConnectAsync().GetAwaiter().GetResult();
        }
        return model;
    }
}
