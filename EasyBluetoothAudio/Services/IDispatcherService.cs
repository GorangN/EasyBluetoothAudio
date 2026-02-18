using System;

namespace EasyBluetoothAudio.Services;

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
}
