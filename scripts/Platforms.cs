using Godot;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;

public partial class Platforms : Node2D
{
    static readonly int GenerationFrames;
    static readonly int StartingPlatformCount;
    static readonly int GenerationEachIteration;
    static readonly Godot.RandomNumberGenerator RNG;
    static readonly int BackCloudsCount;
    static public int Level;
    static int IdOffset;
    static Godot.Vector2 ScreenDimensions;
    int _currentGenerationFrame;
    Lucy _lucy;
    PackedScene _scene;
    PackedScene _backCloudScene;
    bool _generationMode;
    static Platforms()
    {
        GenerationFrames = 5;
        StartingPlatformCount = 150;
        BackCloudsCount = 50;
        GenerationEachIteration = StartingPlatformCount / GenerationFrames; // make these values divisible of each other plz
        RNG = new Godot.RandomNumberGenerator();
        Debug.Assert(RNG != null);
        IdOffset = 1;
    }
    Platforms()
    {
        Level = 0;
        _currentGenerationFrame = 0;
        _generationMode = false;
    }
    public override void _Ready()
    {
        _lucy = (Lucy)GetNode("../Lucy");
        Debug.Assert(_lucy != null);
        _scene = (PackedScene)ResourceLoader.Load("res://scenes/Platform.tscn");
        Debug.Assert(_scene != null);
        _backCloudScene = (PackedScene)ResourceLoader.Load("res://scenes/BackCloud.tscn");
        Debug.Assert(_backCloudScene != null);
        for (int i = 0; i < BackCloudsCount; ++i)
        {
            var cloud = _backCloudScene.Instantiate();
            AddChild(cloud);
        }

        Platform.LoadCloudTextures();
        Platform.LoadDependantScenes();
        RNG.Randomize();
        ScreenDimensions = GetViewportRect().Size;
        Platform.setScreenDimensions(ScreenDimensions);
        generatePlatforms(StartingPlatformCount);
    }
    public override void _Process(double delta)
    {
        if ((_lucy.Position.Y + _lucy.Hitbox.Shape.GetRect().Size.Y * Scale.Y) < -ScreenDimensions.Y / 2)
        {
            _lucy.SetPositionEndOfFrame(_lucy.Position.X, (ScreenDimensions.Y / 2) - 200);
            if (_lucy.Velocity.Y > -200)
            {
                _lucy.SetVelocityEndOfFrame(_lucy.Velocity.X,Lucy.JumpVelocity * 2);
            }
            Level ++;
        }
    }
    private void generatePlatforms(int Count)
    {
        int Index;
        for (Index = 0; Index < Count; ++Index)
        {
            Node Instance = _scene.Instantiate();
            AddChild(Instance);
            Instance.Name = (Index + IdOffset).ToString();
        }
        IdOffset += Index;
    }
}