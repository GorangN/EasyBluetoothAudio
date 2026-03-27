using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Services.Interfaces;

/// <summary>
/// Defines operations for adjusting the Windows Bluetooth SBC codec bitpool
/// to reduce A2DP stream bandwidth on congested Bluetooth radios.
/// </summary>
public interface IBluetoothQualityService
{
    /// <summary>
    /// Gets a value indicating whether low-bandwidth mode is currently applied in the registry.
    /// </summary>
    bool IsLowBandwidthModeApplied { get; }

    /// <summary>
    /// Writes reduced SBC bitpool values to the Windows Bluetooth AVDTP registry key to lower stream bandwidth.
    /// Reads and returns the original values before overwriting so they can be restored later.
    /// Requires administrator privileges; returns <see cref="BluetoothQualityResult.AccessDenied"/> if not elevated.
    /// </summary>
    /// <param name="originalMaxBitpool">
    /// The original <c>MaximumBitpool</c> value found in the registry before writing,
    /// or <see langword="null"/> if the key did not previously exist.
    /// Only meaningful when the return value is <see cref="BluetoothQualityResult.Applied"/>.
    /// </param>
    /// <param name="originalDefaultBitpool">
    /// The original <c>DefaultBitpool</c> value found in the registry before writing,
    /// or <see langword="null"/> if the key did not previously exist.
    /// Only meaningful when the return value is <see cref="BluetoothQualityResult.Applied"/>.
    /// </param>
    /// <returns>A <see cref="BluetoothQualityResult"/> indicating the outcome.</returns>
    BluetoothQualityResult ApplyLowBandwidthMode(out int? originalMaxBitpool, out int? originalDefaultBitpool);

    /// <summary>
    /// Restores the SBC bitpool registry values to the originals captured before low-bandwidth mode was applied.
    /// If both original values are <see langword="null"/> (the key did not exist before), the key is deleted entirely.
    /// Requires administrator privileges; returns <see cref="BluetoothQualityResult.AccessDenied"/> if not elevated.
    /// </summary>
    /// <param name="originalMaxBitpool">The original <c>MaximumBitpool</c> to restore, or <see langword="null"/> to delete the key.</param>
    /// <param name="originalDefaultBitpool">The original <c>DefaultBitpool</c> to restore, or <see langword="null"/> to delete the key.</param>
    /// <returns>A <see cref="BluetoothQualityResult"/> indicating the outcome.</returns>
    BluetoothQualityResult RestoreDefaultMode(int? originalMaxBitpool, int? originalDefaultBitpool);
}
