using System.Threading.Tasks;
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
    /// If the current process lacks administrator privileges, a UAC prompt is shown automatically by relaunching
    /// as an elevated helper that applies the change and exits.
    /// Changes take effect on the next A2DP connection negotiation (disconnect and reconnect required).
    /// </summary>
    /// <returns>A <see cref="BluetoothQualityResult"/> indicating the outcome.</returns>
    Task<BluetoothQualityResult> ApplyLowBandwidthModeAsync();

    /// <summary>
    /// Deletes the AVDTP SBC registry key to restore Windows Bluetooth audio defaults.
    /// If the current process lacks administrator privileges, a UAC prompt is shown automatically.
    /// Changes take effect on the next A2DP connection negotiation (disconnect and reconnect required).
    /// </summary>
    /// <returns>A <see cref="BluetoothQualityResult"/> indicating the outcome.</returns>
    Task<BluetoothQualityResult> RestoreDefaultModeAsync();
}
