using Godot;
using System;
using System.Numerics;
using System.Reflection.Metadata;

public partial class Lucy : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -800.0f;


	private const int COYOTE_FRAMES = 60;

	private bool isAlive = true;
	private bool isSuperJumping = false;
	private bool hasSuperJumped = false;
	private const int DEATH_FRAMES = 20;
	private int deathFrameCount = 0;
	int coyoteFrameCount = 0;
	bool coyoteTime = false;


	private Platform currentPlatform;
	private AnimatedSprite2D sprite; 
	private CollisionShape2D hitbox;
	private Camera2D camera;

	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		Position = new Godot.Vector2(0,0);
		ZIndex = 10;
		sprite = GetNode<AnimatedSprite2D>("Sprite");	
		hitbox = GetNode<CollisionShape2D>("Hitbox");
		camera = GetNode<Camera2D>("Camera");
		camera.LimitRight = (int) GetViewportRect().Size.X / 2;
		camera.LimitLeft = -camera.LimitRight;
		camera.LimitBottom = (int) GetViewportRect().Size.Y / 2;
		physicsState = PhysicsState.GENERAL_INTERACTION;
	}

	public void OnAreaEntered(Area2D area)
	{
		GD.Print("Area Name: ",area.Name);
		if (area.Name.ToString().Substring(0,5) == "Super")
		{
			hasSuperJumped = true;
		}
		else if (!isSuperJumping)
		{
			isAlive = false;
		}
	}
	private void Die(){
		if (deathFrameCount == 0)
		{
			physicsState = PhysicsState.NO_PHYSICS;
			Velocity = new Godot.Vector2(0,0);
			sprite.Play("die");
		}
		if (deathFrameCount > DEATH_FRAMES){
			Position = new Godot.Vector2(0, 0);
			Velocity = new Godot.Vector2(0,0);
			isAlive = true;
			deathFrameCount = 0;
			physicsState = PhysicsState.GENERAL_INTERACTION;
			return;
		}
		deathFrameCount++;
	}
    public override void _Process(double delta)
    {
        base._Process(delta);
		if (!isAlive)
		{
			Die();
		}

		if (Input.IsActionJustPressed("Exit"))
		{
			GetTree().Quit();
		}
    }

	private bool hasJumped = false;
	private bool isJumping = false;
	private bool isRunning = false;
	private Godot.Vector2 position;
	enum PhysicsState
	{
		CLOUD_INTERACTION,
		GENERAL_INTERACTION,
		NO_PHYSICS
	}
	private PhysicsState physicsState;

	private bool HasPlatformInteraction(){
		return (IsOnFloor() || physicsState == PhysicsState.CLOUD_INTERACTION);
	}
	private void PollJump(ref Godot.Vector2 velocity, ref double delta){
		if ((Input.IsActionPressed("lucy_up")) || hasSuperJumped){
			sprite.Play("jumping");
			velocity.Y = JumpVelocity;
			hasJumped = true;
			isJumping = true;
			physicsState = PhysicsState.GENERAL_INTERACTION;
		}
		if (HasPlatformInteraction() && hasJumped){
			hasJumped = false;
		}
		if (hasSuperJumped){
			isSuperJumping = true;
			velocity.Y *= 2.5f;
			hasSuperJumped = false;
		}
	}
	private void PhysicsCloudInteraction(ref Godot.Vector2 velocity, ref double delta){
		float widthAdjustment = hitbox.Shape.GetRect().Size.X * Scale.X / 2;
		float platformWidthAdjustment = currentPlatform.spriteWidth / 2;
		velocity.Y = 0;
		position = new Godot.Vector2(position.X + currentPlatform.velocity.X, (currentPlatform.Position.Y - currentPlatform.spriteHeight / 2) - ((hitbox.Shape.GetRect().Size.Y) * Scale.Y / 2));
		bool withinXBounds = (position.X + widthAdjustment > currentPlatform.Position.X - platformWidthAdjustment)
		&& (position.X - widthAdjustment < currentPlatform.Position.X + platformWidthAdjustment);
		if (!withinXBounds){
			//GD.Print("Not within X Bounds");
			physicsState = PhysicsState.GENERAL_INTERACTION;
			currentPlatform.ResetZIndex();
			currentPlatform.interactingWithPlayer = false;
		}
		
	}
	private void GeneralPlatformInteraction(ref Godot.Vector2 velocity, ref double delta){
		isJumping = false;
		if (!IsOnFloor()){
			velocity.Y += gravity * (float)delta;
		}
		else{
			coyoteFrameCount = 0;
			velocity.Y = 0;
			for (int i = 0; i < GetSlideCollisionCount(); i++){
				KinematicCollision2D collision = GetSlideCollision(i);
				Node baseNode = (Node)collision.GetCollider();
				int ID = 0;
				bool isNumber = int.TryParse(baseNode.Name.ToString(), out ID);
				if (isNumber){ // Identifying collider
					currentPlatform = (Platform)baseNode;
					physicsState = PhysicsState.CLOUD_INTERACTION;
					currentPlatform.BringToFront();
					currentPlatform.interactingWithPlayer = true;
				}
				//GD.Print("Lucy Collided with Platform: ", baseNode.Name.ToString());
				}
		}
	}

    public override void _PhysicsProcess(double delta)
	{
		Godot.Vector2 velocity = Velocity;
		position = Position;
		//GD.Print("PhysicsState: ", physicsState);
		if (isSuperJumping && velocity.Y > 0){
			isSuperJumping = false;
		}
		switch(physicsState)
		{
			case PhysicsState.CLOUD_INTERACTION:
				PhysicsCloudInteraction(ref velocity,ref delta);
				break;
			case PhysicsState.GENERAL_INTERACTION:
				GeneralPlatformInteraction(ref velocity,ref delta);
				break;
			case PhysicsState.NO_PHYSICS:
				return;
			default:
				GD.Print("This isn't supposed to trigger!");
				break;
		}
		PollJump(ref velocity, ref delta);
		Godot.Vector2 direction = Input.GetVector("lucy_left", "lucy_right", "lucy_up", "lucy_down");
		if (direction != Godot.Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
			isRunning = true;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed / 30);
			isRunning = false;
		}


		if (direction.X > 0){
			sprite.FlipH = false;
		}
		else if (direction.X < 0){
			sprite.FlipH = true;
		}
		if (isJumping){
			sprite.Play("jumping");
		}
		else if (Velocity.Y >= 0){
			if (Math.Abs(velocity.X) > 50 && HasPlatformInteraction()){
				sprite.Play("running");
			}
			else if(HasPlatformInteraction()){
				sprite.Play("idle");
			}
			else if (!hasJumped || (hasJumped && velocity.Y > 850)){
				sprite.Play("falling");
			}
		}

		Position = position;
		Velocity = velocity;
		//GD.Print("Velocity: ", velocity);
		MoveAndSlide();
		float leftBound = -GetViewportRect().Size.X / 2 + (hitbox.Shape.GetRect().Size.X) * Scale.X;
		float rightBound = GetViewportRect().Size.X / 2 - (hitbox.Shape.GetRect().Size.X) * Scale.X;

		position = Position;
		if (position.X > rightBound)
		{
			position.X = rightBound;
		}
		else if (position.X < leftBound)
		{
			position.X = leftBound;
		}
		Position = position;
	}
	

}
