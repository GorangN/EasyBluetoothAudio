using System;
using System.Diagnostics;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Microsoft.Win32;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Adjusts the Windows Bluetooth SBC codec bitpool via the AVDTP registry path to reduce
/// A2DP stream bandwidth on congested radios.
/// Changes take effect on the next A2DP connection negotiation (disconnect and reconnect required).
/// Requires administrator privileges for HKLM writes.
/// </summary>
public class BluetoothQualityService : IBluetoothQualityService
{
    private const string AvdtpSbcKeyPath = @"SYSTEM\CurrentControlSet\Control\Bluetooth\Audio\AVDTP\Sbc";
    private const int LowBandwidthMaxBitpool = 15;
    private const int LowBandwidthDefaultBitpool = 15;

    /// <inheritdoc />
    public bool IsLowBandwidthModeApplied
    {
        get
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(AvdtpSbcKeyPath, writable: false);
                return key?.GetValue("MaximumBitpool") is int max && max == LowBandwidthMaxBitpool;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BluetoothQuality] IsLowBandwidthModeApplied check failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public BluetoothQualityResult ApplyLowBandwidthMode(out int? originalMaxBitpool, out int? originalDefaultBitpool)
    {
        originalMaxBitpool = null;
        originalDefaultBitpool = null;

        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(AvdtpSbcKeyPath, writable: true);
            if (key == null)
            {
                return BluetoothQualityResult.NotSupported;
            }

            // Back up existing values before overwriting so they can be restored later.
            if (key.GetValue("MaximumBitpool") is int existingMax)
            {
                originalMaxBitpool = existingMax;
            }

            if (key.GetValue("DefaultBitpool") is int existingDefault)
            {
                originalDefaultBitpool = existingDefault;
            }

            key.SetValue("MaximumBitpool", LowBandwidthMaxBitpool, RegistryValueKind.DWord);
            key.SetValue("DefaultBitpool", LowBandwidthDefaultBitpool, RegistryValueKind.DWord);

            Debug.WriteLine($"[BluetoothQuality] Applied low-bandwidth mode (MaximumBitpool={LowBandwidthMaxBitpool}, DefaultBitpool={LowBandwidthDefaultBitpool}).");
            return BluetoothQualityResult.Applied;
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"[BluetoothQuality] Access denied writing AVDTP registry key: {ex.Message}");
            return BluetoothQualityResult.AccessDenied;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BluetoothQuality] Unexpected error applying low-bandwidth mode: {ex.Message}");
            return BluetoothQualityResult.NotSupported;
        }
    }

    /// <inheritdoc />
    public BluetoothQualityResult RestoreDefaultMode(int? originalMaxBitpool, int? originalDefaultBitpool)
    {
        try
        {
            if (originalMaxBitpool == null && originalDefaultBitpool == null)
            {
                // The key did not exist before low-bandwidth mode was applied — delete it entirely.
                Registry.LocalMachine.DeleteSubKey(AvdtpSbcKeyPath, throwOnMissingSubKey: false);
                Debug.WriteLine("[BluetoothQuality] Deleted AVDTP\\Sbc subkey (key did not exist before).");
                return BluetoothQualityResult.Restored;
            }

            using var key = Registry.LocalMachine.OpenSubKey(AvdtpSbcKeyPath, writable: true);
            if (key == null)
            {
                // Key is already gone — nothing to restore.
                return BluetoothQualityResult.Restored;
            }

            if (originalMaxBitpool.HasValue)
            {
                key.SetValue("MaximumBitpool", originalMaxBitpool.Value, RegistryValueKind.DWord);
            }

            if (originalDefaultBitpool.HasValue)
            {
                key.SetValue("DefaultBitpool", originalDefaultBitpool.Value, RegistryValueKind.DWord);
            }

            Debug.WriteLine("[BluetoothQuality] Restored original AVDTP bitpool values.");
            return BluetoothQualityResult.Restored;
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"[BluetoothQuality] Access denied restoring AVDTP registry key: {ex.Message}");
            return BluetoothQualityResult.AccessDenied;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BluetoothQuality] Unexpected error restoring default mode: {ex.Message}");
            return BluetoothQualityResult.NotSupported;
        }
    }
}
