using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Core;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// Primary ViewModel managing Bluetooth device discovery, audio connection lifecycle, and UI state.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly IAudioService _audioService;
    private readonly IProcessService _processService;
    private readonly IDispatcherService _dispatcherService;
    private BluetoothDevice? _selectedBluetoothDevice;
    private bool _isConnected;
    private bool _isBusy;
    private bool _isSettingsOpen;
    private bool _isRefreshing;
    private bool _autoConnect;
    private CancellationTokenSource? _monitorCts;
    private string _statusText = "IDLE";
    private string? _lastDeviceId;
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="audioService">The audio service for device discovery and connection.</param>
    /// <param name="processService">The process service for launching system URIs.</param>
    /// <param name="dispatcherService">The dispatcher service for UI thread operations.</param>
    /// <param name="updateViewModel">The view model for checking and downloading updates.</param>
    /// <param name="settingsViewModel">The view model for the settings panel.</param>
    /// <param name="settingsService">The service for persisting user preferences.</param>
    public MainViewModel(
        IAudioService audioService, 
        IProcessService processService, 
        IDispatcherService dispatcherService,
        UpdateViewModel updateViewModel, 
        SettingsViewModel settingsViewModel, 
        ISettingsService settingsService)
    {
        _audioService = audioService;
        _processService = processService;
        _dispatcherService = dispatcherService;
        Updater = updateViewModel;
        SettingsViewModel = settingsViewModel;
        _settingsService = settingsService;

        Updater.StatusTextChanged += status => StatusText = status;

        var initialSettings = _settingsService.Load();
        _lastDeviceId = initialSettings.LastDeviceId;
        _autoConnect = initialSettings.AutoConnect;

        BluetoothDevices = new ObservableCollection<BluetoothDevice>();

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
        OpenBluetoothSettingsCommand = new RelayCommand(_ => OpenBluetoothSettings());
        RefreshCommand = new AsyncRelayCommand(RefreshDevicesAsync);

        OpenSettingsCommand = new RelayCommand(_ => IsSettingsOpen = true);

        OpenCommand = new RelayCommand(_ => RequestShow?.Invoke());
        ExitCommand = new RelayCommand(_ => RequestExit?.Invoke());

        SettingsViewModel.RequestClose += () => IsSettingsOpen = false;
        SettingsViewModel.SettingsSaved += autoConnect =>
        {
            AutoConnect = autoConnect;
        };

        AutoConnect = SettingsViewModel.AutoConnect;
    }

    /// <summary>
    /// Gets the update view model injected into this instance.
    /// </summary>
    public UpdateViewModel Updater { get; }

    /// <summary>
    /// Gets the observable collection of discovered Bluetooth devices.
    /// </summary>
    public ObservableCollection<BluetoothDevice> BluetoothDevices { get; }

    /// <summary>
    /// Gets or sets the currently selected Bluetooth device.
    /// Persists the device ID to settings when changed.
    /// </summary>
    public BluetoothDevice? SelectedBluetoothDevice
    {
        get => _selectedBluetoothDevice;
        set
        {
            if (SetProperty(ref _selectedBluetoothDevice, value))
            {
                if (value != null)
                {
                    _lastDeviceId = value.Id;
                    if (!_isRefreshing)
                    {
                        var settings = _settingsService.Load();
                        settings.LastDeviceId = value.Id;
                        _settingsService.Save(settings);
                    }
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an active audio connection exists.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a connection operation is in progress.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Settings panel is currently visible.
    /// </summary>
    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application automatically connects to the
    /// last used device on startup.
    /// </summary>
    public bool AutoConnect
    {
        get => _autoConnect;
        set => SetProperty(ref _autoConnect, value);
    }

    /// <summary>
    /// Gets or sets the status text displayed in the UI.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the settings view model to bind against in the Settings overlay.
    /// </summary>
    public SettingsViewModel SettingsViewModel { get; }

    /// <summary>
    /// Gets the command to open the Settings panel.
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    /// <summary>
    /// Gets the command to initiate a connection to the selected device.
    /// </summary>
    public ICommand ConnectCommand { get; }

    /// <summary>
    /// Gets the command to disconnect the current device.
    /// </summary>
    public ICommand DisconnectCommand { get; }

    /// <summary>
    /// Gets the command to open the Windows Bluetooth settings panel.
    /// </summary>
    public ICommand OpenBluetoothSettingsCommand { get; }

    /// <summary>
    /// Gets the command to refresh the Bluetooth device list.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command to show the main window from the system tray.
    /// </summary>
    public ICommand OpenCommand { get; }

    /// <summary>
    /// Gets the command to exit the application.
    /// </summary>
    public ICommand ExitCommand { get; }

    /// <summary>
    /// Raised when the ViewModel requests the View to show itself.
    /// </summary>
    public event Action? RequestShow;

    /// <summary>
    /// Raised when the ViewModel requests the application to exit.
    /// </summary>
    public event Action? RequestExit;

    /// <summary>
    /// Initialises the application state, checks for updates, refreshes devices, and handles AutoConnect on startup.
    /// </summary>
    public async Task InitializeAsync()
    {
        _ = Updater.CheckForUpdateAsync();

        await RefreshDevicesAsync();

        if (_autoConnect && SelectedBluetoothDevice != null)
        {
            _ = ConnectAsync();
        }
    }

    /// <summary>
    /// Refreshes the Bluetooth device list from the audio service while preserving the current selection.
    /// </summary>
    public async Task RefreshDevicesAsync()
    {
        try
        {
            _isRefreshing = true;
            var currentSelectedId = SelectedBluetoothDevice?.Id ?? _lastDeviceId;
            var devices = (await _audioService.GetBluetoothDevicesAsync()).ToList();

            // Update existing items and add new ones
            foreach (var device in devices)
            {
                var existing = BluetoothDevices.FirstOrDefault(d => d.Id == device.Id);
                if (existing == null)
                {
                    BluetoothDevices.Add(device);
                }
                else
                {
                    existing.IsConnected = device.IsConnected;
                    existing.Name = device.Name;
                }
            }

            // Remove items that are no longer present
            var toRemove = BluetoothDevices.Where(d => !devices.Any(n => n.Id == d.Id)).ToList();
            foreach (var item in toRemove)
            {
                BluetoothDevices.Remove(item);
            }

            // Restore selection if needed
            if (SelectedBluetoothDevice == null && currentSelectedId != null)
            {
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault(d => d.Id == currentSelectedId);
            }

            if (SelectedBluetoothDevice == null)
            {
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RefreshDevices] Error: {ex.Message}");
            StatusText = "SCAN ERROR";
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    internal async Task ConnectAsync()
    {
        if (SelectedBluetoothDevice == null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = $"CONNECTING TO {SelectedBluetoothDevice.Name}...";

            var ok = await _audioService.ConnectBluetoothAudioAsync(SelectedBluetoothDevice.Id);
            if (!ok)
            {
                StatusText = "WAITING FOR SOURCE...";
                StartConnectionMonitor(SelectedBluetoothDevice.Id, SelectedBluetoothDevice.Name);
                return;
            }

            IsConnected = true;
            StatusText = "STREAMING ACTIVE";

            StartConnectionMonitor(SelectedBluetoothDevice.Id, SelectedBluetoothDevice.Name);
        }
        catch (Exception ex)
        {
            StatusText = "ERROR: " + ex.Message;
            IsConnected = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal void Disconnect()
    {
        StopConnectionMonitor();

        try
        {
            _audioService.Disconnect();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Disconnect] Error: {ex.Message}");
        }

        IsConnected = false;
        StatusText = "DISCONNECTED";
    }

    private void StartConnectionMonitor(string deviceId, string deviceName)
    {
        StopConnectionMonitor();
        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(5000, token);

                    var connected = await _audioService.IsBluetoothDeviceConnectedAsync(deviceId);
                    if (connected)
                    {
                        continue;
                    }

                    StatusText = "RECONNECTING...";
                    IsConnected = false;

                    try { _audioService.Disconnect(); }
                    catch { /* already stopped */ }

                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(3000, token);

                        var ok = await _audioService.ConnectBluetoothAudioAsync(deviceId);
                        if (!ok)
                        {
                            continue;
                        }

                        IsConnected = true;
                        StatusText = "STREAMING ACTIVE";
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Monitor] Unexpected error: {ex.Message}");
            }
        }, token);
    }

    private void StopConnectionMonitor()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    private void OpenBluetoothSettings()
    {
        _processService.OpenUri("ms-settings:bluetooth");
    }

    private bool CanConnect()
    {
        return SelectedBluetoothDevice != null && !IsConnected && !IsBusy;
    }

    private bool CanDisconnect()
    {
        return IsConnected;
    }
}
