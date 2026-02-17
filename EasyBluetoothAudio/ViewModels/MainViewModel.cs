using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using EasyBluetoothAudio.Services;

namespace EasyBluetoothAudio.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAudioService _audioService;
        private BluetoothDeviceInfo? _selectedBluetoothDevice;
        private bool _isConnected;
        private bool _isBusy;
        private int _bufferMs = 40;
        private string _statusText = "IDLE";

        public MainViewModel(IAudioService audioService)
        {
            _audioService = audioService;
            BluetoothDevices = new ObservableCollection<BluetoothDeviceInfo>();

            ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => CanConnect());
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => CanDisconnect());
            OpenBluetoothSettingsCommand = new RelayCommand(_ => OpenBluetoothSettings());

            // Initial device load
            _ = SafeRefreshDevicesAsync();

            // Periodic refresh every 5 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += async (s, e) => await SafeRefreshDevicesAsync();
            timer.Start();
        }

        public ObservableCollection<BluetoothDeviceInfo> BluetoothDevices { get; }

        public BluetoothDeviceInfo? SelectedBluetoothDevice
        {
            get => _selectedBluetoothDevice;
            set
            {
                _selectedBluetoothDevice = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            private set { _isConnected = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
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

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand OpenBluetoothSettingsCommand { get; }

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

                // Give Windows time to create the audio endpoint
                StatusText = "WAITING FOR AUDIO ENDPOINT...";
                // No need for arbitrary delay here anymore, StartRoutingAsync has a smarter retry loop
                // await Task.Delay(2500); 

                await _audioService.StartRoutingAsync(SelectedBluetoothDevice.Name, BufferMs);
                IsConnected = true;
                StatusText = "STREAMING ACTIVE";
            }
            catch (Exception ex)
            {
                StatusText = "ERROR: " + ex.Message;
                IsConnected = false;
                Debug.WriteLine($"[Connect] Error: {ex}");
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

    // ── Relay Commands ──────────────────────────────────────────────────

    /// <summary>Synchronous relay command.</summary>
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

    /// <summary>Async relay command that properly handles Task-returning delegates.</summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute == null || _canExecute());

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
