using Godot;
using System;
using System.Security.Cryptography;

public partial class Platforms : Node2D
{
	private const int GENERATION_FRAMES = 5;
	private static int IDOffset = 1;
	private const int STARTING_PLATFORM_COUNT = 150; // make these values divisible of each other plz
	private const int GENERATION_EACH_ITERATION = STARTING_PLATFORM_COUNT / GENERATION_FRAMES;
	int currentGenerationFrame = 0;
	bool generationMode = false;
	// Called when the node enters the scene tree for the first time.
	private Godot.RandomNumberGenerator rng = new Godot.RandomNumberGenerator();
	private static Vector2 screenDimensions;
	Lucy lucy;
	PackedScene scene;

	private void generatePlatforms(int count){
		int i;
		for (i = 0; i < count; ++i){
			Node instance = scene.Instantiate();
			AddChild(instance);
			instance.Name = (i + IDOffset).ToString();
		}
		IDOffset += i;
	}
	public override void _Ready(){
		lucy = (Lucy)GetNode("../Lucy");

		screenDimensions = GetViewportRect().Size;
		Platform.setScreenDimensions(screenDimensions);
		Platform.LoadCloudTextures();
		Platform.LoadDependantScenes();
		rng.Randomize();

		// Load the scene
        scene = (PackedScene)ResourceLoader.Load("res://Platform.tscn");

		generatePlatforms(STARTING_PLATFORM_COUNT);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (lucy.Position.Y < screenDimensions.Y / 2 + ((-screenDimensions.Y) * Platform.generationStage) && generationMode){
			generatePlatforms(GENERATION_EACH_ITERATION * (GENERATION_FRAMES - currentGenerationFrame));
			generationMode = false;
			currentGenerationFrame = 0;
		}
		if (lucy.Position.Y < screenDimensions.Y / 2 + ((-screenDimensions.Y) * Platform.generationStage) + screenDimensions.Y){
			++Platform.generationStage;
			generationMode = true;
			GD.Print("New generationStage: ", Platform.generationStage, " || Lucy Position: ", lucy.Position);
		}
		if (generationMode){
			generatePlatforms(GENERATION_EACH_ITERATION);
			currentGenerationFrame++;
			if (currentGenerationFrame == GENERATION_FRAMES){
				generationMode = false;
				currentGenerationFrame = 0;
			}
		}
	}
}
