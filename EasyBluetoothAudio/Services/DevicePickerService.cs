using System.Windows;
using System.Windows.Interop;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using EasyBluetoothAudio.Services.Interfaces;
using EasyBluetoothAudio.Views;
using WinRT.Interop;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Implements <see cref="IDevicePickerService"/> using the WinRT
/// <see cref="Windows.Devices.Enumeration.DevicePicker"/> to show a native Bluetooth
/// device pairing UI anchored to the main application window.
/// </summary>
public sealed class DevicePickerService : IDevicePickerService
{
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicePickerService"/> class.
    /// </summary>
    /// <param name="mainWindow">
    /// The application's main window, used to obtain the HWND and screen position
    /// required by the WinRT DevicePicker interop API.
    /// </param>
    public DevicePickerService(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <inheritdoc />
    public async Task ShowAsync()
    {
        var picker = new DevicePicker();

        // Show all Bluetooth devices: already-paired and nearby undiscovered devices.
        picker.Filter.SupportedDeviceSelectors.Add(BluetoothDevice.GetDeviceSelector());
        picker.Filter.SupportedDeviceSelectors.Add(
            BluetoothDevice.GetDeviceSelectorFromPairingState(false));

        // WinRT pickers require an HWND owner for the picker window (IInitializeWithWindow).
        var hwnd = new WindowInteropHelper(_mainWindow).Handle;
        InitializeWithWindow.Initialize(picker, hwnd);

        // The return value is intentionally discarded: pairing is handled by Windows,
        // and the ViewModel calls RefreshDevicesAsync() afterwards to update the list.
        _ = await picker.PickSingleDeviceAsync(GetWindowRect());
    }

    /// <summary>
    /// Returns the screen bounds of the main window in physical pixels for DevicePicker placement.
    /// </summary>
    /// <returns>The DPI-scaled screen bounds of the main window.</returns>
    private Windows.Foundation.Rect GetWindowRect()
    {
        var source = PresentationSource.FromVisual(_mainWindow);
        double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        return new Windows.Foundation.Rect(
            _mainWindow.Left * dpiX,
            _mainWindow.Top * dpiY,
            _mainWindow.Width * dpiX,
            _mainWindow.Height * dpiY);
    }
}
