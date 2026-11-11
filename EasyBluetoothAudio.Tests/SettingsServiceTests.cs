using System;
using System.IO;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using Xunit;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Unit tests for <see cref="SettingsService"/>.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _tempPath;
    private readonly SettingsService _sut;

    /// <summary>
    /// Initializes a temporary directory and a <see cref="SettingsService"/> instance for each test.
    /// </summary>
    public SettingsServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"EBATest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempPath);

        _sut = new SettingsService(_tempPath);
    }

    /// <summary>
    /// Cleans up the temporary test directory.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that Load returns default values when no settings file exists.
    /// </summary>
    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var result = _sut.Load();

        Assert.False(result.AutoStartOnStartup);
        Assert.True(result.AutoConnect);
        Assert.Null(result.LastDeviceId);
        Assert.Equal(AppThemeMode.Dark, result.ThemeMode);
        Assert.True(result.ShowNotifications);
        Assert.False(result.PlayConnectionSound);
    }

    /// <summary>
    /// Verifies that Load returns default values when the settings file contains corrupted JSON.
    /// </summary>
    [Fact]
    public void Load_ReturnsDefaults_WhenJsonIsCorrupted()
    {
        var filePath = Path.Combine(_tempPath, "settings.json");
        File.WriteAllText(filePath, "{ invalid json data ");

        var result = _sut.Load();

        Assert.False(result.AutoStartOnStartup);
        Assert.True(result.AutoConnect);
        Assert.Null(result.LastDeviceId);
        Assert.Equal(AppThemeMode.Dark, result.ThemeMode);
        Assert.True(result.ShowNotifications);
        Assert.False(result.PlayConnectionSound);
    }

    /// <summary>
    /// Verifies that all settings properties round-trip through Save/Load correctly.
    /// </summary>
    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var original = new AppSettings
        {
            AutoStartOnStartup = true,
            AutoConnect = true,
            LastDeviceId = "device-abc",
            ThemeMode = AppThemeMode.Light,
            ShowNotifications = false,
            PlayConnectionSound = true
        };

        _sut.Save(original);
        var loaded = _sut.Load();

        Assert.Equal(original.AutoStartOnStartup, loaded.AutoStartOnStartup);
        Assert.Equal(original.AutoConnect, loaded.AutoConnect);
        Assert.Equal(original.LastDeviceId, loaded.LastDeviceId);
        Assert.Equal(original.ThemeMode, loaded.ThemeMode);
        Assert.Equal(original.ShowNotifications, loaded.ShowNotifications);
        Assert.Equal(original.PlayConnectionSound, loaded.PlayConnectionSound);
    }

    /// <summary>Legacy settings restore one source; an explicit empty set never resurrects it.</summary>
    /// <param name="devicesJson">The optional new restoration field.</param>
    /// <param name="expectedCount">Expected source count after migration.</param>
    [Theory]
    [InlineData("", 1)]
    [InlineData(", \"LastConnectedDevices\":null", 1)]
    [InlineData(", \"LastConnectedDevices\":[]", 0)]
    public void Load_MigratesOnlyMissingRestorationSet(string devicesJson, int expectedCount)
    {
        File.WriteAllText(Path.Combine(_tempPath, "settings.json"), "{\"LastDeviceId\":\"phone\"" + devicesJson + "}");
        var settings = _sut.Load();
        Assert.Equal(expectedCount, settings.LastConnectedDevices!.Count);
        _sut.Save(settings);
        Assert.Equal(expectedCount, _sut.Load().LastConnectedDevices!.Count);
    }

    /// <summary>Restoration ignores invalid and duplicate identities without changing other preferences.</summary>
    [Fact]
    public void Load_NormalizesRestorationIdentities()
    {
        _sut.Save(new AppSettings
        {
            ShowNotifications = false,
            LastConnectedDevices = [new() { Id = "A", Name = "Phone" }, new() { Id = "a" }, new() { Id = " " }]
        });
        var settings = _sut.Load();
        Assert.Equal("Phone", Assert.Single(settings.LastConnectedDevices!).Name);
        Assert.False(settings.ShowNotifications);
    }
}
