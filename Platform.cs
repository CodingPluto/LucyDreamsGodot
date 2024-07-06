using Godot;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Diagnostics.Tracing;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;

public partial class Platform : StaticBody2D
{
	// Called when the node enters the scene tree for the first time.
	private static Godot.Vector2 screenDimensions;
	public static int generationStage = 1;
	private const int UNLOAD_DISTANCE_STAGES = 2;
	public float sinPoint = 0;
	private float sinDrift = 0;
	public Godot.Vector2 velocity;
	private float sinDriftSpeed = 0;
	public float driftMultiplier = 0;
	private Godot.Vector2 bufferPosition = new Godot.Vector2(0,0);
	public float windSpeed = 0;
	public int direction = 0;
	private Sprite2D sprite;

	private int originalZIndex = 0;
	public float spriteWidth = 0;
	public float spriteHeight = 0;

	int frameCount = 0;

	static private Texture2D[,] textureArray = new Texture2D[5,3];
	private Godot.RandomNumberGenerator rng = new Godot.RandomNumberGenerator();
	private const int IMG_CLOUD_LENGTHS = 5;
	private const int IMG_CLOUD_VARIATIONS = 3;
	public bool interactingWithPlayer = false;
	private CollisionShape2D hitbox;

	enum ItemScenes
	{
		SPIKE,
		SUPERJUMP,
		ITEMSCENES_LENGTH
	}
	static String[] itemNames = new String[(int)ItemScenes.ITEMSCENES_LENGTH];
	static PackedScene[] itemScenes = new PackedScene[(int)ItemScenes.ITEMSCENES_LENGTH];
	static int[] itemGenerationWeights = new int[(int)ItemScenes.ITEMSCENES_LENGTH];
	public static void LoadCloudTextures(){
		for (int i = 0; i < IMG_CLOUD_LENGTHS; ++i)
		{
			for (int j = 0; j < IMG_CLOUD_VARIATIONS; ++j)
			{
				textureArray[i,j] = (Texture2D)GD.Load("res://assets/images/cloud" + (i+1).ToString() + "/sprite_"+ j.ToString() + ".png");
			}
		}
	}
	public static void LoadDependantScenes(){
		itemScenes[(int)ItemScenes.SPIKE] = (PackedScene)ResourceLoader.Load("res://Spike.tscn");
		itemScenes[(int)ItemScenes.SUPERJUMP] = (PackedScene)ResourceLoader.Load("res://SuperJump.tscn");
		itemNames[(int)ItemScenes.SPIKE] = "Spike";
		itemNames[(int)ItemScenes.SUPERJUMP] = "SuperJump"; // Can be automated with quick addition of child and then grab name and then removal.
		itemGenerationWeights[(int)ItemScenes.SPIKE] = 10;
		itemGenerationWeights[(int)ItemScenes.SUPERJUMP] = 90; // must all add to 100


		for (int i = 0; i < (int)ItemScenes.ITEMSCENES_LENGTH; ++i){
			for (int j = 0; j < i; ++j){
				itemGenerationWeights[i] += itemGenerationWeights[j];
			}
			GD.Print("Item Generation Weight: ", (ItemScenes)i, " || ", itemGenerationWeights[i]);
		}
	}
	
	public void BringToFront(){
		ZIndex = 15;
	}
	public void ResetZIndex(){
		ZIndex = originalZIndex;
	}
	public static void setScreenDimensions(Godot.Vector2 _screenDimensions){
		screenDimensions = _screenDimensions;
	}
	public override void _Ready()
	{
		sinDriftSpeed = rng.RandfRange(0.01f,0.04f);
		sprite = GetNode<Sprite2D>("Sprite");
		hitbox = GetNode<CollisionShape2D>("Hitbox");


		//hitbox.Shape.GetRect()
		int cloud_length = rng.RandiRange(1,IMG_CLOUD_LENGTHS);
		((Node2D)hitbox).Scale = new Godot.Vector2(cloud_length,1);	
		sprite.Texture = textureArray[cloud_length - 1,rng.RandiRange(0,IMG_CLOUD_VARIATIONS - 1)];
		spriteWidth = sprite.GetRect().Size.X * Scale.X;
		spriteHeight = sprite.GetRect().Size.Y * Scale.Y;

		Position = new Godot.Vector2(rng.RandiRange(-(int)screenDimensions.X / 2, (int)screenDimensions.X / 2), (rng.RandiRange(-(int)screenDimensions.Y / 2, (int)screenDimensions.Y / 2)) - (screenDimensions.Y * (generationStage - 1)));
		sinPoint = rng.RandfRange(0.0f,1.0f);
		driftMultiplier = rng.RandfRange(0.1f,0.6f);
		if (rng.RandiRange(0,1) == 1){
			windSpeed = rng.RandfRange(0.3f, 0.8f);
		}
		if (rng.RandiRange(0,1) == 1){
			originalZIndex = 15;
		}
		else{
			originalZIndex = 5;
		}
		ZIndex = originalZIndex;
		direction = rng.RandiRange(0,1);
		
		if (direction == 0)
		{
			direction = -1;
		}
		const int CLOUD_CELL_WIDTH = 12;
		const int CLOUD_CELL_HEIGHT = 12;
		int itemCellPosition = 0;
		for (int i = 0; i < cloud_length; i++){
			if (rng.RandiRange(1,10) == 1){
				itemCellPosition = CLOUD_CELL_WIDTH * i;
				int itemPositioning = -CLOUD_CELL_WIDTH * (cloud_length / 2) + itemCellPosition;
				if (cloud_length % 2 == 0){
					itemPositioning += CLOUD_CELL_WIDTH / 2;
				}
				float randomGeneration = rng.RandiRange(0,100);
				int item_index = 0;
				for (int j = 0; j < (int)ItemScenes.ITEMSCENES_LENGTH; ++j){
					if (randomGeneration <= itemGenerationWeights[j]){
						item_index = j;
						break;
					}
				}
				GD.Print("Item Index: ", item_index);
				Area2D item = (Area2D)itemScenes[item_index].Instantiate();

				item.Name = itemNames[item_index] + i.ToString();
				AddChild(item);
				item.Position = new Godot.Vector2(itemPositioning,-CLOUD_CELL_HEIGHT);
			}
		}
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Godot.Vector2 position = Position;
		sinDrift = (float)(Math.Sin((float)sinPoint) * driftMultiplier);
		velocity = new Godot.Vector2((float)(windSpeed * direction),(float) sinDrift);
		position = new Godot.Vector2(Position.X + velocity.X, Position.Y + velocity.Y);
		sinPoint += sinDriftSpeed;
		float leftBound = -screenDimensions.X / 2 - spriteWidth / 2;
		float rightBound = screenDimensions.X / 2 + spriteWidth / 2;
		if (position.X > rightBound)
		{
			position.X = leftBound;
		}
		else if (position.X < leftBound)
		{
			position.X = rightBound;
		}
		int topOfScreen = (int)-screenDimensions.Y / 2;
		int argument = topOfScreen * (generationStage - UNLOAD_DISTANCE_STAGES);
		/*
		if (frameCount % 60 == 0)
		{
			GD.Print("Is ", position.Y, " more than unload barrier?: ", argument);
		}*/
		if (position.Y > argument){
			//Unload platform
			QueueFree();
		}
		Position = position;
		//frameCount++;
	}
}
