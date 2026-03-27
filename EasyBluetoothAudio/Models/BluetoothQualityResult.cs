namespace EasyBluetoothAudio.Models;

/// <summary>
/// Represents the outcome of a Bluetooth audio quality registry operation.
/// </summary>
public enum BluetoothQualityResult
{
    /// <summary>Low-bandwidth mode was successfully applied to the registry.</summary>
    Applied,

    /// <summary>The registry was successfully restored to default values.</summary>
    Restored,

    /// <summary>The operation failed because administrator privileges are required to write to HKLM.</summary>
    AccessDenied,

    /// <summary>The registry path could not be created; the Bluetooth adapter may not support this setting.</summary>
    NotSupported
}
