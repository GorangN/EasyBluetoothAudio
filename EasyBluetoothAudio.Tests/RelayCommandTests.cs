using EasyBluetoothAudio.Core;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Tests for <see cref="RelayCommand"/> execution and CanExecute behavior.
/// </summary>
public class RelayCommandTests
{
    [Fact]
    public void Execute_InvokesDelegate()
    {
        bool invoked = false;
        var command = new RelayCommand(_ => invoked = true);

        command.Execute(null);

        Assert.True(invoked);
    }

    [Fact]
    public void Execute_PassesParameter()
    {
        object? received = null;
        var command = new RelayCommand(p => received = p);

        command.Execute("hello");

        Assert.Equal("hello", received);
    }

    [Fact]
    public void CanExecute_DefaultsToTrue()
    {
        var command = new RelayCommand(_ => { });

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_UsesProvidedPredicate_WhenTrue()
    {
        var command = new RelayCommand(_ => { }, _ => true);

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_UsesProvidedPredicate_WhenFalse()
    {
        var command = new RelayCommand(_ => { }, _ => false);

        Assert.False(command.CanExecute(null));
    }

    [Fact]
    public void CanExecute_ReceivesParameter()
    {
        object? received = null;
        var command = new RelayCommand(_ => { }, p => { received = p; return true; });

        command.CanExecute("test");

        Assert.Equal("test", received);
    }
}
