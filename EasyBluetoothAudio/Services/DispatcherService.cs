using System;
using System.Threading.Tasks;
using EasyBluetoothAudio.Services.Interfaces;

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

    /// <inheritdoc />
    public Task InvokeAsync(Func<Task> action)
    {
        return System.Windows.Application.Current.Dispatcher.InvokeAsync(action).Task.Unwrap();
    }
}
