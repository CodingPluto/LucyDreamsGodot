using Godot;
using System;

public partial class music : AudioStreamPlayer
{
	// Called when the node enters the scene tree for the first time.
    float _playbackPosition;
    bool _isMuted;
	public override void _Ready()
	{
        _playbackPosition = 0;
        _isMuted = false;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (Input.IsActionJustPressed("mute"))
        {
            if (_isMuted)
            {
                Play(_playbackPosition);
                _isMuted = false;

            }
            else
            {
                _playbackPosition = GetPlaybackPosition();
                Stop();
                _isMuted = true;
            }
        }
	}
}
