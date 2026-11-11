namespace EasyBluetoothAudio.Models;

/// <summary>A previously opened source to restore when automatic startup connection is enabled.</summary>
public sealed class RememberedAudioDevice
{
    /// <summary>Gets or sets the Windows audio source identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the last discovered display name, retained while the source is unavailable.</summary>
    public string Name { get; set; } = "Saved device";
}
