using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EasyBluetoothAudio.Messages;

/// <summary>
/// Messenger message broadcast when the connection-sound preference changes.
/// </summary>
/// <param name="Value"><see langword="true"/> if a connection sound should be played; otherwise <see langword="false"/>.</param>
public sealed class SoundSettingsChangedMessage(bool Value) : ValueChangedMessage<bool>(Value);
