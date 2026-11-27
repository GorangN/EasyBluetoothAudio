using System.Text.Json;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Tests;

public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        var settings = new AppSettings();

        Assert.False(settings.AutoStartOnStartup);
        Assert.True(settings.AutoConnect);
        Assert.Null(settings.LastDeviceId);
        Assert.Equal(AppThemeMode.Dark, settings.ThemeMode);
        Assert.True(settings.ShowNotifications);
        Assert.False(settings.PlayConnectionSound);
    }

    [Fact]
    public void Property_GettersAndSetters_WorkCorrectly()
    {
        var settings = new AppSettings
        {
            AutoStartOnStartup = true,
            AutoConnect = true,
            LastDeviceId = "device123",
            ThemeMode = AppThemeMode.Light,
            ShowNotifications = false,
            PlayConnectionSound = true
        };

        Assert.True(settings.AutoStartOnStartup);
        Assert.True(settings.AutoConnect);
        Assert.Equal("device123", settings.LastDeviceId);
        Assert.Equal(AppThemeMode.Light, settings.ThemeMode);
        Assert.False(settings.ShowNotifications);
        Assert.True(settings.PlayConnectionSound);
    }

    /// <summary>
    /// Preserves the numeric theme values used by saved preferences across releases.
    /// </summary>
    /// <param name="value">The persisted numeric value.</param>
    /// <param name="mode">The appearance represented by that value.</param>
    [Theory]
    [InlineData(0, AppThemeMode.Light)]
    [InlineData(1, AppThemeMode.Dark)]
    [InlineData(2, AppThemeMode.System)]
    [InlineData(3, AppThemeMode.RetroDark)]
    [InlineData(4, AppThemeMode.RetroLight)]
    public void ThemeMode_PreservesPersistedValue(int value, AppThemeMode mode)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>($$"""{"ThemeMode":{{value}}}""");
        Assert.NotNull(settings);
        Assert.Equal(mode, settings.ThemeMode);
        Assert.Equal(value, JsonSerializer.SerializeToElement(settings).GetProperty(nameof(AppSettings.ThemeMode)).GetInt32());
    }
}
