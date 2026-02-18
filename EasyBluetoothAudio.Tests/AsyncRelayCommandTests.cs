using EasyBluetoothAudio.Core;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Tests for <see cref="AsyncRelayCommand"/> async execution and re-entrancy guard.
/// </summary>
public class AsyncRelayCommandTests
{
    [Fact]
    public void CanExecute_DefaultsToTrue()
    {
        var command = new AsyncRelayCommand(() => Task.CompletedTask);

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_UsesProvidedFunc_WhenFalse()
    {
        var command = new AsyncRelayCommand(() => Task.CompletedTask, () => false);

        Assert.False(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_UsesProvidedFunc_WhenTrue()
    {
        var command = new AsyncRelayCommand(() => Task.CompletedTask, () => true);

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task Execute_InvokesAsyncDelegate()
    {
        bool invoked = false;
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(async () =>
        {
            invoked = true;
            tcs.SetResult();
            await Task.CompletedTask;
        });

        command.Execute(null);
        await tcs.Task;

        Assert.True(invoked);
    }

    [Fact]
    public async Task CanExecute_FalseWhileExecuting()
    {
        var tcs = new TaskCompletionSource();
        var command = new AsyncRelayCommand(() => tcs.Task);

        command.Execute(null);

        Assert.False(command.CanExecute(null));

        tcs.SetResult();
        await Task.Delay(50);

        Assert.True(command.CanExecute(null));
    }
}
