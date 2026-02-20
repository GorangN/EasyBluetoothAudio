using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using EasyBluetoothAudio.ViewModels;
using EasyBluetoothAudio.Services;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Tests;

public class UpdateViewModelTests
{
    [Fact]
    public async Task CheckForUpdateAsync_SetsStatusText_WhenRateLimitExceeded()
    {
        // Arrange
        var mockUpdateService = new Mock<IUpdateService>();
        mockUpdateService
            .Setup(s => s.CheckForUpdateAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ThrowsAsync(new Exception("Rate limit exceeded"));

        var vm = new UpdateViewModel(mockUpdateService.Object);
        
        string? updatedStatus = null;
        vm.StatusTextChanged += status => updatedStatus = status;

        // Act
        await vm.CheckForUpdateAsync();

        // Assert
        Assert.Equal("RATE LIMIT EXCEEDED", updatedStatus);
    }
}
