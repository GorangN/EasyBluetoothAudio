using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;

namespace EasyBluetoothAudio.Tests;

/// <summary>Simulates a native route whose open operation can complete after disposal.</summary>
internal sealed class FakeAudioPlaybackConnection : IAudioPlaybackConnection
{
    internal TaskCompletionSource<AudioConnectionResult>? PendingOpen { get; init; }
    internal TaskCompletionSource<bool>? PendingStart { get; init; }
    internal TaskCompletionSource<bool> StartStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource<bool> OpenStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal AudioConnectionResult Result { get; init; } = AudioConnectionResult.Connected;
    internal int DisposeCount { get; private set; }
    internal int StartCount { get; private set; }

    /// <inheritdoc />
    public bool IsOpen { get; private set; }

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public async Task StartAsync()
    {
        StartCount++;
        StartStarted.TrySetResult(true);
        if (PendingStart != null)
        {
            await PendingStart.Task;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public async Task<AudioConnectionResult> OpenAsync()
    {
        OpenStarted.TrySetResult(true);
        var result = PendingOpen == null ? Result : await PendingOpen.Task;
        IsOpen = result == AudioConnectionResult.Connected && DisposeCount == 0;
        return result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeCount++;
        IsOpen = false;
    }

    internal void LoseConnection()
    {
        IsOpen = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void RaiseStateChangedFrom(object sender)
    {
        IsOpen = false;
        StateChanged?.Invoke(sender, EventArgs.Empty);
    }

    internal void OpenConnectionBeforeRequestCompletes()
    {
        IsOpen = true;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    internal Action CaptureStateNotification()
    {
        var handler = StateChanged;
        return () => handler?.Invoke(this, EventArgs.Empty);
    }
}
