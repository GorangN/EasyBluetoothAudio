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
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// Primary ViewModel managing Bluetooth device discovery, audio connection lifecycle, and UI state.
/// Subscribes to <see cref="SettingsSavedMessage"/> to react to settings changes via the Mediator pattern.
/// </summary>
/// <param name="audioService">The audio service for device discovery and connection.</param>
/// <param name="devicePickerService">The service for showing the native Windows Bluetooth device picker UI.</param>
/// <param name="dispatcherService">The dispatcher service for UI thread operations.</param>
/// <param name="updateViewModel">The view model for checking and downloading updates.</param>
/// <param name="settingsViewModel">The view model for the settings panel.</param>
/// <param name="settingsService">The service for persisting user preferences.</param>
/// <param name="messenger">The messenger instance for decoupled communication.</param>
public partial class MainViewModel(
    IAudioService audioService,
    IDevicePickerService devicePickerService,
    UpdateViewModel updateViewModel,
    SettingsViewModel settingsViewModel,
    ISettingsService settingsService,
    IDispatcherService dispatcherService,
    IMessenger messenger) : ObservableObject
{
    /// <summary>
    /// Milliseconds to wait after a disconnect before attempting to reconnect,
    /// allowing Windows to complete Bluetooth teardown before a new connection is opened.
    /// </summary>
    internal const int ReconnectSettleDelayMs = 5_000;

    /// <summary>
    /// Milliseconds to wait when the Bluetooth device is already physically connected in Windows
    /// (i.e. paired and in range) but the audio connection needs to be re-established.
    /// No settle is required because the physical Bluetooth link was never torn down.
    /// </summary>
    internal const int ReconnectPhysicallyConnectedDelayMs = 0;

    /// <summary>
    /// Interval in milliseconds between periodic audio stream health probes.
    /// After this duration of uninterrupted connection, the monitor calls
    /// <see cref="IAudioService.ProbeConnectionAsync"/> which tears down and re-creates the
    /// <c>AudioPlaybackConnection</c>, forcing end-to-end re-negotiation with the phone.
    /// This reliably detects A2DP sessions the phone closed during idle — event-based detection
    /// is not possible because the <c>StateChanged</c> event and <c>State</c> property are
    /// both unreliable in this scenario.
    /// </summary>
    internal const int ConnectionProbeIntervalMs = 20 * 60 * 1000; // 20 minutes

    private CancellationTokenSource? _monitorCts;
    private string? _lastDeviceId;
    private string? _monitoredDeviceId;
    private bool _isRefreshing;
    private volatile bool _isReconnecting;
    private DateTime _lastDisconnectTime = DateTime.UtcNow;

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

        messenger.Register<ReconnectRequestedMessage>(this, (_, _) =>
            OnReconnectRequested());

        if (initialSettings.AutoUpdateOnStartup)
        {
            // Await the update check before touching Bluetooth so we do not connect
            // an audio stream that would be immediately torn down by the installer shutdown.
            await CheckAndAutoInstallUpdateAsync();
        }
        else
        {
            _ = Updater.CheckForUpdateAsync();
        }

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

            var deviceIds = new System.Collections.Generic.HashSet<string>(devices.Select(n => n.Id));
            var toRemove = BluetoothDevices.Where(d => !deviceIds.Contains(d.Id)).ToList();
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

            // Skip the settle delay when the Bluetooth link is already physically up — Windows
            // does not need teardown time when the device never actually disconnected.
            var skipSettle = await audioService.IsBluetoothPhysicallyConnectedAsync(SelectedBluetoothDevice.Id);

            if (!skipSettle)
            {
                var timeSinceDisconnect = (DateTime.UtcNow - _lastDisconnectTime).TotalMilliseconds;
                var settleRemaining = ReconnectSettleDelayMs - (int)timeSinceDisconnect;
                if (settleRemaining > 0)
                {
                    await Task.Delay(settleRemaining);
                }
            }

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
        _lastDisconnectTime = DateTime.UtcNow;
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
    /// Opens the native Windows DevicePicker in Bluetooth scan mode and refreshes
    /// the device list when the picker is dismissed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task OpenBluetoothSettingsAsync()
    {
        try
        {
            await devicePickerService.ShowAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DevicePicker] Error: {ex.Message}");
        }
        finally
        {
            await RefreshDevicesAsync();
        }
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
    /// Maximum time the startup auto-update check is allowed to block initialization.
    /// If the GitHub call has not completed within this window, the check is abandoned and
    /// startup continues so users on slow or restrictive networks are not stalled at launch.
    /// The HTTP request continues in the background and will be observed by HttpClient itself.
    /// </summary>
    private const int AutoUpdateCheckTimeoutMs = 10_000;

    /// <summary>
    /// Checks for an available update and, if one is found, downloads and installs it
    /// silently. Used when the user has enabled the AutoUpdateOnStartup setting so the
    /// update is applied without requiring a manual click. Bounded by
    /// <see cref="AutoUpdateCheckTimeoutMs"/> so unreachable networks do not delay startup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CheckAndAutoInstallUpdateAsync()
    {
        try
        {
            var checkTask = Updater.CheckForUpdateAsync();
            var completed = await Task.WhenAny(checkTask, Task.Delay(AutoUpdateCheckTimeoutMs));
            if (completed != checkTask)
            {
                Debug.WriteLine("[AutoUpdate] Check timed out — continuing startup.");
                return;
            }

            await checkTask;
            if (Updater.UpdateAvailable)
            {
                await Updater.InstallUpdateAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AutoUpdate] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the <see cref="ReconnectRequestedMessage"/> by cycling the audio connection
    /// so that a quality registry change takes effect without requiring manual user action.
    /// No-op if no device is selected or if the app is neither connected nor reconnecting.
    /// </summary>
    private void OnReconnectRequested()
    {
        dispatcherService.Invoke(() =>
        {
            if (SelectedBluetoothDevice == null || (!IsConnected && !_isReconnecting))
            {
                return;
            }

            Disconnect();
            _ = ConnectAsync();
        });
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
        return IsConnected || _isReconnecting;
    }

    /// <summary>
    /// Starts a background task that monitors the Bluetooth connection and automatically reconnects.
    /// </summary>
    /// <param name="deviceId">The device identifier to monitor.</param>
    /// <param name="deviceName">The friendly device name for status messages.</param>
    private void StartConnectionMonitor(string deviceId, string deviceName)
    {
        StopConnectionMonitor();
        _monitoredDeviceId = deviceId;
        audioService.ConnectionLost += OnConnectionLostFromService;
        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;

        const int pollDelayMs = 10_000;

        _ = Task.Run(async () =>
        {
            try
            {
                var lastProbeTime = DateTime.UtcNow;

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(pollDelayMs, token);

                    var connected = await audioService.IsBluetoothDeviceConnectedAsync(deviceId);
                    if (connected)
                    {
                        if (_isReconnecting)
                        {
                            dispatcherService.Invoke(() =>
                            {
                                _isReconnecting = false;
                                IsConnected = true;
                                StatusText = "STREAMING ACTIVE";
                            });

                            // Fresh reconnect — reset probe timer so the clock starts from now.
                            lastProbeTime = DateTime.UtcNow;
                        }

                        if ((DateTime.UtcNow - lastProbeTime).TotalMilliseconds >= ConnectionProbeIntervalMs)
                        {
                            lastProbeTime = DateTime.UtcNow;
                            var probeResult = await audioService.ProbeConnectionAsync(deviceId);

                            if (probeResult == true)
                            {
                                // Audio was already flowing — no action needed.
                                continue;
                            }

                            if (probeResult == null)
                            {
                                // A full A2DP reconnect was performed.  The phone's audio stack
                                // does not always resume streaming automatically after the endpoint
                                // is recreated — send the established message so the connection
                                // sound plays and the user is alerted to re-select the PC on their
                                // device if audio does not resume on its own.
                                messenger.Send(new ConnectionEstablishedMessage(deviceName));
                                continue;
                            }

                            // Probe returned false — device not physically connected or reconnect
                            // failed.  Fall through to the reconnect logic below.
                            Debug.WriteLine("[Monitor] Probe deferred to reconnect loop.");
                        }
                        else
                        {
                            continue;
                        }
                    }

                    // Connection lost – enter reconnect loop
                    dispatcherService.Invoke(() =>
                    {
                        _isReconnecting = true;
                        DisconnectCommand.NotifyCanExecuteChanged();
                        StatusText = "RECONNECTING...";
                        IsConnected = false;
                    });

                    try { audioService.Disconnect(); }
                    catch { /* already stopped */ }

                    // Give Windows time to complete Bluetooth teardown before attempting reconnect.
                    // Skip the delay entirely if the device is already physically paired and in range.
                    var physicallyConnectedForSettle = await audioService.IsBluetoothPhysicallyConnectedAsync(deviceId);
                    if (!physicallyConnectedForSettle)
                    {
                        await Task.Delay(ReconnectSettleDelayMs, token);
                    }

                    while (!token.IsCancellationRequested)
                    {
                        var ok = await audioService.ConnectBluetoothAudioAsync(deviceId);
                        if (ok)
                        {
                            dispatcherService.Invoke(() =>
                            {
                                _isReconnecting = false;
                                IsConnected = true;
                                StatusText = "STREAMING ACTIVE";
                            });
                            messenger.Send(new ConnectionEstablishedMessage(deviceName));
                            lastProbeTime = DateTime.UtcNow;
                            break;
                        }

                        await Task.Delay(pollDelayMs, token);
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
        audioService.ConnectionLost -= OnConnectionLostFromService;
        _isReconnecting = false;
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    /// <summary>
    /// Handles the <see cref="IAudioService.ConnectionLost"/> event by cross-checking the actual
    /// device state before updating the UI, guarding against transient audio-endpoint state changes
    /// that do not represent a real Bluetooth disconnect (e.g. Windows audio device switches).
    /// </summary>
    /// <param name="sender">The audio service that raised the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void OnConnectionLostFromService(object? sender, EventArgs e)
    {
        if (!IsConnected || _monitoredDeviceId == null)
        {
            return;
        }

        var stillConnected = await audioService.IsBluetoothDeviceConnectedAsync(_monitoredDeviceId);
        if (stillConnected)
        {
            return;
        }

        dispatcherService.Invoke(() =>
        {
            IsConnected = false;
            _isReconnecting = true;
            DisconnectCommand.NotifyCanExecuteChanged();
            StatusText = "RECONNECTING...";
        });
    }
}
