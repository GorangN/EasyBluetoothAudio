using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.ViewModels;
using Moq;
using Xunit;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Unit tests for <see cref="SettingsViewModel"/>.
/// </summary>
public class SettingsViewModelTests
{
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly Mock<IStartupService> _startupService = new();
    private readonly IMessenger _messenger = new WeakReferenceMessenger();

    private SettingsViewModel CreateSut(AppSettings? settings = null)
    {
        _settingsService.Setup(s => s.Load()).Returns(settings ?? new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var vm = new SettingsViewModel(_settingsService.Object, _startupService.Object, _messenger);
        vm.Initialize();
        return vm;
    }

    /// <summary>
    /// Verifies that Initialize loads settings from the service.
    /// </summary>
    [Fact]
    public void Initialize_LoadsSettingsFromService()
    {
        var settings = new AppSettings { AutoConnect = true };
        var sut = CreateSut(settings);

        Assert.True(sut.AutoConnect);
    }

    /// <summary>
    /// Verifies that AutoStartOnStartup is read from the startup service.
    /// </summary>
    [Fact]
    public void Initialize_ReadsAutoStartFromStartupService()
    {
        _startupService.Setup(s => s.IsEnabled).Returns(true);
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());

        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object, _messenger);
        sut.Initialize();

        Assert.True(sut.AutoStartOnStartup);
    }

    /// <summary>
    /// Verifies that AutoConnect raises PropertyChanged.
    /// </summary>
    [Fact]
    public void AutoConnect_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        string? raised = null;
        sut.PropertyChanged += (_, e) => raised = e.PropertyName;

        sut.AutoConnect = true;

        Assert.Equal(nameof(sut.AutoConnect), raised);
    }

    /// <summary>
    /// Verifies that SaveCommand persists settings and publishes Messenger messages.
    /// </summary>
    [Fact]
    public void SaveCommand_PersistsSettings_AndPublishesMessages()
    {
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object, _messenger);
        sut.Initialize();
        sut.AutoConnect = true;
        sut.PlayConnectionSound = true;
        sut.ThemeMode = AppThemeMode.Light;

        AppSettings? savedSettings = null;
        _messenger.Register<SettingsSavedMessage>(this, (_, m) => savedSettings = m.Value);

        AppThemeMode? receivedTheme = null;
        _messenger.Register<ThemeChangedMessage>(this, (_, m) => receivedTheme = m.Value);

        bool? receivedSound = null;
        _messenger.Register<SoundSettingsChangedMessage>(this, (_, m) => receivedSound = m.Value);

        sut.CloseCommand.Execute(null);

        _settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.Once);
        Assert.NotNull(savedSettings);
        Assert.True(savedSettings!.AutoConnect);
        Assert.True(savedSettings.PlayConnectionSound);
        Assert.Equal(AppThemeMode.Light, receivedTheme);
        Assert.True(receivedSound);
    }

    /// <summary>
    /// Verifies that SaveCommand enables startup when AutoStartOnStartup is true.
    /// </summary>
    [Fact]
    public void SaveCommand_EnablesStartup_WhenAutoStartIsTrue()
    {
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object, _messenger);
        sut.Initialize();
        sut.AutoStartOnStartup = true;

        sut.CloseCommand.Execute(null);

        _startupService.Verify(s => s.Enable(), Times.Once);
    }

    /// <summary>
    /// Verifies that SaveCommand disables startup when AutoStartOnStartup is false.
    /// </summary>
    [Fact]
    public void SaveCommand_DisablesStartup_WhenAutoStartIsFalse()
    {
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object, _messenger);
        sut.Initialize();
        sut.AutoStartOnStartup = false;

        sut.CloseCommand.Execute(null);

        _startupService.Verify(s => s.Disable(), Times.Once);
    }

    /// <summary>
    /// Verifies that CloseCommand raises RequestClose.
    /// </summary>
    [Fact]
    public void CloseCommand_RaisesRequestClose()
    {
        var sut = CreateSut();
        bool raised = false;
        sut.RequestClose += () => raised = true;

        sut.CloseCommand.Execute(null);

        Assert.True(raised);
    }

    /// <summary>
    /// Verifies that new AppSettings properties are loaded and persisted.
    /// </summary>
    [Fact]
    public void SaveCommand_PersistsNewProperties()
    {
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object, _messenger);
        sut.Initialize();
        sut.ShowNotifications = false;
        sut.ReconnectTimeout = ReconnectTimeout.SixtySeconds;

        sut.CloseCommand.Execute(null);

        _settingsService.Verify(s => s.Save(It.Is<AppSettings>(a =>
            a.ShowNotifications == false &&
            a.ReconnectTimeout == ReconnectTimeout.SixtySeconds
        )), Times.Once);
    }
}
