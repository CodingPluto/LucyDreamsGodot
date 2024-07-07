using Godot;
using System;
using System.Diagnostics;
using System.Security.Cryptography;

public partial class Platforms : Node2D
{
    static readonly int GenerationFrames;
    static readonly int StartingPlatformCount;
    static readonly int GenerationEachIteration;
    static readonly Godot.RandomNumberGenerator RNG;
    static int IdOffset;
    static Vector2 ScreenDimensions;
    int _currentGenerationFrame;
    Lucy _lucy;
    PackedScene _scene;
    bool _generationMode;
    static Platforms()
    {
        GenerationFrames = 5;
        StartingPlatformCount = 150;
        GenerationEachIteration = StartingPlatformCount / GenerationFrames; // make these values divisible of each other plz
        RNG = new Godot.RandomNumberGenerator();
        Debug.Assert(RNG != null);
        IdOffset = 1;
    }
    Platforms()
    {
        _currentGenerationFrame = 0;
        _generationMode = false;
    }
    public override void _Ready()
    {
        _lucy = (Lucy)GetNode("../Lucy");
        Debug.Assert(_lucy != null);
        _scene = (PackedScene)ResourceLoader.Load("res://scenes/Platform.tscn");
        Debug.Assert(_scene != null);
        Platform.LoadCloudTextures();
        Platform.LoadDependantScenes();
        RNG.Randomize();
        ScreenDimensions = GetViewportRect().Size;
        Platform.setScreenDimensions(ScreenDimensions);
        generatePlatforms(StartingPlatformCount);
    }
    public override void _Process(double delta)
    {
        if (_lucy.Position.Y < ScreenDimensions.Y / 2 + ((-ScreenDimensions.Y) * Platform.GenerationStage) && _generationMode)
        {
            generatePlatforms(GenerationEachIteration * (GenerationFrames - _currentGenerationFrame));
            _generationMode = false;
            _currentGenerationFrame = 0;
        }
        if (_lucy.Position.Y < ScreenDimensions.Y / 2 + ((-ScreenDimensions.Y) * Platform.GenerationStage) + ScreenDimensions.Y)
        {
            ++Platform.GenerationStage;
            _generationMode = true;
        }
        if (_generationMode)
        {
            generatePlatforms(GenerationEachIteration);
            _currentGenerationFrame++;
            if (_currentGenerationFrame == GenerationFrames)
            {
                _generationMode = false;
                _currentGenerationFrame = 0;
            }
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
