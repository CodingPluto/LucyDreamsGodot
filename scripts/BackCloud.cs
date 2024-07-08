using Godot;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using static Godot.Vector2;

public partial class BackCloud : Sprite2D
{
    Vector2 _Velocity;
    float _Opacity;
    float _sinPoint;
    float _frequency;
    bool _cloudAttributesSet;
    readonly static RandomNumberGenerator RNG;
    static BackCloud(){
        RNG = new RandomNumberGenerator();
        Debug.Assert(RNG != null);
        GenerateBackCloudsTraditional();
    }
    BackCloud()
    {
        _cloudAttributesSet = false;
    }
    private static void GenerateBackCloudsTraditional()
    {
    }
    public void SetCloudAttributes(Vector2 position, Vector2 velocity, float opacity, int zIndex)
    {
        GD.Print("Set Cloud Attributes!");
        Position = position;
        _Velocity = velocity;
        _Opacity = opacity;
        ZIndex = zIndex;
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, _Opacity);
        _cloudAttributesSet = true;
    }
    public override void _Ready()
    {
        _sinPoint = RNG.RandfRange(0.0f,1.0f);
        _frequency = RNG.RandfRange(0.01f, 0.04f);
        if (!_cloudAttributesSet)
        {
            int XPosition = RNG.RandiRange(-(int)GetViewportRect().Size.X / 2 , (int)GetViewportRect().Size.X / 2);
            int YPosition = RNG.RandiRange(-(int)GetViewportRect().Size.Y / 2 , (int)GetViewportRect().Size.Y / 2);
            float XVelocity = 0;
            if (RNG.RandiRange(0,1) == 1){
                XVelocity = RNG.RandfRange(-3.0f,3.0f);
            }

            SetCloudAttributes(new Vector2(XPosition,YPosition),new Vector2(XVelocity,0),RNG.RandfRange(0.1f,0.9f),RNG.RandiRange(0,15));
        }
    }
    public override void _Process(double delta)
    {
        _sinPoint += _frequency;
        _Velocity.Y = (float)Math.Sin(_sinPoint) * 0.8f;
        Vector2 BufferPosition = new Vector2(Position.X + _Velocity.X, Position.Y + _Velocity.Y);
        float LeftBound = -GetViewportRect().Size.X / 2 - (GetRect().Size.X * Scale.X) / 2;
        float RightBound = GetViewportRect().Size.X / 2 + (GetRect().Size.X * Scale.X) / 2;
        if (BufferPosition.X > RightBound)
        {
            BufferPosition.X = LeftBound;
        }
        else if (BufferPosition.X < LeftBound)
        {
            BufferPosition.X = RightBound;
        }
        Position = BufferPosition;
    }
}