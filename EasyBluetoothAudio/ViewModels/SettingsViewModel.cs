using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// ViewModel for the Settings panel, managing user preferences, their persistence,
/// and broadcasting changes via the <see cref="IMessenger"/> (Mediator pattern).
/// </summary>
/// <param name="settingsService">Service for loading and saving settings.</param>
/// <param name="startupService">Service for managing the Windows startup entry.</param>
/// <param name="messenger">The messenger instance used for decoupled communication.</param>
public partial class SettingsViewModel(
    ISettingsService settingsService,
    IStartupService startupService,
    IMessenger messenger) : ObservableObject
{
    /// <summary>
    /// Gets or sets a value indicating whether the application starts automatically with Windows.
    /// </summary>
    [ObservableProperty]
    private bool _autoStartOnStartup;

    /// <summary>
    /// Gets or sets a value indicating whether the application automatically connects to the last
    /// used device on startup.
    /// </summary>
    [ObservableProperty]
    private bool _autoConnect;

    /// <summary>
    /// Gets or sets the active color theme mode for the application.
    /// </summary>
    [ObservableProperty]
    private AppThemeMode _themeMode;

    /// <summary>
    /// Gets or sets the identifier of the user's preferred Bluetooth audio device.
    /// </summary>
    [ObservableProperty]
    private string? _preferredDeviceId;

    /// <summary>
    /// Gets or sets the reconnect timeout strategy.
    /// </summary>
    [ObservableProperty]
    private ReconnectTimeout _reconnectTimeout;

    /// <summary>
    /// Gets or sets a value indicating whether toast notifications are shown for connection events.
    /// </summary>
    [ObservableProperty]
    private bool _showNotifications;

    /// <summary>
    /// Gets or sets a value indicating whether a sound is played when a connection is established.
    /// </summary>
    [ObservableProperty]
    private bool _playConnectionSound;

    private bool _isInitialized;

    /// <summary>
    /// Raised when the Settings panel should be closed.
    /// </summary>
    public event Action? RequestClose;

    /// <summary>
    /// Gets the available theme modes for ComboBox binding.
    /// </summary>
    public AppThemeMode[] ThemeModes { get; } = Enum.GetValues<AppThemeMode>();

    /// <summary>
    /// Gets the available reconnect timeout options for ComboBox binding.
    /// </summary>
    public ReconnectTimeout[] ReconnectTimeouts { get; } = Enum.GetValues<ReconnectTimeout>();

    /// <summary>
    /// Loads persisted settings into the ViewModel properties.
    /// Must be called after construction to initialize the UI state.
    /// </summary>
    public void Initialize()
    {
        var settings = settingsService.Load();
        AutoStartOnStartup = startupService.IsEnabled;
        AutoConnect = settings.AutoConnect;
        ThemeMode = settings.ThemeMode;
        PreferredDeviceId = settings.PreferredDeviceId;
        ReconnectTimeout = settings.ReconnectTimeout;
        ShowNotifications = settings.ShowNotifications;
        PlayConnectionSound = settings.PlayConnectionSound;
        _isInitialized = true;
    }

    /// <summary>
    /// Saves and closes the Settings panel. Called when the user clicks the close button
    /// or when the window loses focus while settings are open.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        if (_isInitialized)
        {
            SaveInternal();
        }

        RequestClose?.Invoke();
    }

    /// <summary>
    /// Core save logic: persists settings, manages startup, and publishes Messenger messages.
    /// </summary>
    private void SaveInternal()
    {
        if (AutoStartOnStartup)
        {
            startupService.Enable();
        }
        else
        {
            startupService.Disable();
        }

        var settings = settingsService.Load();
        settings.AutoStartOnStartup = AutoStartOnStartup;
        settings.AutoConnect = AutoConnect;
        settings.ThemeMode = ThemeMode;
        settings.PreferredDeviceId = PreferredDeviceId;
        settings.ReconnectTimeout = ReconnectTimeout;
        settings.ShowNotifications = ShowNotifications;
        settings.PlayConnectionSound = PlayConnectionSound;
        settingsService.Save(settings);

        messenger.Send(new ThemeChangedMessage(ThemeMode));
        messenger.Send(new SoundSettingsChangedMessage(PlayConnectionSound));
        messenger.Send(new SettingsSavedMessage(settings));
    }
}
