using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Input;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Core;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// The main view model managing the application state, device list, and connection logic.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IAudioService _audioService;
    private readonly IProcessService _processService;
    private readonly ISettingsService _settingsService;
    private readonly IUpdateService _updateService;
    private readonly SettingsViewModel? _settingsViewModel;
    private BluetoothDevice? _selectedBluetoothDevice;
    private bool _isConnected;
    private bool _isBusy;
    private bool _isSettingsOpen;
    private bool _autoConnect;
    private bool _isCheckingForUpdate;
    private bool _updateAvailable;
    private UpdateInfo? _latestUpdate;
    private int _bufferMs = (int)AudioDelay.Medium;
    private string _statusText = "IDLE";
    private string? _lastDeviceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="audioService">The audio service for device discovery and connection.</param>
    /// <param name="processService">The process service for launching system URIs.</param>
    /// <param name="settingsService">The settings service for loading persisted preferences.</param>
    /// <param name="updateService">The update service for checking and applying GitHub releases.</param>
    /// <param name="settingsViewModel">The settings view model, lazily injected after construction.</param>
    public MainViewModel(IAudioService audioService, IProcessService processService, ISettingsService settingsService, IUpdateService updateService, SettingsViewModel settingsViewModel)
    {
        _audioService = audioService;
        _processService = processService;
        _settingsService = settingsService;
        _updateService = updateService;
        _settingsViewModel = settingsViewModel;

        BluetoothDevices = new ObservableCollection<BluetoothDevice>();

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => CanConnect());
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
        OpenBluetoothSettingsCommand = new RelayCommand(_ => OpenBluetoothSettings());
        RefreshCommand = new AsyncRelayCommand(RefreshDevicesAsync);
        OpenSettingsCommand = new RelayCommand(_ => IsSettingsOpen = !IsSettingsOpen);
        CheckForUpdateCommand = new AsyncRelayCommand(CheckForUpdateAsync);
        InstallUpdateCommand = new AsyncRelayCommand(InstallUpdateAsync, CanInstallUpdate);

        // Tray Commands
        OpenCommand = new RelayCommand(_ => RequestShow?.Invoke());
        ExitCommand = new RelayCommand(_ => RequestExit?.Invoke());

        ApplySettings(_settingsService.Load());
        AppVersion = ResolveAppVersion();
        _ = SafeRefreshDevicesAsync();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += async (s, e) => await SafeRefreshDevicesAsync();
        timer.Start();

        // Fire-and-forget startup update check — runs in the background so it
        // never delays the UI. Any exception is swallowed inside CheckForUpdateAsync.
        _ = Task.Run(() => CheckForUpdateAsync());
    }

    /// <summary>
    /// Gets the application version string.
    /// </summary>
    public string AppVersion { get; }

    /// <summary>
    /// Gets the settings view model used by the Settings panel.
    /// </summary>
    public SettingsViewModel? SettingsViewModel => _settingsViewModel;

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
                CommandManager.InvalidateRequerySuggested();
                if (value != null)
                    PersistLastDeviceId(value.Id);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an active audio connection exists.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set { _isConnected = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
    }

    /// <summary>
    /// Gets a value indicating whether a connection operation is in progress.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
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
    /// Gets or sets the audio buffer size in milliseconds.
    /// </summary>
    public int BufferMs
    {
        get => _bufferMs;
        set { _bufferMs = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Gets or sets the status text displayed in the UI.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Command to initiate a connection to the selected device.
    /// </summary>
    public ICommand ConnectCommand { get; }

    /// <summary>
    /// Command to disconnect the current device.
    /// </summary>
    public ICommand DisconnectCommand { get; }

    /// <summary>
    /// Command to open Windows Bluetooth settings.
    /// </summary>
    public ICommand OpenBluetoothSettingsCommand { get; }

    /// <summary>
    /// Command to show the main window from the tray.
    /// </summary>
    public ICommand OpenCommand { get; }

    /// <summary>
    /// Gets the command to toggle the Settings panel open or closed.
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    /// <summary>
    /// Gets the command that checks GitHub for a newer release.
    /// </summary>
    public ICommand CheckForUpdateCommand { get; }

    /// <summary>
    /// Gets the command that downloads and silently installs the latest release.
    /// Only executable when <see cref="UpdateAvailable"/> is <see langword="true"/>.
    /// </summary>
    public ICommand InstallUpdateCommand { get; }

    /// <summary>
    /// Gets a value indicating whether an update check is currently in progress.
    /// </summary>
    public bool IsCheckingForUpdate
    {
        get => _isCheckingForUpdate;
        private set => SetProperty(ref _isCheckingForUpdate, value);
    }

    /// <summary>
    /// Gets a value indicating whether a newer version is available on GitHub.
    /// </summary>
    public bool UpdateAvailable
    {
        get => _updateAvailable;
        private set
        {
            if (SetProperty(ref _updateAvailable, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Gets the latest <see cref="UpdateInfo"/> returned by the most recent update check,
    /// or <see langword="null"/> if no update is available.
    /// </summary>
    public UpdateInfo? LatestUpdate
    {
        get => _latestUpdate;
        private set => SetProperty(ref _latestUpdate, value);
    }

    /// <summary>
    /// Command to exit the application completely.
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
    /// Command to refresh the device list.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// Refreshes the Bluetooth device list from all available devices, preserving the current selection.
    /// Exposed publicly for testability (no device-type filtering applied).
    /// </summary>
    public async Task RefreshDevicesAsync()
    {
        try
        {
            var currentSelectedId = SelectedBluetoothDevice?.Id ?? _lastDeviceId;
            var devices = (await _audioService.GetBluetoothDevicesAsync()).ToList();

            BluetoothDevices.Clear();
            foreach (var d in devices)
                BluetoothDevices.Add(d);

            if (currentSelectedId != null)
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault(d => d.Id == currentSelectedId);

            if (SelectedBluetoothDevice == null)
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RefreshDevices] Error: {ex.Message}");
            StatusText = "SCAN ERROR";
        }
    }

    private async Task SafeRefreshDevicesAsync()
    {
        try
        {
            var currentSelectedId = SelectedBluetoothDevice?.Id ?? _lastDeviceId;
            var allDevices = (await _audioService.GetBluetoothDevicesAsync()).ToList();
            var devices = allDevices.Where(x => x.IsPhoneOrComputer).ToList();

            BluetoothDevices.Clear();
            foreach (var d in devices)
                BluetoothDevices.Add(d);

            if (currentSelectedId != null)
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault(d => d.Id == currentSelectedId);

            if (SelectedBluetoothDevice == null)
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault();

            if (AutoConnect && SelectedBluetoothDevice != null && !IsConnected && !IsBusy)
                await ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SafeRefresh] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Connects to the currently selected Bluetooth device.
    /// Exposed publicly for testability.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (SelectedBluetoothDevice == null) return;

        try
        {
            IsBusy = true;
            StatusText = $"CONNECTING TO {SelectedBluetoothDevice.Name}...";

            var ok = await _audioService.ConnectBluetoothAudioAsync(SelectedBluetoothDevice.Id);
            if (!ok)
            {
                StatusText = "BT CONNECT FAILED";
                return;
            }

            StatusText = "WAITING FOR AUDIO ENDPOINT...";
            await _audioService.StartRoutingAsync(SelectedBluetoothDevice.Name, BufferMs);

            IsConnected = true;
            StatusText = "STREAMING ACTIVE";
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
    /// Disconnects the current audio session.
    /// Exposed publicly for testability.
    /// </summary>
    public void Disconnect()
    {
        try
        {
            _audioService.StopRouting();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Disconnect] Error: {ex.Message}");
        }

        IsConnected = false;
        StatusText = "DISCONNECTED";
    }

    private void OpenBluetoothSettings()
    {
        _processService.OpenUri("ms-settings:bluetooth");
    }

    /// <summary>
    /// Queries GitHub for the latest release and updates <see cref="UpdateAvailable"/>.
    /// Safe to call from any thread; marshals property updates to the UI thread via
    /// <see cref="System.Windows.Application.Current"/> dispatcher.
    /// </summary>
    internal async Task CheckForUpdateAsync()
    {
        if (IsCheckingForUpdate) return;

        try
        {
            IsCheckingForUpdate = true;
            var info = await _updateService.CheckForUpdateAsync();

            LatestUpdate = info;
            UpdateAvailable = info is not null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CheckForUpdate] Error: {ex.Message}");
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }

    /// <summary>
    /// Downloads and silently installs the latest release, then shuts down the app.
    /// </summary>
    internal async Task InstallUpdateAsync()
    {
        if (_latestUpdate is null) return;

        try
        {
            StatusText = $"DOWNLOADING UPDATE {_latestUpdate.TagName}...";
            await _updateService.DownloadAndInstallAsync(_latestUpdate);
            // Application.Shutdown() is called inside DownloadAndInstallAsync after
            // the installer process has been spawned.
        }
        catch (Exception ex)
        {
            StatusText = "UPDATE FAILED";
            Debug.WriteLine($"[InstallUpdate] Error: {ex.Message}");
        }
    }

    private bool CanInstallUpdate() => UpdateAvailable && _latestUpdate is not null && !IsCheckingForUpdate;

    private bool CanConnect() => SelectedBluetoothDevice != null && !IsConnected && !IsBusy;
    private bool CanDisconnect() => IsConnected;

    private void ApplySettings(AppSettings settings)
    {
        _bufferMs = (int)settings.Delay;
        _autoConnect = settings.AutoConnect;
        _lastDeviceId = settings.LastDeviceId;
    }

    private void PersistLastDeviceId(string deviceId)
    {
        try
        {
            var settings = _settingsService.Load();
            settings.LastDeviceId = deviceId;
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PersistLastDevice] Error: {ex.Message}");
        }
    }

    private static string ResolveAppVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var gitVersion = assembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(gitVersion))
        {
            var version = assembly?.GetName().Version;
            return version != null ? $"v.{version.Major}.{version.Minor}.{version.Build}" : "v.?.?.?";
        }

        return $"v.{gitVersion}";
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
