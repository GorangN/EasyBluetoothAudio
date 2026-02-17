using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
    private BluetoothDevice? _selectedBluetoothDevice;
    private bool _isConnected;
    private bool _isBusy;
    private int _bufferMs = 40;
    private string _statusText = "IDLE";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="audioService">The audio service instance.</param>
    public MainViewModel(IAudioService audioService)
    {
        _audioService = audioService;
        BluetoothDevices = new ObservableCollection<BluetoothDevice>();

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => CanConnect());
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
        OpenBluetoothSettingsCommand = new RelayCommand(_ => OpenBluetoothSettings());
        
        // Tray Commands
        OpenCommand = new RelayCommand(_ => RequestShow?.Invoke());
        ExitCommand = new RelayCommand(_ => RequestExit?.Invoke());

        _ = SafeRefreshDevicesAsync();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += async (s, e) => await SafeRefreshDevicesAsync();
        timer.Start();

        AppVersion = GetAppVersion();
    }

    /// <summary>
    /// Gets the application version string.
    /// </summary>
    public string AppVersion { get; }

    private string GetAppVersion()
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

    /// <summary>
    /// Gets the collection of discovered Bluetooth devices.
    /// </summary>
    public ObservableCollection<BluetoothDevice> BluetoothDevices { get; }

    /// <summary>
    /// Gets or sets the currently selected Bluetooth device.
    /// </summary>
    public BluetoothDevice? SelectedBluetoothDevice
    {
        get => _selectedBluetoothDevice;
        set
        {
            _selectedBluetoothDevice = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
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
    public System.Windows.Input.ICommand ConnectCommand { get; }

    /// <summary>
    /// Command to disconnect the current device.
    /// </summary>
    public System.Windows.Input.ICommand DisconnectCommand { get; }

    /// <summary>
    /// Command to open Windows Bluetooth settings.
    /// </summary>
    public System.Windows.Input.ICommand OpenBluetoothSettingsCommand { get; }

    /// <summary>
    /// Command to show the main window from the tray.
    /// </summary>
    public System.Windows.Input.ICommand OpenCommand { get; }

    /// <summary>
    /// Command to exit the application completely.
    /// </summary>
    public System.Windows.Input.ICommand ExitCommand { get; }

    /// <summary>
    /// Raised when the ViewModel requests the View to show itself.
    /// </summary>
    public event Action? RequestShow;

    /// <summary>
    /// Raised when the ViewModel requests the application to exit.
    /// </summary>
    public event Action? RequestExit;

    private async Task SafeRefreshDevicesAsync()
    {
        try
        {
            var currentSelectedId = SelectedBluetoothDevice?.Id;
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

    private async Task ConnectAsync()
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

    private void Disconnect()
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
        Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
    }

    private bool CanConnect() => SelectedBluetoothDevice != null && !IsConnected && !IsBusy;
    private bool CanDisconnect() => IsConnected;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
