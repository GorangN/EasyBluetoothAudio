using System.Diagnostics;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.ViewModels;

internal sealed class DeviceConnectionMonitor(string deviceId, IAudioService audioService) : IDisposable
{
    internal const int PollDelayMs = 10_000;
    private volatile DeviceConnectionMonitorRegistration? _registration;
    private bool _isSubscribed;

    internal void Start(CancellationToken operationToken, Func<string, Task> recover)
    {
        Stop();
        var registration = new DeviceConnectionMonitorRegistration(operationToken, recover);
        _registration = registration;
        audioService.ConnectionLost += OnConnectionLost;
        _isSubscribed = true;
        _ = PollAsync(registration.CancellationToken, recover);
    }

    internal void Stop()
    {
        if (_isSubscribed)
        {
            audioService.ConnectionLost -= OnConnectionLost;
            _isSubscribed = false;
        }

        var registration = _registration;
        _registration = null;
        registration?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    private async Task PollAsync(CancellationToken token, Func<string, Task> recover)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(PollDelayMs, token);
                if (!await audioService.IsBluetoothDeviceConnectedAsync(deviceId)
                    && !token.IsCancellationRequested)
                {
                    await recover("monitor-detected-loss");
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[Monitor:{deviceId}] {exception.Message}");
        }
    }

    private async void OnConnectionLost(object? sender, DeviceConnectionLostEventArgs args)
    {
        var registration = _registration;
        if (registration == null
            || !string.Equals(args.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var token = registration.CancellationToken;
        try
        {
            if (!await audioService.IsBluetoothDeviceConnectedAsync(deviceId)
                && !token.IsCancellationRequested)
            {
                await registration.Recover("service-connection-lost");
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[ConnectionLost:{deviceId}] {exception.Message}");
        }
    }
}
