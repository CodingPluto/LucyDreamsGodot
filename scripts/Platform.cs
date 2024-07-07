using Godot;
using System;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics.Tracing;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;

public partial class Platform : StaticBody2D
{
    const float SineBottomBoundary = 0.0f;
    const float SineTopBoundary = 1.0f;
    static Godot.Vector2 ScreenDimensions;
    static readonly int UnloadDistanceStages;
    static Texture2D[,] TextureArray;
    static Godot.RandomNumberGenerator RNG;
    static readonly int _imageCloudLengths;
    static readonly int _imageCloudVariations;
    static String[] itemNames;
    static PackedScene[] itemScenes;
    static int[] itemGenerationWeights;
    public static int GenerationStage;
    static readonly Godot.Vector2 DriftMultiplierRNGBounds;
    static readonly Godot.Vector2 WindSpeedRNGBounds;
    float _sinDrift;
    float _sinDriftSpeed;
    Godot.Vector2 _bufferPosition;
    public Godot.Vector2 Velocity;
    public float SinPoint;
    public float DriftMultiplier;
    public float WindSpeed;
    public int Direction;
    public float SpriteWidth;
    public float SpriteHeight;
    public bool InteractingWithPlayer;
    CollisionShape2D _hitbox;
    Sprite2D _sprite;
    enum ItemScenes
    {
        SPIKE, SUPERJUMP, ITEMSCENES_LENGTH
    }

    static Platform()
    {
        itemNames = new String[(int)ItemScenes.ITEMSCENES_LENGTH];
        itemScenes = new PackedScene[(int)ItemScenes.ITEMSCENES_LENGTH];
        itemGenerationWeights = new int[(int)ItemScenes.ITEMSCENES_LENGTH];
        DriftMultiplierRNGBounds = new Godot.Vector2(0.3f, 0.8f);
        WindSpeedRNGBounds = new Godot.Vector2(0.3f, 0.8f);
        UnloadDistanceStages = 2;
        TextureArray = new Texture2D[5, 3];
        RNG = new Godot.RandomNumberGenerator();
        _imageCloudLengths = 5;
        _imageCloudVariations = 3;
        GenerationStage = 1;
    }
    Platform()
    {
        _sinDrift = 0;
        _sinDriftSpeed = 0;
        _bufferPosition = new Godot.Vector2(0, 0);
        SinPoint = 0;
        DriftMultiplier = 0;
        WindSpeed = 0;
        Direction = 0;
        SpriteWidth = 0;
        SpriteHeight = 0;
        InteractingWithPlayer = false;
    }

    public static void LoadCloudTextures()
    {
        for (int CloudLengthIndex = 0; CloudLengthIndex < _imageCloudLengths; ++CloudLengthIndex)
        {
            for (int CloudVariationIndex = 0; CloudVariationIndex < _imageCloudVariations; ++CloudVariationIndex)
            {
                TextureArray[CloudLengthIndex, CloudVariationIndex] = (Texture2D)GD.Load("res://assets/images/cloud" + (CloudLengthIndex + 1).ToString() + "/sprite_" + CloudVariationIndex.ToString() + ".png");
            }
        }
    }
    public static void LoadDependantScenes()
    {
        itemScenes[(int)ItemScenes.SPIKE] = (PackedScene)ResourceLoader.Load("res://scenes/Spike.tscn");
        itemScenes[(int)ItemScenes.SUPERJUMP] = (PackedScene)ResourceLoader.Load("res://scenes/SuperJump.tscn");
        itemNames[(int)ItemScenes.SPIKE] = "Spike";
        itemNames[(int)ItemScenes.SUPERJUMP] = "SuperJump"; // Can be automated with quick addition of child and then grab name and then removal.
        itemGenerationWeights[(int)ItemScenes.SPIKE] = 10;
        itemGenerationWeights[(int)ItemScenes.SUPERJUMP] = 90; // must all add to 100


        for (int Index = 0; Index < (int)ItemScenes.ITEMSCENES_LENGTH; ++Index)
        {
            for (int AdditionIndex = 0; AdditionIndex < Index; ++AdditionIndex)
            {
                itemGenerationWeights[Index] += itemGenerationWeights[AdditionIndex];
            }
        }
    }

    public override void _Ready()
    {
        _sinDriftSpeed = RNG.RandfRange(0.01f, 0.04f);
        _sprite = GetNode<Sprite2D>("Sprite");
        _hitbox = GetNode<CollisionShape2D>("Hitbox");
        int CloudLength = RNG.RandiRange(1, _imageCloudLengths);
        ((Node2D)_hitbox).Scale = new Godot.Vector2(CloudLength, 1);
        _sprite.Texture = TextureArray[CloudLength - 1, RNG.RandiRange(0, _imageCloudVariations - 1)];
        SpriteWidth = _sprite.GetRect().Size.X * Scale.X;
        SpriteHeight = _sprite.GetRect().Size.Y * Scale.Y;
        int XPosition = RNG.RandiRange(-(int)ScreenDimensions.X / 2, (int)ScreenDimensions.X / 2);
        int YPosition = RNG.RandiRange(-(int)ScreenDimensions.Y / 2, (int)ScreenDimensions.Y / 2) - ((int)ScreenDimensions.Y * (GenerationStage - 1));
        Position = new Godot.Vector2(XPosition, YPosition);
        SinPoint = RNG.RandfRange(SineBottomBoundary, SineTopBoundary);
        DriftMultiplier = RNG.RandfRange(DriftMultiplierRNGBounds.X, DriftMultiplierRNGBounds.Y);
        if (RNG.RandiRange(0, 1) == 1)
        {
            WindSpeed = RNG.RandfRange(WindSpeedRNGBounds.X, WindSpeedRNGBounds.Y);
        }
        Direction = RNG.RandiRange(0, 1);
        if (Direction == 0)
        {
            Direction = -1;
        }
        const int CloudCellWidth = 12;
        const int CloudCellHeight = 12;
        int ItemCellPosition = 0;
        for (int Index = 0; Index < CloudLength; Index++)
        {
            if (RNG.RandiRange(1, 10) == 1)
            {
                ItemCellPosition = CloudCellWidth * Index;
                int ItemPositioning = -CloudCellWidth * (CloudLength / 2) + ItemCellPosition;
                if (CloudLength % 2 == 0)
                {
                    ItemPositioning += CloudCellWidth / 2;
                }
                float RandomGeneration = RNG.RandiRange(0, 100);
                int ItemIndex = 0;
                for (int j = 0; j < (int)ItemScenes.ITEMSCENES_LENGTH; ++j)
                {
                    if (RandomGeneration <= itemGenerationWeights[j])
                    {
                        ItemIndex = j;
                        break;
                    }
                }
                Area2D Item = (Area2D)itemScenes[ItemIndex].Instantiate();

                Item.Name = itemNames[ItemIndex] + Index.ToString();
                AddChild(Item);
                Item.Position = new Godot.Vector2(ItemPositioning, -CloudCellHeight);
            }
        }

    }
    public override void _Process(double delta)
    {
        Godot.Vector2 BufferPosition = Position;
        _sinDrift = (float)(Math.Sin((float)SinPoint) * DriftMultiplier);
        Velocity = new Godot.Vector2((float)(WindSpeed * Direction), (float)_sinDrift);
        BufferPosition = new Godot.Vector2(Position.X + Velocity.X, Position.Y + Velocity.Y);
        SinPoint += _sinDriftSpeed;
        float LeftBound = -ScreenDimensions.X / 2 - SpriteWidth / 2;
        float RightBound = ScreenDimensions.X / 2 + SpriteWidth / 2;
        if (BufferPosition.X > RightBound)
        {
            BufferPosition.X = LeftBound;
        }
        else if (BufferPosition.X < LeftBound)
        {
            BufferPosition.X = RightBound;
        }
        int TopOfScreen = (int)-ScreenDimensions.Y / 2;
        int Argument = TopOfScreen * (GenerationStage - UnloadDistanceStages);
        if (BufferPosition.Y > Argument)
        {
            QueueFree();
        }
        Position = BufferPosition;
    }
    public static void setScreenDimensions(Godot.Vector2 _screenDimensions)
    {
        ScreenDimensions = _screenDimensions;
    }
}

