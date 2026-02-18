using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
    private BluetoothDevice? _selectedBluetoothDevice;
    private AudioDevice? _selectedOutputDevice;
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
    public MainViewModel(IAudioService audioService, IProcessService processService)
    {
        _audioService = audioService;
        _processService = processService;
        BluetoothDevices = new ObservableCollection<BluetoothDevice>();
        OutputDevices = new ObservableCollection<AudioDevice>();

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
        OpenBluetoothSettingsCommand = new RelayCommand(_ => OpenBluetoothSettings());
        RefreshCommand = new AsyncRelayCommand(RefreshDevicesAsync);

        OpenCommand = new RelayCommand(_ => RequestShow?.Invoke());
        ExitCommand = new RelayCommand(_ => RequestExit?.Invoke());

        AppVersion = ResolveAppVersion();
    }

    /// <summary>
    /// Gets the application version string derived from assembly metadata.
    /// </summary>
    public string AppVersion { get; }

    /// <summary>
    /// Gets the observable collection of discovered Bluetooth devices.
    /// </summary>
    public ObservableCollection<BluetoothDevice> BluetoothDevices { get; }

    /// <summary>
    /// Gets the observable collection of available output devices.
    /// </summary>
    public ObservableCollection<AudioDevice> OutputDevices { get; }

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
                CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// Gets or sets the currently selected audio output device.
    /// </summary>
    public AudioDevice? SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set => SetProperty(ref _selectedOutputDevice, value);
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
                CommandManager.InvalidateRequerySuggested();
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
                CommandManager.InvalidateRequerySuggested();
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
    /// Gets or sets the audio buffer size in milliseconds.
    /// </summary>
    public int BufferMs
    {
        get => _bufferMs;
        set => SetProperty(ref _bufferMs, value);
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
    /// Refreshes the Bluetooth device list from the audio service while preserving the current selection.
    /// </summary>
    public async Task RefreshDevicesAsync()
    {
        try
        {
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

            // Refresh Output Devices
            var currentOutputId = SelectedOutputDevice?.Id;
            var outputDevices = _audioService.GetOutputDevices().ToList();

            // Add default option
            outputDevices.Insert(0, new AudioDevice { Name = "Default Audio Output", Id = string.Empty });

            OutputDevices.Clear();
            foreach (var device in outputDevices)
            {
                OutputDevices.Add(device);
            }

            if (currentOutputId != null)
            {
                SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.Id == currentOutputId);
            }

            if (SelectedOutputDevice == null)
            {
                SelectedOutputDevice = OutputDevices.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RefreshDevices] Error: {ex.Message}");
            StatusText = "SCAN ERROR";
        }
    }

    internal async Task ConnectAsync()
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
            await _audioService.StartRoutingAsync(SelectedBluetoothDevice.Name, SelectedOutputDevice?.Id, BufferMs);

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

    internal void Disconnect()
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
}
