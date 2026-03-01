using CommunityToolkit.Mvvm.Messaging.Messages;

namespace EasyBluetoothAudio.Messages;

/// <summary>
/// Messenger message broadcast when a Bluetooth audio connection has been successfully established.
/// The value carries the device name that was connected.
/// </summary>
/// <param name="Value">The friendly name of the connected device.</param>
public sealed class ConnectionEstablishedMessage(string Value) : ValueChangedMessage<string>(Value);
