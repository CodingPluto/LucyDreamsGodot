using Godot;
using System;
using System.Collections.Generic;
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

        Platform.LoadCloudTextures();
        Platform.LoadDependantScenes();
        RNG.Randomize();
        ScreenDimensions = GetViewportRect().Size;
        Platform.setScreenDimensions(ScreenDimensions);
        GeneratePlatforms(StartingPlatformCount);
    }
    public override void _Process(double delta)
    {
        if ((_lucy.Position.Y + _lucy.Hitbox.Shape.GetRect().Size.Y * Scale.Y) < -ScreenDimensions.Y / 2)
        {
            _lucy.LevelUp();
            foreach(Node Platform in _platforms)
            {
                Platform.QueueFree();
            }
            foreach(Node BackCloud in _backClouds)
            {
                BackCloud.QueueFree();
            }
            Level ++;
            _platforms.Clear();
            _backClouds.Clear();
            GeneratePlatforms(StartingPlatformCount - 5 * Level);
            GenerateBackClouds(BackCloudsCount - 2 * Level);
        }
    }
    List<Node> _platforms = new List<Node>();
    private void GeneratePlatforms(int Count)
    {
        int Index;
        for (Index = 0; Index < Count; ++Index)
        {
            Node Instance = _scene.Instantiate();
            AddChild(Instance);
            Instance.Name = (Index + IdOffset).ToString();
            _platforms.Add(Instance);
        }
        IdOffset += Index;
    }
    List<Node> _backClouds = new List<Node>();
    private void GenerateBackClouds(int Count)
    {
        for (int Index = 0; Index < Count; ++Index)
        {
            var cloud = _backCloudScene.Instantiate();
            AddChild(cloud);
            _backClouds.Add(cloud);
        }
    }
}