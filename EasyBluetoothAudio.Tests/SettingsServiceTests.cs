using System;
using System.IO;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using Xunit;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Unit tests for <see cref="SettingsService"/>.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly SettingsService _sut;

    public SettingsServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"EBATest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempPath);

        _sut = new SettingsService(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath, recursive: true);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var result = _sut.Load();

        Assert.False(result.AutoStartOnStartup);
        Assert.False(result.AutoConnect);
        Assert.Null(result.LastDeviceId);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var original = new AppSettings
        {
            AutoStartOnStartup = true,
            AutoConnect = true,
            LastDeviceId = "device-abc"
        };

        _sut.Save(original);
        var loaded = _sut.Load();

        Assert.Equal(original.AutoStartOnStartup, loaded.AutoStartOnStartup);
        Assert.Equal(original.AutoConnect, loaded.AutoConnect);
        Assert.Equal(original.LastDeviceId, loaded.LastDeviceId);
    }
}
