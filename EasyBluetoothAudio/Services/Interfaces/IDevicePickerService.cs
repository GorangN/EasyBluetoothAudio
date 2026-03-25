namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Abstracts the Windows DevicePicker UI for Bluetooth device pairing,
/// enabling testability by decoupling the WinRT picker from the ViewModel.
/// </summary>
public interface IDevicePickerService
{
    /// <summary>
    /// Opens the Windows DevicePicker in Bluetooth scan mode, anchored near the
    /// application window. Awaits until the user dismisses the picker.
    /// </summary>
    /// <returns>A task that completes when the picker is dismissed.</returns>
    Task ShowAsync();
}
