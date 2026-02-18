using System;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Implements the dispatcher service using the current application dispatcher.
/// </summary>
public class DispatcherService : IDispatcherService
{
    /// <inheritdoc />
    public void Invoke(Action action)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(action);
    }
}
