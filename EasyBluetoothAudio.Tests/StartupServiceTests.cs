using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EasyBluetoothAudio.Services;
using Microsoft.Win32;
using Xunit;

namespace EasyBluetoothAudio.Tests;

public class StartupServiceTests : IDisposable
{
    private const string TestRegistryKeyPath = @"Software\EasyBluetoothAudio\TestStartup";
    private const string TestAppName = "TestEasyBluetoothAudio";

    private readonly StartupService? _sut;

    public StartupServiceTests()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Ensure a clean state by creating the test registry key.
            // This guarantees that OpenSubKey won't fail due to missing path.
            using var key = Registry.CurrentUser.CreateSubKey(TestRegistryKeyPath);

            _sut = new StartupService(TestRegistryKeyPath, TestAppName);
        }
    }

    public void Dispose()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Clean up test registry key after tests
            Registry.CurrentUser.DeleteSubKeyTree(TestRegistryKeyPath, throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenNotSet()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        Assert.NotNull(_sut);

        // Ensure no leftover value exists before test.
        using (var key = Registry.CurrentUser.OpenSubKey(TestRegistryKeyPath, writable: true))
        {
            key?.DeleteValue(TestAppName, throwOnMissingValue: false);
        }

        var isEnabled = _sut.IsEnabled;

        Assert.False(isEnabled);
    }

    [Fact]
    public void Enable_SetsRegistryKey_And_IsEnabled_ReturnsTrue()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        Assert.NotNull(_sut);

        _sut.Enable();

        var isEnabled = _sut.IsEnabled;
        Assert.True(isEnabled);

        // Verify the expected value in registry
        using var key = Registry.CurrentUser.OpenSubKey(TestRegistryKeyPath, writable: false);
        var value = key?.GetValue(TestAppName) as string;

        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        Assert.Equal($"\"{exePath}\" --silent", value);
    }

    [Fact]
    public void Disable_RemovesRegistryKey_And_IsEnabled_ReturnsFalse()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        Assert.NotNull(_sut);

        // First, enable it
        _sut.Enable();
        Assert.True(_sut.IsEnabled);

        // Then, disable it
        _sut.Disable();

        var isEnabled = _sut.IsEnabled;
        Assert.False(isEnabled);

        // Verify value is actually removed from registry
        using var key = Registry.CurrentUser.OpenSubKey(TestRegistryKeyPath, writable: false);
        var value = key?.GetValue(TestAppName);
        Assert.Null(value);
    }
}
