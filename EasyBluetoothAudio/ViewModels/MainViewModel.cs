using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// Primary ViewModel managing Bluetooth device discovery, audio connection lifecycle, and UI state.
/// Subscribes to <see cref="SettingsSavedMessage"/> to react to settings changes via the Mediator pattern.
/// </summary>
/// <param name="audioService">The audio service for device discovery and connection.</param>
/// <param name="processService">The process service for launching system URIs.</param>
/// <param name="dispatcherService">The dispatcher service for UI thread operations.</param>
/// <param name="updateViewModel">The view model for checking and downloading updates.</param>
/// <param name="settingsViewModel">The view model for the settings panel.</param>
/// <param name="settingsService">The service for persisting user preferences.</param>
/// <param name="messenger">The messenger instance for decoupled communication.</param>
public partial class MainViewModel(
    IAudioService audioService,
    IProcessService processService,
    UpdateViewModel updateViewModel,
    SettingsViewModel settingsViewModel,
    ISettingsService settingsService,
    IMessenger messenger) : ObservableObject
{
    private CancellationTokenSource? _monitorCts;
    private string? _lastDeviceId;
    private bool _isRefreshing;

    /// <summary>
    /// Gets or sets the currently selected Bluetooth device.
    /// Persists the device ID to settings when changed by the user.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private BluetoothDevice? _selectedBluetoothDevice;

    /// <summary>
    /// Gets a value indicating whether an active audio connection exists.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    /// <summary>
    /// Gets a value indicating whether a connection operation is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private bool _isBusy;

    /// <summary>
    /// Gets or sets a value indicating whether the Settings panel is currently visible.
    /// </summary>
    [ObservableProperty]
    private bool _isSettingsOpen;

    /// <summary>
    /// Gets or sets a value indicating whether the application automatically connects
    /// to the last used device on startup.
    /// </summary>
    [ObservableProperty]
    private bool _autoConnect;

    /// <summary>
    /// Gets or sets the status text displayed in the UI.
    /// </summary>
    [ObservableProperty]
    private string _statusText = "IDLE";

    /// <summary>
    /// Gets the update view model injected into this instance.
    /// </summary>
    public UpdateViewModel Updater { get; } = updateViewModel;

    /// <summary>
    /// Gets the settings view model to bind against in the Settings overlay.
    /// </summary>
    public SettingsViewModel SettingsViewModel { get; } = settingsViewModel;

    /// <summary>
    /// Gets the observable collection of discovered Bluetooth devices.
    /// </summary>
    public ObservableCollection<BluetoothDevice> BluetoothDevices { get; } = [];

    /// <summary>
    /// Raised when the ViewModel requests the View to show itself.
    /// </summary>
    public event Action? RequestShow;

    /// <summary>
    /// Raised when the ViewModel requests the application to exit.
    /// </summary>
    public event Action? RequestExit;

    /// <summary>
    /// Initialises the application state, wires up Messenger subscriptions,
    /// checks for updates, refreshes devices, and handles AutoConnect on startup.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization.</returns>
    public async Task InitializeAsync()
    {
        var initialSettings = settingsService.Load();
        _lastDeviceId = initialSettings.LastDeviceId;
        AutoConnect = initialSettings.AutoConnect;

        Updater.StatusTextChanged += status => StatusText = status;

        SettingsViewModel.RequestClose += () => IsSettingsOpen = false;
        SettingsViewModel.Initialize();

        messenger.Register<SettingsSavedMessage>(this, (_, message) =>
            OnSettingsSaved(message.Value));

        _ = Updater.CheckForUpdateAsync();

        await RefreshDevicesAsync();

        if (AutoConnect && SelectedBluetoothDevice != null)
        {
            _ = ConnectAsync();
        }
    }

    /// <summary>
    /// Refreshes the Bluetooth device list from the audio service while preserving the current selection.
    /// </summary>
    /// <returns>A task representing the asynchronous refresh operation.</returns>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task RefreshDevicesAsync()
    {
        try
        {
            _isRefreshing = true;
            var currentSelectedId = SelectedBluetoothDevice?.Id ?? _lastDeviceId;
            var devices = (await audioService.GetBluetoothDevicesAsync()).ToList();

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

            var toRemove = BluetoothDevices.Where(d => !devices.Any(n => n.Id == d.Id)).ToList();
            foreach (var item in toRemove)
            {
                BluetoothDevices.Remove(item);
            }

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

    /// <summary>
    /// Establishes an audio connection to the currently selected Bluetooth device.
    /// </summary>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    [CommunityToolkit.Mvvm.Input.RelayCommand(CanExecute = nameof(CanConnect))]
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

            var ok = await audioService.ConnectBluetoothAudioAsync(SelectedBluetoothDevice.Id);
            if (!ok)
            {
                StatusText = "WAITING FOR SOURCE...";
                StartConnectionMonitor(SelectedBluetoothDevice.Id, SelectedBluetoothDevice.Name);
                return;
            }

            IsConnected = true;
            StatusText = "STREAMING ACTIVE";
            messenger.Send(new ConnectionEstablishedMessage(SelectedBluetoothDevice.Name));

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

    /// <summary>
    /// Disconnects the current Bluetooth audio device.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand(CanExecute = nameof(CanDisconnect))]
    internal void Disconnect()
    {
        StopConnectionMonitor();

        try
        {
            audioService.Disconnect();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Disconnect] Error: {ex.Message}");
        }

        IsConnected = false;
        StatusText = "DISCONNECTED";
    }

    /// <summary>
    /// Opens the system Settings panel.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    /// <summary>
    /// Opens the Windows Bluetooth settings page.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void OpenBluetoothSettings()
    {
        processService.OpenUri("ms-settings:bluetooth");
    }

    /// <summary>
    /// Requests the main window to show itself.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Open()
    {
        RequestShow?.Invoke();
    }

    /// <summary>
    /// Requests the application to exit.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Exit()
    {
        RequestExit?.Invoke();
    }

    /// <summary>
    /// Persists the selected device identifier when the selected device changes
    /// (but not during a device list refresh).
    /// </summary>
    /// <param name="value">The newly selected Bluetooth device.</param>
    partial void OnSelectedBluetoothDeviceChanged(BluetoothDevice? value)
    {
        if (value != null)
        {
            _lastDeviceId = value.Id;
            if (!_isRefreshing)
            {
                var settings = settingsService.Load();
                settings.LastDeviceId = value.Id;
                settingsService.Save(settings);
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="SettingsSavedMessage"/> by updating the AutoConnect flag.
    /// </summary>
    /// <param name="settings">The persisted settings snapshot.</param>
    private void OnSettingsSaved(AppSettings settings)
    {
        AutoConnect = settings.AutoConnect;
    }

    /// <summary>
    /// Determines whether the connect command can execute.
    /// </summary>
    /// <returns><see langword="true"/> if a device is selected, not connected, and not busy.</returns>
    private bool CanConnect()
    {
        return SelectedBluetoothDevice != null && !IsConnected && !IsBusy;
    }

    /// <summary>
    /// Determines whether the disconnect command can execute.
    /// </summary>
    /// <returns><see langword="true"/> if currently connected.</returns>
    private bool CanDisconnect()
    {
        return IsConnected;
    }

    /// <summary>
    /// Starts a background task that monitors the Bluetooth connection and automatically reconnects.
    /// </summary>
    /// <param name="deviceId">The device identifier to monitor.</param>
    /// <param name="deviceName">The friendly device name for status messages.</param>
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

                    var connected = await audioService.IsBluetoothDeviceConnectedAsync(deviceId);
                    if (connected)
                    {
                        continue;
                    }

                    StatusText = "RECONNECTING...";
                    IsConnected = false;

                    try { audioService.Disconnect(); }
                    catch { /* already stopped */ }

                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(3000, token);

                        var ok = await audioService.ConnectBluetoothAudioAsync(deviceId);
                        if (!ok)
                        {
                            continue;
                        }

                        IsConnected = true;
                        StatusText = "STREAMING ACTIVE";
                        messenger.Send(new ConnectionEstablishedMessage(deviceName));
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

    /// <summary>
    /// Stops the connection monitor background task.
    /// </summary>
    private void StopConnectionMonitor()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }
}
