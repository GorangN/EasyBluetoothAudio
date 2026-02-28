using System;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using EasyBluetoothAudio.Messages;

namespace EasyBluetoothAudio.Services;

/// <summary>
/// Plays a custom notification sound when a Bluetooth connection is established,
/// controlled by the <see cref="SoundSettingsChangedMessage"/> preference.
/// Uses <see cref="MediaPlayer"/> for MP3 support.
/// </summary>
/// <param name="messenger">The messenger instance for receiving sound-related messages.</param>
/// <param name="settingsService">The settings service for reading the persisted sound preference.</param>
public class SoundService(IMessenger messenger, ISettingsService settingsService) : ISoundService
{
    private const string SoundFileName = "Assets\\NotifySound.mp3";

    private bool _playConnectionSound;
    private Uri? _soundUri;
    private readonly MediaPlayer _player = new();

    /// <inheritdoc />
    public void Initialize()
    {
        _playConnectionSound = settingsService.Load().PlayConnectionSound;

        var soundPath = Path.Combine(AppContext.BaseDirectory, SoundFileName);
        if (File.Exists(soundPath))
        {
            _soundUri = new Uri(soundPath, UriKind.Absolute);
            _player.Open(_soundUri);
        }

        messenger.Register<SoundSettingsChangedMessage>(this, (_, message) =>
            _playConnectionSound = message.Value);

        messenger.Register<ConnectionEstablishedMessage>(this, (_, _) => OnConnectionEstablished());
    }

    /// <summary>
    /// Plays the custom notification sound if the connection-sound preference is enabled.
    /// Falls back to the system asterisk if the sound file is unavailable.
    /// </summary>
    private void OnConnectionEstablished()
    {
        if (!_playConnectionSound)
        {
            return;
        }

        try
        {
            if (_soundUri != null)
            {
                _player.Stop();
                _player.Position = TimeSpan.Zero;
                _player.Play();
            }
            else
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
        }
        catch
        {
            // Sound playback failures are non-critical; swallow silently.
        }
    }
}
