using CommunityToolkit.Mvvm.Messaging.Messages;
using EasyBluetoothAudio.Models;

namespace EasyBluetoothAudio.Messages;

/// <summary>
/// Messenger message broadcast when the user changes the application theme.
/// </summary>
/// <param name="Value">The newly selected <see cref="AppThemeMode"/>.</param>
public sealed class ThemeChangedMessage(AppThemeMode Value) : ValueChangedMessage<AppThemeMode>(Value);
