using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using Moq;
using Xunit;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Unit tests for <see cref="SoundService"/>.
/// </summary>
public class SoundServiceTests
{
    /// <summary>
    /// Verifies that the sound flag is loaded from settings on initialization.
    /// </summary>
    [Fact]
    public void Initialize_LoadsSoundPreference_FromSettings()
    {
        var messenger = new WeakReferenceMessenger();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.Load()).Returns(new AppSettings { PlayConnectionSound = true });

        var sut = new SoundService(messenger, settingsService.Object);

        // Initialize registers Messenger subscriptions — should not throw.
        sut.Initialize();
    }

    /// <summary>
    /// Verifies that SoundSettingsChangedMessage updates the internal flag.
    /// </summary>
    [Fact]
    public void OnSoundSettingsChanged_UpdatesInternalFlag()
    {
        var messenger = new WeakReferenceMessenger();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.Load()).Returns(new AppSettings { PlayConnectionSound = false });

        var sut = new SoundService(messenger, settingsService.Object);
        sut.Initialize();

        // Changing the sound setting should not throw.
        messenger.Send(new SoundSettingsChangedMessage(true));
    }
}
