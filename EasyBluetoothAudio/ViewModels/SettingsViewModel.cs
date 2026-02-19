using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EasyBluetoothAudio.Core;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// ViewModel for the Settings panel, managing user preferences and their persistence.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly IAudioService _audioService;

    private bool _autoStartOnStartup;
    private bool _autoConnect;
    private AudioDelay _selectedDelay;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class and loads persisted settings.
    /// </summary>
    /// <param name="settingsService">Service for loading and saving settings.</param>
    /// <param name="startupService">Service for managing the Windows startup entry.</param>
    /// <param name="audioService">Service for enumerating audio output devices.</param>
    public SettingsViewModel(ISettingsService settingsService, IStartupService startupService, IAudioService audioService)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _audioService = audioService;

        OutputDevices = new ObservableCollection<AudioDevice>();

        SaveCommand = new RelayCommand(_ => Save());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());

        LoadFromSettings(_settingsService.Load());
    }

    /// <summary>
    /// Raised when the Settings panel should be closed without saving.
    /// </summary>
    public event Action? RequestClose;

    /// <summary>
    /// Raised after settings are saved, providing the new buffer milliseconds and auto-connect flag.
    /// </summary>
    public event Action<int, bool>? SettingsSaved;

    /// <summary>
    /// Gets the command that persists the current settings.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Gets the command that closes the Settings panel without saving.
    /// </summary>
    public ICommand CloseCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the application starts automatically with Windows.
    /// </summary>
    public bool AutoStartOnStartup
    {
        get => _autoStartOnStartup;
        set => SetProperty(ref _autoStartOnStartup, value);
    }



    /// <summary>
    /// Gets or sets a value indicating whether the application automatically connects to the last
    /// used device on startup.
    /// </summary>
    public bool AutoConnect
    {
        get => _autoConnect;
        set => SetProperty(ref _autoConnect, value);
    }

    /// <summary>
    /// Gets or sets the selected audio buffer delay preset.
    /// </summary>
    public AudioDelay SelectedDelay
    {
        get => _selectedDelay;
        set => SetProperty(ref _selectedDelay, value);
    }





    private void LoadFromSettings(AppSettings settings)
    {
        _autoStartOnStartup = _startupService.IsEnabled;
        _autoConnect = settings.AutoConnect;
        _selectedDelay = settings.Delay;
    }

    private void Save()
    {
        if (AutoStartOnStartup)
            _startupService.Enable();
        else
            _startupService.Disable();

        var settings = _settingsService.Load();
        settings.AutoStartOnStartup = AutoStartOnStartup;
        settings.AutoConnect = AutoConnect;
        settings.Delay = SelectedDelay;
        _settingsService.Save(settings);

        SettingsSaved?.Invoke((int)SelectedDelay, AutoConnect);
        RequestClose?.Invoke();
    }
}
