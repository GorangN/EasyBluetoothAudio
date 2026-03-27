using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EasyBluetoothAudio.Messages;

/// <summary>
/// Messenger message broadcast when a Bluetooth quality registry change has been successfully
/// applied and the audio connection must be cycled for the setting to take effect.
/// </summary>
public sealed class ReconnectRequestedMessage() : ValueChangedMessage<bool>(true);
