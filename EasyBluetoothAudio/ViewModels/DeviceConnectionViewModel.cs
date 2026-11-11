using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.ViewModels;

/// <summary>
/// Presents one Bluetooth audio source with independent connection commands and status.
/// </summary>
public sealed partial class DeviceConnectionViewModel : ObservableObject, IDisposable
{
    private readonly DeviceConnectionController _controller;
    private readonly IMessenger _messenger;
    private string _name;
    private bool _isAvailable;

    /// <summary>
    /// Creates a source row whose connection lifecycle is isolated from other sources.
    /// </summary>
    /// <param name="device">The discovered or remembered device identity.</param>
    /// <param name="audioService">The device-scoped audio connection service.</param>
    /// <param name="dispatcherService">The dispatcher for observable state changes.</param>
    /// <param name="messenger">The messenger used for connection sound notifications.</param>
    public DeviceConnectionViewModel(BluetoothDevice device, IAudioService audioService,
        IDispatcherService dispatcherService, IMessenger messenger)
        : this(device, audioService, dispatcherService, messenger, null)
    {
    }

    internal DeviceConnectionViewModel(BluetoothDevice device, IAudioService audioService,
        IDispatcherService dispatcherService, IMessenger messenger,
        Func<int, CancellationToken, Task>? retryDelay, TimeProvider? timeProvider = null)
    {
        Id = device.Id;
        _name = device.Name;
        _isAvailable = device.IsConnected;
        _messenger = messenger;
        _controller = new DeviceConnectionController(Id, audioService, dispatcherService, retryDelay, timeProvider);
        _controller.Changed += OnControllerChanged;
        _controller.ConnectionEstablished += OnConnectionEstablished;
    }

    /// <summary>
    /// Raised after this source has an opened, stabilized route, including successful recovery.
    /// </summary>
    public event EventHandler? ConnectionEstablished;

    /// <summary>
    /// Raised when the user clears this source's connection and startup restoration intent.
    /// </summary>
    public event EventHandler? UserDisconnected;

    /// <summary>Gets the stable Windows device identifier used for source ownership.</summary>
    public string Id { get; }

    /// <summary>Gets or sets the latest friendly name reported by Windows.</summary>
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>Gets or sets whether Windows currently discovers this audio source.</summary>
    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            if (SetProperty(ref _isAvailable, value))
            {
                OnPropertyChanged(nameof(StatusText));
                ConnectCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets readable route state without implying that audio is currently playing.</summary>
    public string StatusText
    {
        get
        {
            return _controller.State switch
            {
                DeviceConnectionState.Connecting => "Connecting…",
                DeviceConnectionState.Connected => "Connected",
                DeviceConnectionState.Reconnecting => "Reconnecting…",
                DeviceConnectionState.RecoverableLoss => "Reconnect required",
                DeviceConnectionState.Failed => "Connection failed",
                _ => IsAvailable ? "Ready to connect" : "Device unavailable"
            };
        }
    }

    /// <summary>Gets actionable failure information for this source, or an empty string.</summary>
    public string ErrorText
    {
        get { return _controller.ErrorText; }
    }

    /// <summary>Gets whether this app has an opened audio route for this source.</summary>
    public bool IsConnected
    {
        get { return _controller.State == DeviceConnectionState.Connected; }
    }

    /// <summary>Gets whether this source has a connection or recovery operation in progress.</summary>
    public bool IsBusy
    {
        get { return _controller.State is DeviceConnectionState.Connecting or DeviceConnectionState.Reconnecting; }
    }

    /// <summary>Gets whether this source is restoring an audio route.</summary>
    public bool IsReconnecting
    {
        get { return _controller.State == DeviceConnectionState.Reconnecting; }
    }

    /// <summary>Gets whether the full Connect action belongs in this row.</summary>
    public bool ShowConnectAction
    {
        get { return !ShowDisconnectAction; }
    }

    /// <summary>Gets whether a route can be recycled using the Reconnect action.</summary>
    public bool ShowReconnectActions
    {
        get { return _controller.State is DeviceConnectionState.Connected or DeviceConnectionState.RecoverableLoss; }
    }

    /// <summary>Gets whether the user's active connection intent can be cancelled.</summary>
    public bool ShowDisconnectAction
    {
        get { return IsBusy || ShowReconnectActions; }
    }

    /// <summary>
    /// Starts connecting with sticky retries, priming only when no other source has a managed route.
    /// Startup restoration may call this directly before the device becomes available.
    /// </summary>
    /// <returns>A task completing after connection, cancellation, or a terminal failure.</returns>
    [RelayCommand(CanExecute = nameof(CanConnect))]
    public Task ConnectAsync()
    {
        return _controller.ConnectAsync();
    }

    /// <summary>Attempts one manual route recycle, preserving physical-disconnect timing.</summary>
    /// <returns>A task completing after connection, cancellation, or manual fallback.</returns>
    [RelayCommand(CanExecute = nameof(CanReconnect))]
    public Task ReconnectAsync()
    {
        return _controller.ReconnectAsync();
    }

    /// <summary>Cancels this source's retry and monitor scopes before aggregate teardown.</summary>
    public void CancelOperations()
    {
        _controller.CancelOperations();
    }

    /// <summary>Disconnects only this source and clears its startup restoration intent.</summary>
    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    public void Disconnect()
    {
        _controller.Disconnect();
        UserDisconnected?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Releases this row's connection and handlers without clearing saved startup intent.</summary>
    public void Dispose()
    {
        _controller.Changed -= OnControllerChanged;
        _controller.ConnectionEstablished -= OnConnectionEstablished;
        _controller.Dispose();
    }

    private bool CanConnect()
    {
        return !_controller.IsDisposed && IsAvailable && ShowConnectAction;
    }

    private bool CanReconnect()
    {
        return !_controller.IsDisposed && ShowReconnectActions;
    }

    private bool CanDisconnect()
    {
        return !_controller.IsDisposed && ShowDisconnectAction;
    }

    private void OnConnectionEstablished(object? sender, EventArgs args)
    {
        _messenger.Send(new ConnectionEstablishedMessage(Name));
        ConnectionEstablished?.Invoke(this, EventArgs.Empty);
    }

    private void OnControllerChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsReconnecting));
        OnPropertyChanged(nameof(ShowConnectAction));
        OnPropertyChanged(nameof(ShowReconnectActions));
        OnPropertyChanged(nameof(ShowDisconnectAction));
        ConnectCommand.NotifyCanExecuteChanged();
        ReconnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();
    }
}
