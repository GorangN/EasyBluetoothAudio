using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Tests;

public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        var settings = new AppSettings();

        Assert.False(settings.AutoStartOnStartup);
        Assert.False(settings.AutoConnect);
        Assert.Null(settings.LastDeviceId);
        Assert.Equal(AppThemeMode.Dark, settings.ThemeMode);
        Assert.Null(settings.PreferredDeviceId);
        Assert.Equal(ReconnectTimeout.ThirtySeconds, settings.ReconnectTimeout);
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
            PreferredDeviceId = "device456",
            ReconnectTimeout = ReconnectTimeout.SixtySeconds,
            ShowNotifications = false,
            PlayConnectionSound = true
        };

        Assert.True(settings.AutoStartOnStartup);
        Assert.True(settings.AutoConnect);
        Assert.Equal("device123", settings.LastDeviceId);
        Assert.Equal(AppThemeMode.Light, settings.ThemeMode);
        Assert.Equal("device456", settings.PreferredDeviceId);
        Assert.Equal(ReconnectTimeout.SixtySeconds, settings.ReconnectTimeout);
        Assert.False(settings.ShowNotifications);
        Assert.True(settings.PlayConnectionSound);
    }
}
