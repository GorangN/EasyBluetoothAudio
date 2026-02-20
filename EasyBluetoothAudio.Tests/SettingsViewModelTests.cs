using System;
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

    private SettingsViewModel CreateSut(AppSettings? settings = null)
    {
        _settingsService.Setup(s => s.Load()).Returns(settings ?? new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        return new SettingsViewModel(_settingsService.Object, _startupService.Object);
    }

    [Fact]
    public void Constructor_LoadsSettingsFromService()
    {
        var settings = new AppSettings { Delay = AudioDelay.High, AutoConnect = true, SyncVolume = true };
        var sut = CreateSut(settings);

        Assert.Equal(AudioDelay.High, sut.SelectedDelay);
        Assert.True(sut.AutoConnect);
        Assert.True(sut.SyncVolume);
    }

    [Fact]
    public void Constructor_ReadsAutoStartFromStartupService()
    {
        _startupService.Setup(s => s.IsEnabled).Returns(true);
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());

        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object);

        Assert.True(sut.AutoStartOnStartup);
    }

    [Fact]
    public void AutoConnect_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        string? raised = null;
        sut.PropertyChanged += (_, e) => raised = e.PropertyName;

        sut.AutoConnect = true;

        Assert.Equal(nameof(sut.AutoConnect), raised);
    }

    [Fact]
    public void SelectedDelay_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        string? raised = null;
        sut.PropertyChanged += (_, e) => raised = e.PropertyName;

        sut.SelectedDelay = AudioDelay.Low;

        Assert.Equal(nameof(sut.SelectedDelay), raised);
    }

    [Fact]
    public void SaveCommand_PersistsSettings_AndRaisesSettingsSaved()
    {
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object)
        {
            SelectedDelay = AudioDelay.Low,
            AutoConnect = true
        };

        int? savedBuffer = null;
        bool? savedAutoConnect = null;
        sut.SettingsSaved += (b, a) => { savedBuffer = b; savedAutoConnect = a; };

        sut.SaveCommand.Execute(null);

        _settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.Once);
        Assert.Equal((int)AudioDelay.Low, savedBuffer);
        Assert.True(savedAutoConnect);
    }

    [Fact]
    public void SaveCommand_EnablesStartup_WhenAutoStartIsTrue()
    {
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object)
        {
            AutoStartOnStartup = true
        };

        sut.SaveCommand.Execute(null);

        _startupService.Verify(s => s.Enable(), Times.Once);
    }

    [Fact]
    public void SaveCommand_DisablesStartup_WhenAutoStartIsFalse()
    {
        _settingsService.Setup(s => s.Load()).Returns(new AppSettings());
        _startupService.Setup(s => s.IsEnabled).Returns(false);
        var sut = new SettingsViewModel(_settingsService.Object, _startupService.Object)
        {
            AutoStartOnStartup = false
        };

        sut.SaveCommand.Execute(null);

        _startupService.Verify(s => s.Disable(), Times.Once);
    }

    [Fact]
    public void CloseCommand_RaisesRequestClose()
    {
        var sut = CreateSut();
        bool raised = false;
        sut.RequestClose += () => raised = true;

        sut.CloseCommand.Execute(null);

        Assert.True(raised);
    }
}
