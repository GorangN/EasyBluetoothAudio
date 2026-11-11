using System.Diagnostics;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.ViewModels;

internal sealed class DeviceConnectionController : IDisposable
{
    internal const int RetryDelayMs = 3_000;
    internal const int AutomaticRecoveryAttemptLimit = 3;
    private readonly string _deviceId;
    private readonly IAudioService _audioService;
    private readonly IDispatcherService _dispatcher;
    private readonly DeviceConnectionMonitor _monitor;
    private readonly Func<int, CancellationToken, Task> _retryDelay;
    private readonly TimeProvider _timeProvider;
    private DeviceConnectionOperation? _operation;
    private volatile bool _isDisposed;

    internal DeviceConnectionController(string deviceId, IAudioService audioService,
        IDispatcherService dispatcher, Func<int, CancellationToken, Task>? retryDelay = null,
        TimeProvider? timeProvider = null)
    {
        _deviceId = deviceId;
        _audioService = audioService;
        _dispatcher = dispatcher;
        _monitor = new DeviceConnectionMonitor(deviceId, audioService);
        _retryDelay = retryDelay ?? ((milliseconds, token) => Task.Delay(milliseconds, token));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    internal event EventHandler? Changed;
    internal event EventHandler? ConnectionEstablished;
    internal DeviceConnectionState State { get; private set; }
    internal string ErrorText { get; private set; } = string.Empty;
    internal bool IsDisposed
    {
        get { return _isDisposed; }
    }

    internal async Task ConnectAsync()
    {
        if (_isDisposed || State is DeviceConnectionState.Connecting or DeviceConnectionState.Reconnecting
            or DeviceConnectionState.Connected or DeviceConnectionState.RecoverableLoss)
        {
            return;
        }

        var operation = BeginOperation();
        if (!Apply(operation, () => SetState(DeviceConnectionState.Connecting)))
        {
            return;
        }

        var primeStartup = !_audioService.HasOtherAudioConnections(_deviceId);
        await RunStickyConnectAsync(operation, primeStartup);
    }

    internal async Task ReconnectAsync()
    {
        if (_isDisposed || State is not (DeviceConnectionState.Connected or DeviceConnectionState.RecoverableLoss))
        {
            return;
        }

        var operation = BeginOperation();
        try
        {
            if (!Apply(operation, () =>
                {
                    SetState(DeviceConnectionState.Reconnecting);
                    _audioService.Disconnect(_deviceId, "manual-recover", preserveDisconnectTimestamp: true);
                }))
            {
                return;
            }

            var result = await _audioService.ConnectBluetoothAudioAsync(_deviceId, operation.CancellationToken);
            if (!IsCurrent(operation))
            {
                return;
            }

            if (result == AudioConnectionResult.Connected)
            {
                ApplyConnected(operation);
            }
            else if (result == AudioConnectionResult.DeniedBySystem)
            {
                Apply(operation, () => SetState(DeviceConnectionState.Failed, GetErrorText(result)));
            }
            else
            {
                var isPresent = await _audioService.IsBluetoothPhysicallyConnectedAsync(_deviceId);
                Apply(operation, () => SetState(
                    isPresent ? DeviceConnectionState.RecoverableLoss : DeviceConnectionState.Failed,
                    isPresent ? "Audio connection failed. Choose Reconnect to try again."
                        : "Device unavailable. Connect it in Bluetooth settings, then choose Connect."));
            }
        }
        catch (OperationCanceledException) when (!IsCurrent(operation))
        {
        }
        catch (Exception exception)
        {
            ApplyFailure(operation, exception);
        }
    }

    internal void CancelOperations()
    {
        var operation = _operation;
        _operation = null;
        _monitor.Stop();
        operation?.Dispose();
    }

    internal void Disconnect()
    {
        CancelOperations();
        try
        {
            _audioService.Disconnect(_deviceId, "user");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[Disconnect:{_deviceId}] {exception.Message}");
        }
        finally
        {
            _dispatcher.Invoke(() => SetState(DeviceConnectionState.Disconnected));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        CancelOperations();
        _monitor.Dispose();
        try
        {
            _audioService.Disconnect(_deviceId, "dispose");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[Dispose:{_deviceId}] {exception.Message}");
        }
    }

    private DeviceConnectionOperation BeginOperation()
    {
        CancelOperations();
        var operation = new DeviceConnectionOperation(_timeProvider);
        _operation = operation;
        return operation;
    }

    private async Task RunStickyConnectAsync(DeviceConnectionOperation operation, bool primeStartup,
        int maximumAttempts = int.MaxValue)
    {
        var shouldPrime = primeStartup;
        var attemptCount = 0;
        try
        {
            while (IsCurrent(operation))
            {
                attemptCount++;
                var result = await _audioService.ConnectBluetoothAudioAsync(_deviceId, operation.CancellationToken);
                if (!IsCurrent(operation))
                {
                    return;
                }

                if (result == AudioConnectionResult.Connected && shouldPrime)
                {
                    shouldPrime = false;
                    var didPrime = false;
                    if (!Apply(operation, () =>
                        {
                            if (!_audioService.HasOtherAudioConnections(_deviceId))
                            {
                                _audioService.Disconnect(_deviceId, "startup-prime", preserveDisconnectTimestamp: true);
                                didPrime = true;
                            }
                        }))
                    {
                        return;
                    }

                    if (didPrime)
                    {
                        result = await _audioService.ConnectBluetoothAudioAsync(_deviceId, operation.CancellationToken);
                    }
                }

                if (!IsCurrent(operation))
                {
                    return;
                }

                if (result == AudioConnectionResult.Connected)
                {
                    ApplyConnected(operation);
                    return;
                }

                if (result == AudioConnectionResult.DeniedBySystem)
                {
                    Apply(operation, () =>
                    {
                        _monitor.Stop();
                        SetState(DeviceConnectionState.Failed, GetErrorText(result));
                    });
                    return;
                }

                Apply(operation, () => SetState(DeviceConnectionState.Reconnecting, GetErrorText(result)));
                if (attemptCount >= maximumAttempts)
                {
                    await ApplyAutomaticRecoveryFallbackAsync(operation, "Automatic reconnection failed.");
                    return;
                }

                await _retryDelay(RetryDelayMs, operation.CancellationToken);
            }
        }
        catch (OperationCanceledException) when (!IsCurrent(operation))
        {
        }
        catch (Exception exception)
        {
            ApplyFailure(operation, exception);
        }
    }

    private async Task RecoverAsync(DeviceConnectionOperation operation, string reason)
    {
        if (Interlocked.CompareExchange(ref operation.RecoveryInFlight, 1, 0) != 0)
        {
            return;
        }

        try
        {
            var recoveryAllowed = false;
            if (Apply(operation, () =>
                {
                    recoveryAllowed = operation.TryBeginAutomaticRecovery();
                    SetState(DeviceConnectionState.Reconnecting);
                    if (!recoveryAllowed)
                    {
                        _monitor.Stop();
                    }

                    _audioService.Disconnect(_deviceId, recoveryAllowed ? reason : "automatic-recovery-paused");
                }))
            {
                if (recoveryAllowed)
                {
                    await RunStickyConnectAsync(operation, primeStartup: false, AutomaticRecoveryAttemptLimit);
                }
                else
                {
                    await ApplyAutomaticRecoveryFallbackAsync(operation, "Audio connection was interrupted repeatedly.");
                }
            }
        }
        catch (OperationCanceledException) when (!IsCurrent(operation))
        {
        }
        catch (Exception exception)
        {
            ApplyFailure(operation, exception);
        }
        finally
        {
            Interlocked.Exchange(ref operation.RecoveryInFlight, 0);
        }
    }

    private async Task ApplyAutomaticRecoveryFallbackAsync(DeviceConnectionOperation operation, string message)
    {
        var isPresent = await _audioService.IsBluetoothPhysicallyConnectedAsync(_deviceId);
        Apply(operation, () =>
        {
            _monitor.Stop();
            SetState(isPresent ? DeviceConnectionState.RecoverableLoss : DeviceConnectionState.Failed,
                message + (isPresent ? " Choose Reconnect to try again."
                    : " Connect it in Bluetooth settings, then choose Connect."));
        });
    }

    private bool IsCurrent(DeviceConnectionOperation operation)
    {
        return !_isDisposed && ReferenceEquals(_operation, operation)
            && !operation.CancellationToken.IsCancellationRequested;
    }

    private bool Apply(DeviceConnectionOperation operation, Action action)
    {
        var applied = false;
        _dispatcher.Invoke(() =>
        {
            if (IsCurrent(operation))
            {
                action();
                applied = true;
            }
        });
        return applied;
    }

    private void ApplyConnected(DeviceConnectionOperation operation)
    {
        Apply(operation, () =>
        {
            SetState(DeviceConnectionState.Connected);
            _monitor.Start(operation.CancellationToken, reason => RecoverAsync(operation, reason));
            ConnectionEstablished?.Invoke(this, EventArgs.Empty);
        });
    }

    private void ApplyFailure(DeviceConnectionOperation operation, Exception exception)
    {
        Debug.WriteLine($"[Connection:{_deviceId}] {exception.Message}");
        Apply(operation, () =>
        {
            _monitor.Stop();
            SetState(DeviceConnectionState.Failed, "Couldn't connect. Choose Connect to try again.");
        });
    }

    private void SetState(DeviceConnectionState state, string errorText = "")
    {
        State = state;
        ErrorText = errorText;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static string GetErrorText(AudioConnectionResult result)
    {
        return result switch
        {
            AudioConnectionResult.Unavailable => "Device unavailable. Keep it nearby with Bluetooth enabled.",
            AudioConnectionResult.TimedOut => "The connection timed out. Trying again…",
            AudioConnectionResult.DeniedBySystem => "Windows couldn't open this audio source. Check Bluetooth settings, then choose Connect.",
            _ => "Couldn't open the audio connection. Trying again…"
        };
    }
}
