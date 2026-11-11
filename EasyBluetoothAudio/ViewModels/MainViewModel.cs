using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>Coordinates source discovery, exclusive audio connections, and startup restoration.</summary>
/// <param name="audioService">Device-scoped audio operations.</param>
/// <param name="devicePickerService">Windows pairing picker.</param>
/// <param name="updateViewModel">Update presentation.</param>
/// <param name="settingsViewModel">Preferences presentation.</param>
/// <param name="settingsService">Persistent preferences.</param>
/// <param name="dispatcherService">UI scheduling.</param>
/// <param name="messenger">Application messages.</param>
/// <param name="sourceFactory">Creates an independently owned source.</param>
public partial class MainViewModel(IAudioService audioService, IDevicePickerService devicePickerService,
    UpdateViewModel updateViewModel, SettingsViewModel settingsViewModel, ISettingsService settingsService,
    IDispatcherService dispatcherService, IMessenger messenger,
    Func<BluetoothDevice, DeviceConnectionViewModel> sourceFactory) : ObservableObject, IDisposable
{
    private bool _disposed;
    private int _restoreGeneration;
    private readonly HashSet<string> _rememberedIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets stable source rows, including saved unavailable sources.</summary>
    public ObservableCollection<DeviceConnectionViewModel> Sources { get; } = [];
    /// <summary>Gets or sets the source presented by the compact controls without changing its route or startup preferences.</summary>
    [ObservableProperty]
    private DeviceConnectionViewModel? _selectedSource;
    /// <summary>Gets independent update presentation.</summary>
    public UpdateViewModel Updater { get; } = updateViewModel;
    /// <summary>Gets preferences presentation.</summary>
    public SettingsViewModel SettingsViewModel { get; } = settingsViewModel;
    /// <summary>Gets whether any sources exist.</summary>
    public bool HasSources
    {
        get
        {
            return Sources.Count > 0;
        }
    }

    /// <summary>Gets counts derived from actual route states.</summary>
    public string ConnectionSummary
    {
        get
        {
            return $"{Sources.Count(s => s.IsConnected)} connected" +
        (Sources.Any(s => s.IsReconnecting) ? $" · {Sources.Count(s => s.IsReconnecting)} reconnecting" : "");
        }
    }

    /// <summary>Gets or sets whether preferences are open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CloseActionLabel))]
    [NotifyPropertyChangedFor(nameof(SettingsActionLabel))]
    private bool _isSettingsOpen;
    /// <summary>Gets the header close action for the current page.</summary>
    public string CloseActionLabel
    {
        get { return IsSettingsOpen ? "Close settings" : "Hide to system tray"; }
    }
    /// <summary>Gets the reversible settings button action for assistive technology and tooltips.</summary>
    public string SettingsActionLabel
    {
        get { return IsSettingsOpen ? "Close settings" : "Settings"; }
    }
    /// <summary>Gets whether discovery is pending.</summary>
    [ObservableProperty] private bool _isRefreshing;
    /// <summary>Gets recoverable discovery or pairing feedback.</summary>
    [ObservableProperty] private string? _discoveryError;
    /// <summary>Gets whether a native interaction should keep the shell open.</summary>
    [ObservableProperty] private bool _isExternalInteractionActive;
    /// <summary>Gets update feedback independently from source state.</summary>
    [ObservableProperty] private string? _updateStatusText;
    /// <summary>Requests that the host show the panel.</summary>
    public event Action? RequestShow;
    /// <summary>Requests that the host hide the panel.</summary>
    public event Action? RequestHide;
    /// <summary>Requests process exit.</summary>
    public event Action? RequestExit;
    /// <summary>Requests activation after native pairing.</summary>
    public event Action? RequestActivate;

    /// <summary>Materializes saved sources and restores only the last remembered device.</summary>
    /// <returns>A task completing when restoration has been launched.</returns>
    public async Task InitializeAsync()
    {
        if (_disposed) { return; }
        var generation = _restoreGeneration;
        var settings = settingsService.Load();
        SettingsViewModel.Initialize();
        SettingsViewModel.RequestClose += OnSettingsClosed;
        Updater.StatusTextChanged += OnUpdateStatusChanged;
        messenger.Register<ReconnectRequestedMessage>(this, (_, _) => RecycleEligibleSources());
        var saved = settings.LastConnectedDevices ?? (string.IsNullOrWhiteSpace(settings.LastDeviceId)
            ? [] : new List<RememberedAudioDevice> { new() { Id = settings.LastDeviceId } });
        var savedDevices = saved.Where(d => !string.IsNullOrWhiteSpace(d.Id)).ToArray();
        foreach (var device in savedDevices.DistinctBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
        {
            AddSource(new BluetoothDevice { Id = device.Id, Name = device.Name });
        }
        if (savedDevices.LastOrDefault() is { } lastDevice)
        {
            _rememberedIds.Add(lastDevice.Id);
        }
        await RefreshDevicesAsync();
        if (_disposed || generation != _restoreGeneration) { return; }
        if (settings.AutoConnect)
        {
            foreach (var source in Sources.Where(s => _rememberedIds.Contains(s.Id)).ToArray())
            {
                if (_disposed || generation != _restoreGeneration) { break; }
                _ = source.ConnectAsync();
            }
        }
        _ = CheckUpdatesAsync(settings.AutoUpdateOnStartup);
    }

    /// <summary>Reconciles successful discovery while preserving rows and routes on scan failure.</summary>
    /// <returns>A task completing after discovery feedback is applied.</returns>
    [RelayCommand]
    public async Task RefreshDevicesAsync()
    {
        if (_disposed || IsRefreshing) { return; }
        IsRefreshing = true;
        try
        {
            var devices = (await audioService.GetBluetoothDevicesAsync()).ToArray();
            dispatcherService.Invoke(() =>
            {
                if (_disposed) { return; }
                var ids = devices.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var source in Sources.ToArray())
                {
                    source.IsAvailable = ids.Contains(source.Id);
                    if (!source.IsAvailable && !source.IsConnected && !source.IsBusy && !source.IsReconnecting && !_rememberedIds.Contains(source.Id))
                    {
                        RemoveSource(source);
                    }
                }
                var rememberedNameChanged = false;
                foreach (var device in devices.DistinctBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
                {
                    var source = Sources.FirstOrDefault(s => string.Equals(s.Id, device.Id, StringComparison.OrdinalIgnoreCase)) ?? AddSource(device);
                    rememberedNameChanged |= _rememberedIds.Contains(source.Id) && source.Name != device.Name;
                    source.Name = device.Name;
                    source.IsAvailable = true;
                }
                if (rememberedNameChanged) { SaveRestorationSet(); }
                SelectedSource ??= Sources.FirstOrDefault(source => source.IsAvailable) ?? Sources.FirstOrDefault();
                DiscoveryError = null;
                NotifySourcesChanged();
            });
        }
        catch (Exception)
        {
            if (!_disposed) { DiscoveryError = "Couldn’t refresh devices. Check Bluetooth and try again."; }
        }
        finally { IsRefreshing = false; }
    }

    private DeviceConnectionViewModel AddSource(BluetoothDevice device)
    {
        var source = sourceFactory(device);
        source.ConnectionEstablished += OnSourceConnected;
        source.UserDisconnected += OnSourceDisconnected;
        source.PropertyChanged += OnSourcePropertyChanged;
        Sources.Add(source);
        NotifySourcesChanged();
        return source;
    }
    private void OnSourceConnected(object? sender, EventArgs args)
    {
        if (!_disposed && sender is DeviceConnectionViewModel source)
        {
            _rememberedIds.Clear();
            _rememberedIds.Add(source.Id);
            SaveRestorationSet();
            NotifyConnection(source.Name, "connected");
        }
    }
    private void OnSourceDisconnected(object? sender, EventArgs args)
    {
        if (!_disposed && sender is DeviceConnectionViewModel source)
        {
            _rememberedIds.Remove(source.Id);
            SaveRestorationSet();
            NotifyConnection(source.Name, "disconnected");
        }
    }
    private void SaveRestorationSet()
    {
        var settings = settingsService.Load();
        settings.LastConnectedDevices = Sources.Where(s => _rememberedIds.Contains(s.Id))
            .Select(s => new RememberedAudioDevice { Id = s.Id, Name = s.Name }).ToList();
        try
        {
            settingsService.Save(settings);
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException)
        {
            DiscoveryError = "Connections are unchanged, but startup preferences couldn’t be saved. Check access to the settings folder.";
        }
    }
    private void NotifyConnection(string name, string state)
    {
        if (settingsService.Load().ShowNotifications)
        {
            messenger.Send(new ShowBalloonRequestedMessage(new BalloonContent("Easy Bluetooth Audio",
                $"{name} {state}.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info)));
        }
    }
    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!_disposed && args.PropertyName == nameof(DeviceConnectionViewModel.IsBusy)
            && sender is DeviceConnectionViewModel { IsBusy: true } source)
        {
            _restoreGeneration++;
            foreach (var other in Sources.Where(other => !ReferenceEquals(other, source) && other.ShowDisconnectAction).ToArray())
            {
                other.Disconnect();
            }
        }
        NotifySourcesChanged();
    }

    private void NotifySourcesChanged()
    {
        OnPropertyChanged(nameof(HasSources));
        OnPropertyChanged(nameof(ConnectionSummary));
        DisconnectAllCommand.NotifyCanExecuteChanged();
    }
    private bool CanDisconnectAll()
    {
        return Sources.Any(s => s.IsConnected || s.IsBusy || s.IsReconnecting) || _rememberedIds.Count > 0;
    }

    /// <summary>Cancels every operation before clearing startup intent and releasing owned routes.</summary>
    [RelayCommand(CanExecute = nameof(CanDisconnectAll))]
    public void DisconnectAll()
    {
        _restoreGeneration++;
        foreach (var source in Sources) { source.CancelOperations(); }
        foreach (var source in Sources)
        {
            source.UserDisconnected -= OnSourceDisconnected;
            source.Disconnect();
            source.UserDisconnected += OnSourceDisconnected;
        }
        _rememberedIds.Clear();
        SaveRestorationSet();
        audioService.DisconnectAll("user-all");
        NotifySourcesChanged();
    }
    private void RecycleEligibleSources()
    {
        if (_disposed) { return; }
        foreach (var source in Sources.Where(s => s.ShowReconnectActions && !s.IsBusy).ToArray())
        {
            if (source.ReconnectCommand.CanExecute(null)) { _ = source.ReconnectCommand.ExecuteAsync(null); }
        }
    }
    [RelayCommand]
    private async Task OpenBluetoothSettingsAsync()
    {
        IsExternalInteractionActive = true;
        try { await devicePickerService.ShowAsync(); await RefreshDevicesAsync(); }
        catch (Exception) { DiscoveryError = "Couldn’t open device pairing. Check Windows Bluetooth settings and try again."; }
        finally
        {
            IsExternalInteractionActive = false;
            if (!_disposed) { RequestActivate?.Invoke(); }
        }
    }
    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        if (IsSettingsOpen)
        {
            await SettingsViewModel.CloseCommand.ExecuteAsync(null);
        }
        else
        {
            IsSettingsOpen = true;
        }
    }

    [RelayCommand]
    private void Open()
    {
        RequestShow?.Invoke();
    }

    [RelayCommand]
    private void Hide()
    {
        RequestHide?.Invoke();
    }

    [RelayCommand]
    private void Exit()
    {
        RequestExit?.Invoke();
    }

    [RelayCommand]
    private async Task CloseOrHideAsync()
    {
        if (IsSettingsOpen) { await SettingsViewModel.CloseCommand.ExecuteAsync(null); }
        else { Hide(); }
    }
    private void OnSettingsClosed()
    {
        IsSettingsOpen = false;
    }

    private void OnUpdateStatusChanged(string status)
    {
        UpdateStatusText = status;
    }

    private async Task CheckUpdatesAsync(bool install)
    {
        await Updater.CheckForUpdateAsync();
        if (!_disposed && install && Updater.UpdateAvailable) { await Updater.InstallUpdateAsync(); }
    }
    private void RemoveSource(DeviceConnectionViewModel source)
    {
        source.ConnectionEstablished -= OnSourceConnected;
        source.UserDisconnected -= OnSourceDisconnected;
        source.PropertyChanged -= OnSourcePropertyChanged;
        source.Dispose();
        Sources.Remove(source);
        if (ReferenceEquals(SelectedSource, source))
        {
            SelectedSource = _disposed ? null : Sources.FirstOrDefault();
        }
    }
    /// <summary>Cancels all controllers without modifying startup restoration preferences.</summary>
    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        _restoreGeneration++;
        messenger.UnregisterAll(this);
        SettingsViewModel.RequestClose -= OnSettingsClosed;
        Updater.StatusTextChanged -= OnUpdateStatusChanged;
        foreach (var source in Sources.ToArray()) { RemoveSource(source); }
    }
}
