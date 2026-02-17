using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using EasyBluetoothAudio.Services;
using System.Windows.Threading;
using System.Diagnostics;

namespace EasyBluetoothAudio.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAudioService _audioService;
        private BluetoothDeviceInfo? _selectedBluetoothDevice;
        private bool _isConnected;
        private int _bufferMs = 40;
        private string _statusText = "IDLE";
        private DispatcherTimer _refreshTimer;

        public MainViewModel(IAudioService audioService)
        {
            _audioService = audioService;
            BluetoothDevices = new ObservableCollection<BluetoothDeviceInfo>();
            
            RefreshDevicesCommand = new RelayCommand(async _ => await RefreshDevicesAsync());
            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => CanConnect());
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
            OpenBluetoothSettingsCommand = new RelayCommand(_ => OpenBluetoothSettings());
            
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += async (s, e) => await RefreshDevicesAsync();
            _refreshTimer.Start();

            _ = RefreshDevicesAsync();
        }

        public ObservableCollection<BluetoothDeviceInfo> BluetoothDevices { get; }

        public BluetoothDeviceInfo? SelectedBluetoothDevice
        {
            get => _selectedBluetoothDevice;
            set { _selectedBluetoothDevice = value; OnPropertyChanged(); }
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set { _isConnected = value; OnPropertyChanged(); }
        }

        public int BufferMs
        {
            get => _bufferMs;
            set { _bufferMs = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public ICommand RefreshDevicesCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand OpenBluetoothSettingsCommand { get; }

        private async Task RefreshDevicesAsync()
        {
            var currentSelectedId = SelectedBluetoothDevice?.Id;
            var devices = await _audioService.GetBluetoothDevicesAsync();
            
            BluetoothDevices.Clear();
            foreach (var d in devices) BluetoothDevices.Add(d);
            
            if (currentSelectedId != null)
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault(d => d.Id == currentSelectedId);
            else if (SelectedBluetoothDevice == null)
                SelectedBluetoothDevice = BluetoothDevices.FirstOrDefault();
        }

        private async Task ConnectAsync()
        {
            try
            {
                if (SelectedBluetoothDevice != null)
                {
                    StatusText = "CONNECTING...";
                    await _audioService.ConnectBluetoothAudioAsync(SelectedBluetoothDevice.Id);
                    
                    // Wait for endpoint creation
                    await Task.Delay(2000); 
                    
                    _audioService.StartRouting(SelectedBluetoothDevice.Name, BufferMs);
                    IsConnected = true;
                    StatusText = "STREAMING ACTIVE";
                }
            }
            catch (Exception ex)
            {
                StatusText = "ERROR: " + ex.Message;
                IsConnected = false;
            }
        }

        private void Disconnect()
        {
            _audioService.StopRouting();
            IsConnected = false;
            StatusText = "DISCONNECTED";
        }

        private void OpenBluetoothSettings()
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
        }

        private bool CanConnect() => SelectedBluetoothDevice != null && !IsConnected;
        private bool CanDisconnect() => IsConnected;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
