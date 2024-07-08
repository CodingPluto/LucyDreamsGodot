using Godot;
using System;

public partial class Background : Sprite2D
{
    int _levelTracker;
    static readonly float DimMultiplier;
    static Texture2D stars; 
    static readonly float StarsOpacityIncrement;
    Color _originalModulate;
    bool _starsInSky;
    float _starsOpacity;
    static Background()
    {
        DimMultiplier = 0.08f;
        StarsOpacityIncrement = 0.01f;
    }
    
	public override void _Ready()
	{
        GD.Print("Original Modulation: ", Modulate);
        GD.Print("Made Background!");
        _levelTracker = 0;
        stars = (Texture2D)GD.Load("res://assets/images/stars.png");
        _starsOpacity = 0.0f;
        _starsInSky = false;
        _originalModulate = Modulate;
	}
	public override void _Process(double delta)
	{
        GD.Print("Level: ", Platforms.Level);
        if (_levelTracker != Platforms.Level && Platforms.Level % 2 == 0)
        {
            if (Platforms.Level < 15)
            {
                float DimAmount = DimMultiplier * Platforms.Level;
                Modulate = new Color(_originalModulate.R - DimAmount, _originalModulate.G - DimAmount, _originalModulate.B - DimAmount, Modulate.A);
                _levelTracker = Platforms.Level;
            }
            else
            {
                _starsInSky = true;
            }
        }
        if (_starsInSky)
        {
            Texture = stars;
            Modulate = new Color(_originalModulate.R, _originalModulate.G, _originalModulate.B, _starsOpacity);
            if (_starsOpacity < 1.0f)
            {
                _starsOpacity += StarsOpacityIncrement;
            }
        }
	}
}
