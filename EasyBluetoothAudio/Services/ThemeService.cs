using System;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Microsoft.Win32;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Manages runtime theme switching by subscribing to <see cref="ThemeChangedMessage"/>
/// and swapping the application's <see cref="ResourceDictionary"/>.
/// </summary>
/// <param name="messenger">The messenger instance for receiving theme-change messages.</param>
/// <param name="settingsService">The settings service for reading the persisted theme preference.</param>
public class ThemeService(IMessenger messenger, ISettingsService settingsService) : IThemeService
{
    private const string DarkThemePath = "Resources/DarkTheme.xaml";
    private const string LightThemePath = "Resources/LightTheme.xaml";

    /// <inheritdoc />
    public void Initialize()
    {
        messenger.Register<ThemeChangedMessage>(this, (_, message) => ApplyTheme(message.Value));

        var settings = settingsService.Load();
        ApplyTheme(settings.ThemeMode);
    }

    /// <summary>
    /// Applies the specified theme by replacing the first <see cref="ResourceDictionary"/>
    /// in <see cref="Application.Current"/> with the matching theme dictionary.
    /// </summary>
    /// <param name="mode">The <see cref="AppThemeMode"/> to apply.</param>
    private static void ApplyTheme(AppThemeMode mode)
    {
        var resolvedMode = mode == AppThemeMode.System ? DetectSystemTheme() : mode;
        var themePath = resolvedMode == AppThemeMode.Light ? LightThemePath : DarkThemePath;

        var themeUri = new Uri(themePath, UriKind.Relative);
        var newTheme = new ResourceDictionary { Source = themeUri };

        var mergedDictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;

        // The theme dictionary is always the first entry in MergedDictionaries.
        if (mergedDictionaries.Count > 0)
        {
            mergedDictionaries[0] = newTheme;
        }
        else
        {
            mergedDictionaries.Insert(0, newTheme);
        }
    }

    /// <summary>
    /// Reads the Windows personalization registry key to determine the OS-level theme.
    /// Falls back to <see cref="AppThemeMode.Dark"/> if the key cannot be read.
    /// </summary>
    /// <returns>The detected <see cref="AppThemeMode"/>.</returns>
    private static AppThemeMode DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");

            if (value is int intValue)
            {
                return intValue == 1 ? AppThemeMode.Light : AppThemeMode.Dark;
            }
        }
        catch
        {
            // Registry access can fail in sandboxed environments; default to Dark.
        }

        return AppThemeMode.Dark;
    }
}
