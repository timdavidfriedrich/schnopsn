namespace Schnopsn.core;

using Godot;

public partial class AudioManager : Node2D
{
    public static AudioManager Instance { get; private set; }

    [Export]
    private AudioStreamPlayer _musicPlayer;

    [Export]
    private AudioStreamPlayer _soundPlayer;

    private AudioStream _menuMusic;
    private AudioStream _gameMusic;
    private AudioStream _buttonSound;
    private AudioStream _errorSound;
    private AudioStream _flightSound;
    private AudioStream _flipSound;
    private AudioStream _lostSound;
    private AudioStream _wonSound;

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public override void _Ready()
    {
        _musicPlayer.VolumeDb = -16;
        _soundPlayer.VolumeDb = -8;

        _menuMusic = GD.Load<AudioStream>("res://core/assets/audio/menu_music.mp3");
        _gameMusic = GD.Load<AudioStream>("res://core/assets/audio/game_music.mp3");
        _buttonSound = GD.Load<AudioStream>("res://core/assets/audio/button_sound.wav");
        _errorSound = GD.Load<AudioStream>("res://core/assets/audio/error_sound.wav");
        _flightSound = GD.Load<AudioStream>("res://core/assets/audio/flight_sound.wav");
        _flipSound = GD.Load<AudioStream>("res://core/assets/audio/flip_sound.wav");
        _lostSound = GD.Load<AudioStream>("res://core/assets/audio/lost_sound.wav");
        _wonSound = GD.Load<AudioStream>("res://core/assets/audio/won_sound.wav");

        _musicPlayer.Finished += OnMusicFinished;
    }

    public override void _ExitTree()
    {
        _musicPlayer.Finished -= OnMusicFinished;
    }

    public SignalAwaiter GetSoundFinishedSignal()
    {
        return ToSignal(_soundPlayer, AudioStreamPlayer.SignalName.Finished);
    }

    private void OnMusicFinished()
    {
        if (_musicPlayer.Stream != null)
        {
            _musicPlayer.Play();
        }
    }

    public void PlayMenuMusic()
    {
        if (_musicPlayer.Stream != _menuMusic)
        {
            _musicPlayer.Stream = _menuMusic;
            _musicPlayer.Play();
        }
    }

    public void PlayGameMusic()
    {
        if (_musicPlayer.Stream != _gameMusic)
        {
            _musicPlayer.Stream = _gameMusic;
            _musicPlayer.Play();
        }
    }

    public void PlayButtonSound()
    {
        _soundPlayer.Stream = _buttonSound;
        _soundPlayer.Play();
    }

    public void PlayErrorSound()
    {
        _soundPlayer.Stream = _errorSound;
        _soundPlayer.Play();
    }

    public void PlayFlightSound()
    {
        _soundPlayer.Stream = _flightSound;
        _soundPlayer.Play();
    }

    public void PlayFlipSound()
    {
        _soundPlayer.Stream = _flipSound;
        _soundPlayer.Play();
    }

    public void PlayLostSound()
    {
        _soundPlayer.Stream = _lostSound;
        _soundPlayer.Play();
    }

    public void PlayWonSound()
    {
        _soundPlayer.Stream = _wonSound;
        _soundPlayer.Play();
    }
}