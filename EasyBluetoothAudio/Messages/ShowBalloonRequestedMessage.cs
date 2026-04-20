using CommunityToolkit.Mvvm.Messaging.Messages;
using Hardcodet.Wpf.TaskbarNotification;

namespace EasyBluetoothAudio.Messages;

/// <summary>
/// Payload describing a tray balloon tip that the application should surface to the user.
/// </summary>
/// <param name="Title">The balloon header text.</param>
/// <param name="Body">The balloon body text shown underneath the header.</param>
/// <param name="Icon">The system icon rendered next to the message (info, warning, error).</param>
public sealed record BalloonContent(string Title, string Body, BalloonIcon Icon);

/// <summary>
/// Messenger message requesting that the tray icon display a balloon tip.
/// Used by non-UI layers (such as the ViewModel's recovery flow) to surface
/// user-facing notifications without holding a reference to the TrayIcon instance.
/// </summary>
/// <param name="Value">The balloon content to show.</param>
public sealed class ShowBalloonRequestedMessage(BalloonContent Value) : ValueChangedMessage<BalloonContent>(Value);
