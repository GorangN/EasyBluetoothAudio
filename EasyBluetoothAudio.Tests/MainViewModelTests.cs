using Moq;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace EasyBluetoothAudio.Tests;

public class MainViewModelTests
{
    [Fact]
    public async Task SafeRefreshDevicesAsync_ShouldOnlyAddPhonesAndComputers()
    {
        // Arrange
        var mockAudioService = new Mock<IAudioService>();
        
        var mockDevices = new List<BluetoothDevice>
        {
            new BluetoothDevice { Name = "My iPhone", Id = "1", IsPhoneOrComputer = true },
            new BluetoothDevice { Name = "My Laptop", Id = "2", IsPhoneOrComputer = true },
            new BluetoothDevice { Name = "AirPods Pro", Id = "3", IsPhoneOrComputer = false },
            new BluetoothDevice { Name = "Sony Speaker", Id = "4", IsPhoneOrComputer = false }
        };

        mockAudioService.Setup(s => s.GetBluetoothDevicesAsync())
                        .ReturnsAsync(mockDevices);

        // Act
        // The constructor calls SafeRefreshDevicesAsync, but we use reflection to call it explicitly 
        // or just rely on the constructor's initial call. 
        // Note: MainViewModel constructor starts a DispatcherTimer which might fail in a non-WPF environment,
        // but let's see if we can get away with it or if we need to mock more.
        var vm = new MainViewModel(mockAudioService.Object);
        
        // Wait a bit for the async call in constructor to finish or call it manually
        // Since SafeRefreshDevicesAsync is private, we'll try to trigger it via constructor and wait
        await Task.Delay(100); 

        // Assert
        Assert.Equal(2, vm.BluetoothDevices.Count);
        Assert.Contains(vm.BluetoothDevices, d => d.Name == "My iPhone");
        Assert.Contains(vm.BluetoothDevices, d => d.Name == "My Laptop");
        Assert.DoesNotContain(vm.BluetoothDevices, d => d.Name == "AirPods Pro");
        Assert.DoesNotContain(vm.BluetoothDevices, d => d.Name == "Sony Speaker");
    }
}
