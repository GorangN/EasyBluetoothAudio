using CommunityToolkit.Mvvm.Messaging.Messages;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Messages;

/// <summary>
/// Messenger message broadcast after settings have been persisted,
/// carrying the full <see cref="AppSettings"/> snapshot.
/// </summary>
/// <param name="Value">The persisted <see cref="AppSettings"/> instance.</param>
public sealed class SettingsSavedMessage(AppSettings Value) : ValueChangedMessage<AppSettings>(Value);
