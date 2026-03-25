using System;
using System.Threading.Tasks;

namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Defines a service for dispatching actions to the UI thread.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Executes the specified action synchronously on the UI thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    void Invoke(Action action);

    /// <summary>
    /// Executes the specified asynchronous action on the UI thread and awaits its completion.
    /// </summary>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <returns>A task that completes when the action has finished.</returns>
    Task InvokeAsync(Func<Task> action);
}
