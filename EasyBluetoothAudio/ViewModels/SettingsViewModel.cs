using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// ViewModel for the Settings panel, managing user preferences, their persistence,
/// and broadcasting changes via the <see cref="IMessenger"/> (Mediator pattern).
/// </summary>
/// <param name="settingsService">Service for loading and saving settings.</param>
/// <param name="startupService">Service for managing the Windows startup entry.</param>
/// <param name="qualityService">Service for adjusting Bluetooth SBC bitpool quality settings.</param>
/// <param name="messenger">The messenger instance used for decoupled communication.</param>
public partial class SettingsViewModel(
    ISettingsService settingsService,
    IStartupService startupService,
    IBluetoothQualityService qualityService,
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
    /// Gets or sets a value indicating whether toast notifications are shown for connection events.
    /// </summary>
    [ObservableProperty]
    private bool _showNotifications;

    /// <summary>
    /// Gets or sets a value indicating whether a sound is played when a connection is established.
    /// </summary>
    [ObservableProperty]
    private bool _playConnectionSound;

    /// <summary>
    /// Gets or sets a value indicating whether low-end hardware mode is enabled,
    /// reducing the Bluetooth SBC bitpool to improve stability on congested radios.
    /// </summary>
    [ObservableProperty]
    private bool _lowEndHardwareMode;

    /// <summary>
    /// Gets or sets the feedback message displayed after a Bluetooth quality registry operation.
    /// <see langword="null"/> when no operation has been performed in the current session.
    /// </summary>
    [ObservableProperty]
    private string? _qualityApplyFeedback;

    private bool _isInitialized;
    private bool _suppressQualityChange;
    private System.Threading.Tasks.Task _pendingQualityChange = System.Threading.Tasks.Task.CompletedTask;

    /// <summary>
    /// Raised when the Settings panel should be closed.
    /// </summary>
    public event Action? RequestClose;

    /// <summary>
    /// Gets the available theme modes for ComboBox binding.
    /// </summary>
    public AppThemeMode[] ThemeModes { get; } = Enum.GetValues<AppThemeMode>();

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
        ShowNotifications = settings.ShowNotifications;
        PlayConnectionSound = settings.PlayConnectionSound;
        LowEndHardwareMode = settings.LowEndHardwareMode;
        QualityApplyFeedback = null;
        _isInitialized = true;
    }

    /// <summary>
    /// Saves and closes the Settings panel. Called when the user clicks the close button
    /// or when the window loses focus while settings are open.
    /// Awaits any in-progress quality registry operation so the correct toggle state is persisted.
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task CloseAsync()
    {
        if (_isInitialized)
        {
            await _pendingQualityChange;
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
        settings.ShowNotifications = ShowNotifications;
        settings.PlayConnectionSound = PlayConnectionSound;
        settings.LowEndHardwareMode = LowEndHardwareMode;
        settingsService.Save(settings);

        messenger.Send(new ThemeChangedMessage(ThemeMode));
        messenger.Send(new SoundSettingsChangedMessage(PlayConnectionSound));
        messenger.Send(new SettingsSavedMessage(settings));
    }

    /// <summary>
    /// Kicks off the async quality change when the checkbox is toggled,
    /// so the feedback banner is visible while the Settings panel is still open.
    /// </summary>
    /// <param name="value">The new value of <see cref="LowEndHardwareMode"/>.</param>
    partial void OnLowEndHardwareModeChanged(bool value)
    {
        if (!_isInitialized || _suppressQualityChange)
        {
            return;
        }

        _pendingQualityChange = ApplyQualityChangeAsync(value);
    }

    /// <summary>
    /// Calls the quality service asynchronously, then updates UI state and persists the result.
    /// Runs a UAC elevation prompt if needed (via the service's elevated helper mechanism).
    /// </summary>
    /// <param name="enable">Whether to enable or disable low-bandwidth mode.</param>
    private async System.Threading.Tasks.Task ApplyQualityChangeAsync(bool enable)
    {
        var result = enable
            ? await qualityService.ApplyLowBandwidthModeAsync()
            : await qualityService.RestoreDefaultModeAsync();

        var expectedResult = enable ? BluetoothQualityResult.Applied : BluetoothQualityResult.Restored;
        if (result != expectedResult)
        {
            // UAC was cancelled or the adapter is unsupported — revert checkbox to match reality.
            _suppressQualityChange = true;
            LowEndHardwareMode = !enable;
            _suppressQualityChange = false;
        }
        else
        {
            messenger.Send(new ReconnectRequestedMessage());
        }

        QualityApplyFeedback = GetFeedbackText(result);
    }

    /// <summary>
    /// Maps a <see cref="BluetoothQualityResult"/> to the feedback text displayed in the Settings panel.
    /// </summary>
    /// <param name="result">The result returned by the quality service.</param>
    /// <returns>A feedback string for the UI, or <see langword="null"/> for unrecognised values.</returns>
    private static string? GetFeedbackText(BluetoothQualityResult result)
    {
        return result switch
        {
            BluetoothQualityResult.Applied => "APPLIED — RECONNECTING...",
            BluetoothQualityResult.Restored => "RESTORED — RECONNECTING...",
            BluetoothQualityResult.AccessDenied => "UAC CANCELLED — ADMINISTRATOR RIGHTS ARE REQUIRED FOR THIS SETTING",
            BluetoothQualityResult.NotSupported => "YOUR BLUETOOTH ADAPTER MAY NOT SUPPORT THIS SETTING",
            _ => null
        };
    }
}
