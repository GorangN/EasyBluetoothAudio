using EasyBluetoothAudio.Core;

namespace EasyBluetoothAudio.Tests;

/// <summary>
/// Tests for <see cref="ViewModelBase"/> property change notification infrastructure.
/// </summary>
public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        private string _value = string.Empty;

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }

    [Fact]
    public void SetProperty_RaisesPropertyChanged()
    {
        var vm = new TestViewModel();
        string? raisedProperty = null;
        vm.PropertyChanged += (s, e) => raisedProperty = e.PropertyName;

        vm.Value = "test";

        Assert.Equal("Value", raisedProperty);
    }

    [Fact]
    public void SetProperty_ReturnsFalse_WhenValueUnchanged()
    {
        var vm = new TestViewModel { Value = "test" };
        bool raised = false;
        vm.PropertyChanged += (s, e) => raised = true;

        vm.Value = "test";

        Assert.False(raised);
    }

    [Fact]
    public void SetProperty_ReturnsTrue_WhenValueChanged()
    {
        var vm = new TestViewModel { Value = "old" };

        vm.Value = "new";

        Assert.Equal("new", vm.Value);
    }
}
