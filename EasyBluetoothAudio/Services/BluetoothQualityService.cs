using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EasyBluetoothAudio.Models;
using EasyBluetoothAudio.Services.Interfaces;
using Microsoft.Win32;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Adjusts the Windows Bluetooth SBC codec bitpool via the AVDTP registry path to reduce
/// A2DP stream bandwidth on congested radios.
/// When the calling process lacks administrator rights the service automatically relaunches
/// the application with UAC elevation as a minimal helper that applies the change and exits.
/// Changes take effect on the next A2DP connection negotiation (disconnect and reconnect required).
/// </summary>
public class BluetoothQualityService : IBluetoothQualityService
{
    /// <summary>
    /// Registry path for Windows Bluetooth AVDTP SBC codec configuration.
    /// Shared with <see cref="App"/> for the elevated helper write path.
    /// </summary>
    internal const string AvdtpSbcKeyPath = @"SYSTEM\CurrentControlSet\Control\Bluetooth\Audio\AVDTP\Sbc";

    /// <summary>
    /// The SBC <c>MaximumBitpool</c> and <c>DefaultBitpool</c> value written in low-bandwidth mode.
    /// Bitpool 15 ≈ 110 kbps, significantly reducing radio load compared to the default of 35–53.
    /// </summary>
    public const int LowBandwidthBitpool = 15;

    /// <inheritdoc />
    public bool IsLowBandwidthModeApplied
    {
        get
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(AvdtpSbcKeyPath, writable: false);
                return key?.GetValue("MaximumBitpool") is int max && max == LowBandwidthBitpool;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BluetoothQuality] IsLowBandwidthModeApplied check failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public async Task<BluetoothQualityResult> ApplyLowBandwidthModeAsync()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(AvdtpSbcKeyPath, writable: true);
            if (key == null)
            {
                return BluetoothQualityResult.NotSupported;
            }

            key.SetValue("MaximumBitpool", LowBandwidthBitpool, RegistryValueKind.DWord);
            key.SetValue("DefaultBitpool", LowBandwidthBitpool, RegistryValueKind.DWord);

            Debug.WriteLine($"[BluetoothQuality] Applied low-bandwidth mode (bitpool={LowBandwidthBitpool}).");
            return BluetoothQualityResult.Applied;
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine("[BluetoothQuality] Access denied — launching elevated helper for apply.");
            return await LaunchElevatedHelperAsync(App.ArgApplyLowEndMode);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BluetoothQuality] Error applying low-bandwidth mode: {ex.Message}");
            return BluetoothQualityResult.NotSupported;
        }
    }

    /// <inheritdoc />
    public async Task<BluetoothQualityResult> RestoreDefaultModeAsync()
    {
        try
        {
            Registry.LocalMachine.DeleteSubKey(AvdtpSbcKeyPath, throwOnMissingSubKey: false);
            Debug.WriteLine("[BluetoothQuality] Deleted AVDTP\\Sbc key — Windows defaults restored.");
            return BluetoothQualityResult.Restored;
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine("[BluetoothQuality] Access denied — launching elevated helper for restore.");
            return await LaunchElevatedHelperAsync(App.ArgRestoreLowEndMode);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BluetoothQuality] Error restoring default mode: {ex.Message}");
            return BluetoothQualityResult.NotSupported;
        }
    }

    /// <summary>
    /// Relaunches the current executable with UAC elevation and the given argument,
    /// waits for it to exit, then verifies the registry state to determine the result.
    /// Returns <see cref="BluetoothQualityResult.AccessDenied"/> if the user cancels the UAC prompt.
    /// </summary>
    /// <param name="argument">The command-line argument identifying the operation to perform.</param>
    /// <returns>A <see cref="BluetoothQualityResult"/> based on the elevated helper's exit code.</returns>
    private static async Task<BluetoothQualityResult> LaunchElevatedHelperAsync(string argument)
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null)
            {
                return BluetoothQualityResult.NotSupported;
            }

            using var helperProcess = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = argument,
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            if (helperProcess == null)
            {
                return BluetoothQualityResult.NotSupported;
            }

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
            await helperProcess.WaitForExitAsync(cts.Token);

            if (helperProcess.ExitCode != 0)
            {
                return BluetoothQualityResult.NotSupported;
            }

            return argument == App.ArgApplyLowEndMode
                ? BluetoothQualityResult.Applied
                : BluetoothQualityResult.Restored;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The user dismissed or cancelled the UAC prompt.
            Debug.WriteLine("[BluetoothQuality] UAC prompt cancelled by user.");
            return BluetoothQualityResult.AccessDenied;
        }
        catch (OperationCanceledException)
        {
            // Elevated helper did not exit within the timeout — treat as a failed operation.
            Debug.WriteLine("[BluetoothQuality] Elevated helper timed out after 30 seconds.");
            return BluetoothQualityResult.NotSupported;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BluetoothQuality] Error launching elevated helper: {ex.Message}");
            return BluetoothQualityResult.NotSupported;
        }
    }
}
