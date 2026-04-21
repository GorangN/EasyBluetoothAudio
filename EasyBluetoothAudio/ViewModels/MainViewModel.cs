using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Services.Interfaces;
using Hardcodet.Wpf.TaskbarNotification;

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
    /// Number of automatic reconnect attempts that may run after a real connection loss
    /// or failed initial connect before the UI falls back to explicit manual reconnect.
    /// </summary>
    internal const int AutoReconnectAttemptLimit = 2;

    /// <summary>
    /// Delay in milliseconds between automatic reconnect attempts.
    /// </summary>
    internal const int AutoReconnectDelayMs = 3_000;

    /// <summary>
    /// Poll interval in milliseconds for verifying that the currently monitored device is still connected.
    /// </summary>
    internal const int MonitorPollDelayMs = 10_000;

    /// <summary>
    /// Maximum time the startup auto-update check is allowed to block initialization.
    /// If the GitHub call has not completed within this window, the check is abandoned and
    /// startup continues so users on slow or restrictive networks are not stalled at launch.
    /// The HTTP request continues in the background and will be observed by HttpClient itself.
    /// </summary>
    private const int AutoUpdateCheckTimeoutMs = 10_000;

    private CancellationTokenSource? _connectAttemptCts;
    private CancellationTokenSource? _monitorCts;
    private string? _lastDeviceId;
    private string? _monitoredDeviceId;
    private string? _monitoredDeviceName;
    private bool _isRefreshing;
    private volatile bool _isReconnecting;
    private int _autoReconnectInFlight;
    private int _connectAttemptGeneration;
    private int _monitorGeneration;
    private bool _hasShownReconnectBalloon;

    /// <summary>
    /// Gets or sets the currently selected Bluetooth device.
    /// Persists the device ID to settings when changed by the user.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReconnectCommand))]
    private BluetoothDevice? _selectedBluetoothDevice;

    /// <summary>
    /// Gets a value indicating whether an active audio connection exists.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isConnected;

    /// <summary>
    /// Gets a value indicating whether the audio stream is lost but the physical Bluetooth
    /// link is still present, so the UI should guide the user to manual reconnect.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    private bool _isRecoverableConnectionLoss;

    /// <summary>
    /// Gets a value indicating whether a connection operation is in progress.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReconnectCommand))]
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
    /// Gets a value indicating whether the reconnect/disconnect action pair should be shown.
    /// </summary>
    public bool ShowReconnectActions
    {
        get
        {
            return IsConnected || IsRecoverableConnectionLoss;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the full connect action should be shown.
    /// </summary>
    public bool ShowConnectAction
    {
        get
        {
            return !ShowReconnectActions;
        }
    }

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
    [RelayCommand]
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

            var deviceIds = new System.Collections.Generic.HashSet<string>(devices.Select(device => device.Id));
            var toRemove = BluetoothDevices.Where(device => !deviceIds.Contains(device.Id)).ToList();
            foreach (var item in toRemove)
            {
                BluetoothDevices.Remove(item);
            }

            if (SelectedBluetoothDevice == null && currentSelectedId != null)
            {
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault(device => device.Id == currentSelectedId);
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
    /// Performs a bounded automatic retry if the initial connect fails.
    /// </summary>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    internal async Task ConnectAsync()
    {
        if (SelectedBluetoothDevice == null)
        {
            return;
        }

        var deviceId = SelectedBluetoothDevice.Id;
        var deviceName = SelectedBluetoothDevice.Name;
        var (connectAttemptCts, cancellationToken, connectAttemptGeneration) = BeginConnectAttempt();

        try
        {
            IsBusy = true;
            ResetReconnectGuidance();
            StopConnectionMonitor();
            IsRecoverableConnectionLoss = false;
            StatusText = $"CONNECTING TO {deviceName}...";

            var connected = await ConnectWithAutoReconnectAsync(
                deviceId,
                deviceName,
                cancellationToken,
                () => IsCurrentConnectAttempt(cancellationToken, connectAttemptGeneration));

            if (!IsCurrentConnectAttempt(cancellationToken, connectAttemptGeneration))
            {
                return;
            }

            if (!connected)
            {
                await ApplyManualFallbackStateAsync(
                    deviceId,
                    showGuidanceBalloon: true,
                    () => IsCurrentConnectAttempt(cancellationToken, connectAttemptGeneration));
                return;
            }

            StartConnectionMonitor(deviceId, deviceName);
        }
        catch (Exception ex)
        {
            StatusText = "ERROR: " + ex.Message;
            IsConnected = false;
        }
        finally
        {
            CompleteConnectAttempt(connectAttemptCts);
            IsBusy = false;
        }
    }

    /// <summary>
    /// Disconnects the current Bluetooth audio device and clears any recoverable fallback state.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    internal void Disconnect()
    {
        CancelPendingConnectAttempt();
        StopConnectionMonitor();

        try
        {
            audioService.Disconnect("user");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Disconnect] Error: {ex.Message}");
        }

        ApplyDisconnectedState("DISCONNECTED");
    }

    /// <summary>
    /// Performs a one-shot audio-route recycle for the currently selected device.
    /// This is the primary manual reconnect action exposed in the UI and tray when the
    /// audio route is active or the physical Bluetooth link is still recoverable.
    /// </summary>
    /// <returns>A task representing the asynchronous reconnect operation.</returns>
    [RelayCommand(CanExecute = nameof(CanReconnect))]
    internal async Task ReconnectAsync()
    {
        if (SelectedBluetoothDevice == null || !CanReconnect())
        {
            return;
        }

        var deviceId = SelectedBluetoothDevice.Id;
        var deviceName = SelectedBluetoothDevice.Name;

        try
        {
            IsBusy = true;
            ResetReconnectGuidance();
            StopConnectionMonitor();
            IsRecoverableConnectionLoss = false;
            StatusText = "RECONNECTING...";

            if (IsConnected || _isReconnecting)
            {
                try
                {
                    audioService.Disconnect("manual-recover", preserveDisconnectTimestamp: true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Reconnect] Disconnect error: {ex.Message}");
                }
            }

            var connected = await audioService.ConnectBluetoothAudioAsync(deviceId);
            if (!connected)
            {
                await ApplyManualFallbackStateAsync(deviceId, showGuidanceBalloon: false);
                return;
            }

            ApplyConnectedState(deviceName);
            StartConnectionMonitor(deviceId, deviceName);
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
    /// Opens the system Settings panel.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    /// <summary>
    /// Opens the native Windows DevicePicker in Bluetooth scan mode and refreshes
    /// the device list when the picker is dismissed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
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
    [RelayCommand]
    private void Open()
    {
        RequestShow?.Invoke();
    }

    /// <summary>
    /// Requests the application to exit.
    /// </summary>
    [RelayCommand]
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
    /// Raises derived action-visibility notifications when the connected state changes.
    /// </summary>
    /// <param name="value">The updated connected state.</param>
    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowReconnectActions));
        OnPropertyChanged(nameof(ShowConnectAction));
    }

    /// <summary>
    /// Raises derived action-visibility notifications when the recoverable-loss state changes.
    /// </summary>
    /// <param name="value">The updated recoverable-loss state.</param>
    partial void OnIsRecoverableConnectionLossChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowReconnectActions));
        OnPropertyChanged(nameof(ShowConnectAction));
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
                Debug.WriteLine("[AutoUpdate] Check timed out - continuing startup.");
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
    /// so that a quality registry change takes effect without requiring a full reconnect.
    /// No-op if no device is selected or if no active/recoverable audio route exists.
    /// </summary>
    private void OnReconnectRequested()
    {
        dispatcherService.Invoke(() =>
        {
            if (SelectedBluetoothDevice == null || (!IsConnected && !IsRecoverableConnectionLoss))
            {
                return;
            }

            _ = ReconnectAsync();
        });
    }

    /// <summary>
    /// Determines whether the connect command can execute.
    /// </summary>
    /// <returns><see langword="true"/> if a device is selected, no recoverable route exists, and no blocking operation is running.</returns>
    private bool CanConnect()
    {
        return SelectedBluetoothDevice != null && !IsConnected && !IsRecoverableConnectionLoss && !IsBusy;
    }

    /// <summary>
    /// Determines whether the disconnect command can execute.
    /// </summary>
    /// <returns><see langword="true"/> if the app has an active, recoverable, or in-flight reconnect route to clear.</returns>
    private bool CanDisconnect()
    {
        return IsConnected || IsRecoverableConnectionLoss || _isReconnecting;
    }

    /// <summary>
    /// Determines whether the explicit manual reconnect command can execute.
    /// </summary>
    /// <returns><see langword="true"/> if a device is selected, no blocking operation is running, and a recoverable route exists.</returns>
    private bool CanReconnect()
    {
        return SelectedBluetoothDevice != null && !IsBusy && (IsConnected || IsRecoverableConnectionLoss);
    }

    /// <summary>
    /// Starts a new user-triggered connect attempt scope and cancels any previous pending
    /// initial-connect retry sequence that should no longer be allowed to apply UI state.
    /// </summary>
    /// <returns>The cancellation source, token, and generation stamp for the new connect attempt.</returns>
    private (CancellationTokenSource ConnectAttemptCts, CancellationToken CancellationToken, int ConnectAttemptGeneration) BeginConnectAttempt()
    {
        CancelPendingConnectAttempt();
        var connectAttemptCts = new CancellationTokenSource();
        _connectAttemptCts = connectAttemptCts;
        return (connectAttemptCts, connectAttemptCts.Token, _connectAttemptGeneration);
    }

    /// <summary>
    /// Cancels any pending user-triggered initial connect attempt so stale retries or fallback
    /// state updates cannot outlive a later disconnect or replacement connect operation.
    /// </summary>
    private void CancelPendingConnectAttempt()
    {
        Interlocked.Increment(ref _connectAttemptGeneration);
        _connectAttemptCts?.Cancel();
        _connectAttemptCts?.Dispose();
        _connectAttemptCts = null;
    }

    /// <summary>
    /// Clears the active connect-attempt scope if it still belongs to the completing operation.
    /// </summary>
    /// <param name="connectAttemptCts">The cancellation source captured by the completing connect attempt.</param>
    private void CompleteConnectAttempt(CancellationTokenSource connectAttemptCts)
    {
        if (!ReferenceEquals(_connectAttemptCts, connectAttemptCts))
        {
            return;
        }

        connectAttemptCts.Dispose();
        _connectAttemptCts = null;
    }

    /// <summary>
    /// Determines whether the supplied connect-attempt token and generation still belong to the
    /// latest user-triggered initial connect operation.
    /// </summary>
    /// <param name="cancellationToken">The token captured for the connect attempt.</param>
    /// <param name="connectAttemptGeneration">The generation stamp captured for the connect attempt.</param>
    /// <returns><see langword="true"/> if the connect attempt is still current; otherwise <see langword="false"/>.</returns>
    private bool IsCurrentConnectAttempt(CancellationToken cancellationToken, int connectAttemptGeneration)
    {
        return !cancellationToken.IsCancellationRequested && connectAttemptGeneration == _connectAttemptGeneration;
    }

    /// <summary>
    /// Starts a background task that monitors the currently connected device and only reacts
    /// to confirmed connection loss. Idle silence does not trigger any background reconnect.
    /// </summary>
    /// <param name="deviceId">The device identifier to monitor.</param>
    /// <param name="deviceName">The friendly device name for status messages.</param>
    private void StartConnectionMonitor(string deviceId, string deviceName)
    {
        StopConnectionMonitor();

        _monitoredDeviceId = deviceId;
        _monitoredDeviceName = deviceName;
        audioService.ConnectionLost += OnConnectionLostFromService;
        _monitorCts = new CancellationTokenSource();
        var cancellationToken = _monitorCts.Token;
        var monitorGeneration = _monitorGeneration;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(MonitorPollDelayMs, cancellationToken);

                    var connected = await audioService.IsBluetoothDeviceConnectedAsync(deviceId);
                    if (connected)
                    {
                        continue;
                    }

                    await HandleConfirmedConnectionLossAsync(
                        deviceId,
                        deviceName,
                        "monitor-detected-loss",
                        cancellationToken,
                        monitorGeneration);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Monitor] Unexpected error: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Stops the connection monitor background task and clears the current monitored device metadata.
    /// </summary>
    private void StopConnectionMonitor()
    {
        Interlocked.Increment(ref _monitorGeneration);
        audioService.ConnectionLost -= OnConnectionLostFromService;
        _monitoredDeviceId = null;
        _monitoredDeviceName = null;
        _isReconnecting = false;
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Attempts an initial connection and, if that fails, performs a bounded automatic reconnect sequence.
    /// </summary>
    /// <param name="deviceId">The device identifier to connect.</param>
    /// <param name="deviceName">The friendly device name for status updates and notifications.</param>
    /// <param name="cancellationToken">A cancellation token that can abort the retry loop.</param>
    /// <param name="shouldApplyResult">A guard used to discard stale reconnect results after the monitor has been stopped.</param>
    /// <returns><see langword="true"/> if the device is connected; otherwise <see langword="false"/>.</returns>
    private async Task<bool> ConnectWithAutoReconnectAsync(
        string deviceId,
        string deviceName,
        CancellationToken cancellationToken,
        Func<bool> shouldApplyResult)
    {
        var connected = await audioService.ConnectBluetoothAudioAsync(deviceId);
        if (connected)
        {
            if (!shouldApplyResult())
            {
                audioService.Disconnect("stale-auto-reconnect");
                return false;
            }

            ApplyConnectedState(deviceName);
            return true;
        }

        return await TryAutoReconnectAsync(deviceId, deviceName, cancellationToken, shouldApplyResult);
    }

    /// <summary>
    /// Performs the bounded automatic reconnect sequence used for real connection loss
    /// and failed initial connects.
    /// </summary>
    /// <param name="deviceId">The device identifier to reconnect.</param>
    /// <param name="deviceName">The friendly device name for status updates.</param>
    /// <param name="cancellationToken">A cancellation token that can abort the retry loop.</param>
    /// <param name="shouldApplyResult">A guard used to discard stale reconnect results after the monitor has been stopped.</param>
    /// <returns><see langword="true"/> if a retry restored the connection; otherwise <see langword="false"/>.</returns>
    private async Task<bool> TryAutoReconnectAsync(
        string deviceId,
        string deviceName,
        CancellationToken cancellationToken,
        Func<bool> shouldApplyResult)
    {
        dispatcherService.Invoke(ApplyReconnectingState);

        for (var attempt = 0; attempt < AutoReconnectAttemptLimit; attempt++)
        {
            if (cancellationToken.IsCancellationRequested || !shouldApplyResult())
            {
                return false;
            }

            try
            {
                await Task.Delay(AutoReconnectDelayMs, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (!shouldApplyResult())
            {
                return false;
            }

            var connected = await audioService.ConnectBluetoothAudioAsync(deviceId);
            if (!connected)
            {
                continue;
            }

            if (!shouldApplyResult())
            {
                audioService.Disconnect("stale-auto-reconnect");
                return false;
            }

            dispatcherService.Invoke(() => ApplyConnectedState(deviceName));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles a confirmed connection loss by running the bounded automatic reconnect sequence.
    /// When the retry budget is exhausted, the monitor is stopped and the UI falls back to the
    /// correct manual action for the remaining Bluetooth state.
    /// </summary>
    /// <param name="deviceId">The device identifier to reconnect.</param>
    /// <param name="deviceName">The friendly device name for status updates.</param>
    /// <param name="disconnectReason">The disconnect reason passed through to the audio service.</param>
    /// <param name="cancellationToken">A cancellation token for the current monitor run.</param>
    /// <param name="monitorGeneration">The generation stamp of the current monitor run.</param>
    /// <returns>A task representing the asynchronous reconnect operation.</returns>
    private async Task HandleConfirmedConnectionLossAsync(
        string deviceId,
        string deviceName,
        string disconnectReason,
        CancellationToken cancellationToken,
        int monitorGeneration)
    {
        if (Interlocked.CompareExchange(ref _autoReconnectInFlight, 1, 0) != 0)
        {
            return;
        }

        try
        {
            try
            {
                audioService.Disconnect(disconnectReason);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Reconnect] Disconnect error: {ex.Message}");
            }

            var connected = await TryAutoReconnectAsync(
                deviceId,
                deviceName,
                cancellationToken,
                () => !cancellationToken.IsCancellationRequested && monitorGeneration == _monitorGeneration);

            if (connected || cancellationToken.IsCancellationRequested || monitorGeneration != _monitorGeneration)
            {
                return;
            }

            var isPhysicallyConnected = await audioService.IsBluetoothPhysicallyConnectedAsync(deviceId);
            if (cancellationToken.IsCancellationRequested || monitorGeneration != _monitorGeneration)
            {
                return;
            }

            dispatcherService.Invoke(() =>
            {
                ApplyReconnectRequiredState(isPhysicallyConnected);
                ShowReconnectGuidanceBalloon(isPhysicallyConnected);
            });
            StopConnectionMonitor();
        }
        finally
        {
            Interlocked.Exchange(ref _autoReconnectInFlight, 0);
        }
    }

    /// <summary>
    /// Applies the connected UI state and emits the connection-established message.
    /// </summary>
    /// <param name="deviceName">The friendly device name of the connected source.</param>
    private void ApplyConnectedState(string deviceName)
    {
        ResetReconnectGuidance();
        _isReconnecting = false;
        IsConnected = true;
        IsRecoverableConnectionLoss = false;
        StatusText = "STREAMING ACTIVE";
        DisconnectCommand.NotifyCanExecuteChanged();
        messenger.Send(new ConnectionEstablishedMessage(deviceName));
    }

    /// <summary>
    /// Applies the transient reconnecting UI state used during bounded automatic reconnect.
    /// </summary>
    private void ApplyReconnectingState()
    {
        _isReconnecting = true;
        IsConnected = false;
        IsRecoverableConnectionLoss = false;
        StatusText = "RECONNECTING...";
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Applies the stable failure state used after the automatic reconnect budget is exhausted.
    /// </summary>
    /// <param name="isPhysicallyConnected"><see langword="true"/> when the Bluetooth link still exists and the UI should guide to manual reconnect.</param>
    private void ApplyReconnectRequiredState(bool isPhysicallyConnected)
    {
        _isReconnecting = false;
        IsConnected = false;
        IsRecoverableConnectionLoss = isPhysicallyConnected;
        StatusText = isPhysicallyConnected
            ? "AUDIO LOST - CLICK RECONNECT"
            : "AUDIO LOST - CLICK CONNECT";
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Applies the fully disconnected state after the user intentionally disconnects or clears
    /// a recoverable audio-loss prompt.
    /// </summary>
    /// <param name="statusText">The status text to show in the UI.</param>
    private void ApplyDisconnectedState(string statusText)
    {
        ResetReconnectGuidance();
        _isReconnecting = false;
        IsConnected = false;
        IsRecoverableConnectionLoss = false;
        StatusText = statusText;
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Applies the correct manual fallback state after a connect or reconnect attempt fails.
    /// </summary>
    /// <param name="deviceId">The device identifier whose physical Bluetooth state should be checked.</param>
    /// <param name="showGuidanceBalloon"><see langword="true"/> to show the one-shot tray guidance balloon.</param>
    /// <returns>A task representing the asynchronous state update.</returns>
    private async Task ApplyManualFallbackStateAsync(
        string deviceId,
        bool showGuidanceBalloon,
        Func<bool>? shouldApplyResult = null)
    {
        var canApplyResult = shouldApplyResult ?? (() => true);
        var isPhysicallyConnected = await audioService.IsBluetoothPhysicallyConnectedAsync(deviceId);
        if (!canApplyResult())
        {
            return;
        }

        ApplyReconnectRequiredState(isPhysicallyConnected);

        if (showGuidanceBalloon)
        {
            ShowReconnectGuidanceBalloon(isPhysicallyConnected);
        }
    }

    /// <summary>
    /// Clears the one-shot reconnect guidance notification gate for a fresh connection attempt.
    /// </summary>
    private void ResetReconnectGuidance()
    {
        _hasShownReconnectBalloon = false;
    }

    /// <summary>
    /// Shows the one-shot tray balloon that explains the manual reconnect action after the
    /// bounded automatic reconnect budget has been exhausted.
    /// </summary>
    /// <param name="guideToReconnect"><see langword="true"/> to guide the user to the manual reconnect action; otherwise guide them to a full connect.</param>
    private void ShowReconnectGuidanceBalloon(bool guideToReconnect)
    {
        if (_hasShownReconnectBalloon)
        {
            return;
        }

        _hasShownReconnectBalloon = true;
        var actionLabel = guideToReconnect ? "Reconnect" : "Connect";
        messenger.Send(new ShowBalloonRequestedMessage(new BalloonContent(
            "Bluetooth Audio",
            $"Use {actionLabel} or toggle the route on the iPhone.",
            BalloonIcon.Warning)));
    }

    /// <summary>
    /// Reacts to the audio service's immediate connection-lost signal and starts the bounded
    /// reconnect flow without waiting for the next poll cycle, but only after a cross-check
    /// confirms the device is actually disconnected.
    /// </summary>
    /// <param name="sender">The audio service that raised the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void OnConnectionLostFromService(object? sender, EventArgs e)
    {
        if (_monitorCts == null || _monitoredDeviceId == null || _monitoredDeviceName == null)
        {
            return;
        }

        var cancellationToken = _monitorCts.Token;
        var deviceId = _monitoredDeviceId;
        var deviceName = _monitoredDeviceName;
        var monitorGeneration = _monitorGeneration;

        try
        {
            var stillConnected = await audioService.IsBluetoothDeviceConnectedAsync(deviceId);
            if (stillConnected || cancellationToken.IsCancellationRequested || monitorGeneration != _monitorGeneration)
            {
                return;
            }

            await HandleConfirmedConnectionLossAsync(
                deviceId,
                deviceName,
                "service-connection-lost",
                cancellationToken,
                monitorGeneration);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || monitorGeneration != _monitorGeneration)
        {
        }
    }
}
